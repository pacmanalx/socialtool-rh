using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

// Token de uso único enviado por e-mail (convite, redefinição de senha). Só o hash é persistido.
public class UserToken : BaseEntity
{
    public Guid UserId { get; set; }
    public UserTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsUsable(DateTime nowUtc) => UsedAt == null && ExpiresAt > nowUtc;
}
