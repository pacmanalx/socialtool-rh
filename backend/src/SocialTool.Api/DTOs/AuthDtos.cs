namespace SocialTool.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    UserProfileDto User,
    TenantDto Tenant
);

public record UserProfileDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Email,
    string JobTitle,
    string Role,
    string? AvatarUrl,
    int CoinsAvailableToGive,
    int CoinsBalanceToSpend,
    string? DepartmentName
);

public record TenantDto(
    Guid Id,
    string Name,
    string Subdomain,
    string CurrencyName,
    int MonthlyCoinsQuota
);
