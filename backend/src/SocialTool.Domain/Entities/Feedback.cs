using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class Feedback : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;
    public FeedbackVisibility Visibility { get; set; } = FeedbackVisibility.Private;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Sent;

    // Navigations
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
