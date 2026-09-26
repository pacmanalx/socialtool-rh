using SocialTool.Domain.Enums;

namespace SocialTool.Api.DTOs;

public record CreatePostRequest(
    string? Title,
    string Content,
    string? ImageUrl,
    PostType Type = PostType.General
);

public record PostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string AuthorJobTitle,
    string? AuthorAvatarUrl,
    PostType Type,
    string? Title,
    string Content,
    string? ImageUrl,
    bool IsPinned,
    DateTime CreatedAt,
    int ReactionsCount,
    Dictionary<string, int> ReactionsByType,
    List<string> CurrentUserReactions,
    RecognitionSummaryDto? Recognition,
    List<PostCommentDto> Comments
);

public record RecognitionSummaryDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    Guid ReceiverId,
    string ReceiverName,
    string? ReceiverAvatarUrl,
    string ValueTitle,
    string ValueIcon,
    int CoinsAmount
);

public record PostCommentDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Content,
    DateTime CreatedAt
);

public record ReactPostRequest(ReactionType Type);

public record CreateCommentRequest(string Content);
