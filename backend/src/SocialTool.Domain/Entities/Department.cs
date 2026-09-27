using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

// Nó da estrutura da empresa (unidade, departamento, setor). A árvore vem de ParentDepartmentId.
public class Department : BaseEntity
{
    public Guid? ParentDepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AreaKind Kind { get; set; } = AreaKind.Department;
    public Guid? LeaderId { get; set; }

    // Navigations
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public User? Leader { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
