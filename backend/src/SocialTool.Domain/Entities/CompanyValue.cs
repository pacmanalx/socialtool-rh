using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

public class CompanyValue : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "Award"; // Nome do ícone Lucide
    public bool IsActive { get; set; } = true;

    // Navigations
    public ICollection<Recognition> Recognitions { get; set; } = new List<Recognition>();
}
