using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class OneOnOne : BaseEntity
{
    public Guid LeaderId { get; set; }
    public Guid LedId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public OneOnOneStatus Status { get; set; } = OneOnOneStatus.Scheduled;
    public string? PrivateLeaderNotes { get; set; }
    public string? SharedNotes { get; set; }

    // Navigations
    public User Leader { get; set; } = null!;
    public User Led { get; set; } = null!;

    public ICollection<OneOnOnePoint> Points { get; set; } = new List<OneOnOnePoint>();
    public ICollection<OneOnOneAction> Actions { get; set; } = new List<OneOnOneAction>();
}

public class OneOnOnePoint : BaseEntity
{
    public Guid OneOnOneId { get; set; }
    public Guid CreatedById { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;

    // Navigations
    public OneOnOne OneOnOne { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}

public class OneOnOneAction : BaseEntity
{
    public Guid OneOnOneId { get; set; }
    public Guid AssigneeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public bool IsDone { get; set; } = false;

    // Navigations
    public OneOnOne OneOnOne { get; set; } = null!;
    public User Assignee { get; set; } = null!;
}
