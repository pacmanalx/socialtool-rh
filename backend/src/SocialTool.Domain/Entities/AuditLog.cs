using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Registro de ações administrativas: quem fez o quê, sobre quem, quando e (quando exigido) por quê.
// Só inclusão: não há tela nem endpoint que altere ou apague registros.
public class AuditLog : BaseEntity
{
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? TargetName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
}
