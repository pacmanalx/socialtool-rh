using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string CurrencyName { get; set; } = "SocialCoins";
    public int MonthlyCoinsQuota { get; set; } = 100;
    public bool IsActive { get; set; } = true;

    // Navigation collections
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<CompanyValue> CompanyValues { get; set; } = new List<CompanyValue>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
