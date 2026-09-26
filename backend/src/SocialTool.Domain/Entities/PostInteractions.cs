using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class PostReaction : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public ReactionType Type { get; set; } = ReactionType.Like;

    // Navigations
    public Post Post { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class PostComment : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;

    // Navigations
    public Post Post { get; set; } = null!;
    public User Author { get; set; } = null!;
}
