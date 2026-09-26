using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public DateOnly HireDate { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Employee;

    public int CoinsAvailableToGive { get; set; } = 100;
    public int CoinsBalanceToSpend { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigations
    public Tenant Tenant { get; set; } = null!;
    public Department? Department { get; set; }
    public User? Manager { get; set; }
    public ICollection<User> Subordinates { get; set; } = new List<User>();

    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();

    public ICollection<Recognition> RecognitionsSent { get; set; } = new List<Recognition>();
    public ICollection<Recognition> RecognitionsReceived { get; set; } = new List<Recognition>();

    public ICollection<DailyMood> DailyMoods { get; set; } = new List<DailyMood>();
}
