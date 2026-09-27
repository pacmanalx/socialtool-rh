using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

public class Department : BaseEntity
{
    public Guid? ParentDepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? LeaderId { get; set; }

    // Navigations
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public User? Leader { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
