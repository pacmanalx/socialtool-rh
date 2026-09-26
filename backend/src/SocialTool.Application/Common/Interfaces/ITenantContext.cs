namespace SocialTool.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? Subdomain { get; }
    bool HasTenant => TenantId.HasValue;
    void SetTenant(Guid tenantId, string subdomain);
}
