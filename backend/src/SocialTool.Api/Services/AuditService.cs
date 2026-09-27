using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

// Grava as ações administrativas. Nomes são copiados para o registro: renomear ou desativar alguém
// depois não muda o que a auditoria mostra sobre o passado.
public class AuditService
{
    public static class Actions
    {
        public const string UserInvited = "user.invited";
        public const string UserInviteResent = "user.invite_resent";
        public const string UserInvitesBulk = "user.invites_bulk";
        public const string UserUpdated = "user.updated";
        public const string UserRoleChanged = "user.role_changed";
        public const string UserRevoked = "user.revoked";
        public const string UserAuthorized = "user.authorized";
        public const string UsersImported = "users.imported";
        public const string PermissionsChanged = "permissions.changed";
        public const string OrganizationUpdated = "organization.updated";
        public const string SensitiveAccessed = "sensitive.accessed";
        public const string AreaCreated = "structure.area_created";
        public const string AreaUpdated = "structure.area_updated";
        public const string AreaDeleted = "structure.area_deleted";
        public const string AreaMembersChanged = "structure.members_changed";
        public const string SurveyCreated = "survey.created";
        public const string SurveyUpdated = "survey.updated";
        public const string SurveyPublished = "survey.published";
        public const string SurveyClosed = "survey.closed";
        public const string SurveyDeleted = "survey.deleted";
        public const string ScaleChanged = "survey.scale_changed";
    }

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext dbContext, ICurrentUserService currentUser, IHttpContextAccessor http)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _http = http;
    }

    public async Task LogAsync(string action, string summary, User? target = null, string? reason = null)
    {
        var actorName = _currentUser.UserId is { } actorId
            ? await _dbContext.Users.Where(u => u.Id == actorId).Select(u => u.Name).FirstOrDefaultAsync()
            : null;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorId = _currentUser.UserId,
            ActorName = Truncate(actorName ?? "Sistema", 150),
            Action = action,
            TargetUserId = target?.Id,
            TargetName = target == null ? null : Truncate(target.Name, 150),
            Summary = Truncate(summary, 500),
            Reason = reason == null ? null : Truncate(reason, 500),
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await _dbContext.SaveChangesAsync();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
