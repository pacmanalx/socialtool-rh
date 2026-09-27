namespace SocialTool.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record GoogleLoginRequest(string Credential);

public record AcceptInvitationRequest(string Token, string Password);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string Password);

public record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

public record AuthConfigDto(string? GoogleClientId, bool PasswordResetAvailable);

public record InvitationInfoDto(string Name, string Email, string OrganizationName);

public record SessionResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    UserProfileDto User,
    OrganizationDto Organization
);

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string JobTitle,
    string Role,
    string? AvatarUrl,
    int CoinsAvailableToGive,
    int CoinsBalanceToSpend,
    string? DepartmentName,
    bool HasPassword
);

public record OrganizationDto(
    string Name,
    string CurrencyName,
    int MonthlyCoinsQuota
);
