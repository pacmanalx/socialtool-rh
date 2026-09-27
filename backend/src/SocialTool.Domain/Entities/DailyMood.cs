using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class DailyMood : BaseEntity
{
    public Guid UserId { get; set; }
    public MoodScore Score { get; set; }
    public string? Note { get; set; }
    public DateOnly Date { get; set; }

    // Navigations
    public User User { get; set; } = null!;
}
