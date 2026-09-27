using Microsoft.EntityFrameworkCore;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

// Primeira subida de uma instalação: garante a linha da organização e, se a seção Bootstrap trouxer um
// e-mail de administrador e ainda não houver nenhum usuário, cria esse administrador sem senha e manda o convite.
// Idempotente: numa instalação que já tem usuários, não faz nada além de conferir a organização.
public static class OrganizationBootstrapper
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var section = configuration.GetSection("Bootstrap");
        var db = services.GetRequiredService<ApplicationDbContext>();

        var organizationName = section["OrganizationName"]?.Trim();
        await DbInitializer.EnsureOrganizationAsync(db, string.IsNullOrEmpty(organizationName) ? "Minha Organização" : organizationName);

        var adminEmail = section["AdminEmail"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(adminEmail) || await db.Users.AnyAsync())
            return;

        var organization = await db.GetOrganizationAsync();
        // Aceita vários domínios separados por vírgula: "acme.com,acme.com.br".
        var googleDomains = section["GoogleWorkspaceDomains"];
        if (!string.IsNullOrWhiteSpace(googleDomains))
            organization.SetGoogleWorkspaceDomains(googleDomains.Split(','));

        var admin = new User
        {
            Name = section["AdminName"]?.Trim() is { Length: > 0 } adminName ? adminName : "Administrador",
            Email = adminEmail,
            JobTitle = "Administrador",
            Role = UserRole.Admin,
            CoinsAvailableToGive = organization.MonthlyCoinsQuota
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("Bootstrap: administrador {Email} criado.", adminEmail);

        try
        {
            await services.GetRequiredService<AccountTokenService>().SendInvitationAsync(admin, organization.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bootstrap: o convite do administrador não pôde ser enviado. " +
                "Confira a configuração de e-mail e use \"Esqueci minha senha\" com {Email} para receber o link.", adminEmail);
        }
    }
}
