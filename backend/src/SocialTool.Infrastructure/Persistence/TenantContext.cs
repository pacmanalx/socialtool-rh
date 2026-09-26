using SocialTool.Application.Common.Interfaces;

namespace SocialTool.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? Subdomain { get; private set; }

    public void SetTenant(Guid tenantId, string subdomain)
    {
        TenantId = tenantId;
        Subdomain = subdomain;
    }
}
