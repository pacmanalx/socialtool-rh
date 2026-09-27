using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SocialTool.Api.Authorization;
using SocialTool.Api.Controllers;
using SocialTool.Api.Hubs;
using SocialTool.Api.Middlewares;
using SocialTool.Api.Services;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Infrastructure.Persistence;
using SocialTool.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey) || Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
    throw new InvalidOperationException("Jwt:SecretKey precisa ter pelo menos 32 bytes.");
if (!builder.Environment.IsDevelopment() && jwtSecretKey.StartsWith("CHANGE-ME", StringComparison.Ordinal))
    throw new InvalidOperationException("Troque Jwt:SecretKey por um segredo próprio antes de rodar fora de Development.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0)), mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
// SmtpTransport não é um IEmailSender: o único jeito de enviar e-mail é pelo wrapper com trava e desvio.
builder.Services.AddScoped<SmtpTransport>();
builder.Services.AddScoped<IEmailSender, GuardedEmailSender>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AccountTokenService>();
builder.Services.AddScoped<WorkspaceUserImportService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SocialToolRh",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SocialToolRhClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // WebSockets não mandam header Authorization: o SignalR passa o token na query string.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// Tudo exige login por padrão; endpoints públicos são marcados com [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
// [RequirePermission("...")] e [RequireAdmin]: políticas montadas sob demanda e conferidas no banco a cada requisição.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) => new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
        new { message = "Muitas tentativas. Aguarde um minuto e tente de novo." }, cancellationToken));
    options.AddPolicy(AuthController.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSignalR();

// Em desenvolvimento o front chega pelo proxy do Vite (mesma origem) e CORS não é necessário.
// Só libere origens explícitas: com AllowCredentials, uma origem aberta entregaria a sessão a qualquer site.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });
}

builder.Services.AddOpenApi();

// Atrás de um proxy reverso ou túnel, o IP do visitante chega no X-Forwarded-For. Sem isto, o limite de
// tentativas de login seria um só para todo mundo. Só ligue se o app NÃO for acessível sem passar pelo proxy.
var behindProxy = builder.Configuration.GetValue("ForwardedHeaders:Enabled", false);
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (behindProxy)
{
    app.UseForwardedHeaders();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await SystemDataSeeder.SeedAsync(db);

        if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Seed:SampleData", false))
        {
            var samplePassword = app.Configuration["Seed:SamplePassword"]
                ?? throw new InvalidOperationException("Seed:SamplePassword não configurada.");
            await DbInitializer.SeedSampleDataAsync(db, services.GetRequiredService<IPasswordHasher>(), samplePassword);
        }

        await OrganizationBootstrapper.RunAsync(services, app.Configuration, app.Logger);
        app.Logger.LogInformation("Banco de dados sincronizado.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Erro ao aplicar migrações ou carga de dados inicial no MySQL.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

// Em produção o front compilado fica em wwwroot e é servido pelo próprio backend (mesma origem).
// index.html sempre revalidado (uma versão nova chega no próximo carregamento); /static/* tem hash no nome
// e pode ficar em cache por um ano.
var frontFiles = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = ctx.Context.Request.Path.StartsWithSegments("/static")
            ? "public, max-age=31536000, immutable"
            : "no-cache";
    }
};
app.UseDefaultFiles();
app.UseStaticFiles(frontFiles);

app.UseAuthentication();
app.UseMiddleware<ActiveUserMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SocialFeedHub>("/hubs/feed");
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Rotas do front (/convite/..., /redefinir-senha/...) devolvem o index.html; /api e /hubs continuam 404.
// O "nonfile" é essencial: sem ele a rota casa também /assets/x.js e o navegador recebe HTML no lugar do script.
app.MapFallbackToFile("{*path:nonfile:regex(^(?!api/|hubs/).*$)}", "index.html", frontFiles).AllowAnonymous();

app.Run();
