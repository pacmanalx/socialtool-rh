using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Sessão de login. Rotacionado a cada uso; só o hash é persistido.
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive(DateTime nowUtc) => RevokedAt == null && ExpiresAt > nowUtc;
}
