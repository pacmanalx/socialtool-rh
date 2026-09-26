using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class Post : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid AuthorId { get; set; }
    public PostType Type { get; set; } = PostType.General;
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPinned { get; set; } = false;

    // Navigations
    public Tenant Tenant { get; set; } = null!;
    public User Author { get; set; } = null!;
    public Recognition? Recognition { get; set; }

    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}
