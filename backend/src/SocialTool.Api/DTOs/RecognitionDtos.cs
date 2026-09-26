namespace SocialTool.Api.DTOs;

public record SendRecognitionRequest(
    Guid ReceiverId,
    Guid CompanyValueId,
    int CoinsAmount,
    string Message
);

public record CompanyValueDto(
    Guid Id,
    string Title,
    string Description,
    string Icon
);

public record LeaderboardItemDto(
    Guid UserId,
    string Name,
    string JobTitle,
    string? AvatarUrl,
    int CoinsReceived,
    int RecognitionsCount
);
