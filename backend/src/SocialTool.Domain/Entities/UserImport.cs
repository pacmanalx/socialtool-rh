using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Registro de cada importação de usuários (auditoria: quem importou, de qual arquivo, o que mudou).
public class UserImport : BaseEntity
{
    public Guid ImportedById { get; set; }
    public string Source { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalInFile { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public int Skipped { get; set; }
    public int DepartmentsCreated { get; set; }

    public User ImportedBy { get; set; } = null!;
}
