using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

public class Recognition : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public Guid CompanyValueId { get; set; }
    public Guid? PostId { get; set; }

    public int CoinsAmount { get; set; }
    public string Message { get; set; } = string.Empty;

    // Navigations
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
    public CompanyValue CompanyValue { get; set; } = null!;
    public Post? Post { get; set; }
}
