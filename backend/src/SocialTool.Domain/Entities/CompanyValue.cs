using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

public class CompanyValue : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "Award"; // Nome do ícone Lucide
    public bool IsActive { get; set; } = true;

    // Navigations
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Recognition> Recognitions { get; set; } = new List<Recognition>();
}
