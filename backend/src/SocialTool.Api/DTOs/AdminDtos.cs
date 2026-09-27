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
    string? GoogleWorkspaceDomain,
    bool GoogleLoginConfigured
);

public record UpdateOrganizationSettingsRequest(
    string Name,
    string CurrencyName,
    int MonthlyCoinsQuota,
    string? GoogleWorkspaceDomain
);
