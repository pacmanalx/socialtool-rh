namespace SocialTool.Api.DTOs;

public record AdminUserDto(
    Guid Id,
    string Name,
    string Email,
    string JobTitle,
    string Role,
    Guid? DepartmentId,
    string? DepartmentName,
    string Status,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public record InviteUserRequest(
    string Name,
    string Email,
    string JobTitle,
    string Role,
    Guid? DepartmentId,
    DateOnly? HireDate
);

public record UpdateUserRequest(
    string Name,
    string JobTitle,
    string Role,
    Guid? DepartmentId
);

public record DepartmentOptionDto(Guid Id, string Name);

public record OrganizationSettingsDto(
    string Name,
    string CurrencyName,
    int MonthlyCoinsQuota,
    IReadOnlyList<string> GoogleWorkspaceDomains,
    bool GoogleLoginConfigured
);

public record UpdateOrganizationSettingsRequest(
    string Name,
    string CurrencyName,
    int MonthlyCoinsQuota,
    List<string>? GoogleWorkspaceDomains
);

// Importação de usuários a partir do arquivo "Fazer o download dos usuários" do Admin Console do Google.
public class UserImportRequest
{
    public IFormFile? File { get; set; }
    // Contas que nunca entraram no Google costumam ser de serviço ou compartilhadas.
    public bool IncludeNeverSignedIn { get; set; }
    // Desativar aqui quem está suspenso no Workspace. Só administradores.
    public bool DeactivateSuspended { get; set; }
    public bool CreateMissingDepartments { get; set; }
    public List<string>? ExcludedEmails { get; set; }
}

public record UserImportRowDto(
    string Email,
    string Name,
    string? JobTitle,
    string? Department,
    string Action,
    string? Reason,
    IReadOnlyList<string> Changes,
    bool SuspendedInWorkspace,
    bool NeverSignedInGoogle,
    string? LocalStatus
);

public record UserImportPreviewDto(
    int TotalInFile,
    int ToCreate,
    int ToUpdate,
    int ToDeactivate,
    int Unchanged,
    int Skipped,
    int LocalNotInFile,
    IReadOnlyList<string> DepartmentsToCreate,
    IReadOnlyList<string> UnmappedDepartments,
    IReadOnlyList<UserImportRowDto> Rows
);

public record UserImportResultDto(int Created, int Updated, int Deactivated, int Skipped, int DepartmentsCreated);

public record BulkInviteRequest(List<Guid> UserIds);

public record BulkInviteResultDto(int Sent, int Skipped, int Failed, IReadOnlyList<string> FailedEmails);
