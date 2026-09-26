namespace SocialTool.Api.DTOs;

public record MoodCheckinRequest(int Score, string? Note);

public record MoodSummaryDto(
    int Score,
    string? Note,
    DateOnly Date
);

public record UserSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string JobTitle,
    string? AvatarUrl,
    string? DepartmentName
);
