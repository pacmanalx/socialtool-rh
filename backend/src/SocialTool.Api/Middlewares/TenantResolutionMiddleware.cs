using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Middlewares;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ApplicationDbContext dbContext)
    {
        string? identifier = null;

        // 1. Verificar cabeçalho customizado X-Tenant-Id
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            identifier = headerVal.ToString().Trim();
        }

        // 2. Se logado, verificar claim do Token JWT
        if (string.IsNullOrEmpty(identifier) && context.User.Identity?.IsAuthenticated == true)
        {
            identifier = context.User.FindFirst("TenantId")?.Value
                         ?? context.User.FindFirst("Subdomain")?.Value;
        }

        // 3. Verificar subdomínio no Host (ex: acme.example.com)
        if (string.IsNullOrEmpty(identifier))
        {
            var host = context.Request.Host.Host;
            var parts = host.Split('.');
            if (parts.Length > 2 && parts[0] != "www")
            {
                identifier = parts[0];
            }
        }

        // 4. Fallback padrão para ambiente de desenvolvimento: "demo"
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = "demo";
        }

        // Resolução do Tenant no banco com IgnoreQueryFilters
        var tenant = Guid.TryParse(identifier, out var tenantGuid)
            ? await dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantGuid && t.IsActive)
            : await dbContext.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Subdomain == identifier && t.IsActive);

        if (tenant != null)
        {
            tenantContext.SetTenant(tenant.Id, tenant.Subdomain);
        }

        await _next(context);
    }
}
