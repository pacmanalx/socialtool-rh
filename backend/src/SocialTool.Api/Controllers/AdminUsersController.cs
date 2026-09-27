using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.Authorization;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Authorization;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// Cada ação exige a sua permissão (concedida pessoa a pessoa pelo Admin; o Admin pode tudo).
// Quem não é Admin nunca age sobre contas de Admin nem de outros gestores de RH, e só o Admin
// dá ou tira os papéis de RH e Admin (dar RH concede permissões).
[ApiController]
[Route("api/admin/users")]
[RequirePermission(Permissions.UsersView)]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly AccountTokenService _accountTokens;
    private readonly SessionService _sessions;
    private readonly WorkspaceUserImportService _importer;
    private readonly PermissionService _permissions;
    private readonly AuditService _audit;
    private readonly ILogger<AdminUsersController> _logger;

    private const long ImportMaxBytes = 20 * 1024 * 1024;

    public AdminUsersController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        AccountTokenService accountTokens,
        SessionService sessions,
        WorkspaceUserImportService importer,
        PermissionService permissions,
        AuditService audit,
        ILogger<AdminUsersController> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _accountTokens = accountTokens;
        _sessions = sessions;
        _importer = importer;
        _permissions = permissions;
        _audit = audit;
        _logger = logger;
    }

    private Task<bool> IsAdminAsync() => _permissions.IsAdminAsync();

    // Contas de Admin e de gestores de RH só são mexidas pelo Admin.
    private async Task<bool> CanActOnAsync(User target) =>
        await IsAdminAsync() || target.Role is not (UserRole.Admin or UserRole.HR);

    // Colaborador <-> Líder com a permissão de alterar papel; qualquer mudança envolvendo RH ou Admin, só o Admin.
    private async Task<bool> CanAssignRoleAsync(UserRole from, UserRole to)
    {
        if (from == to)
            return true;
        if (await IsAdminAsync())
            return true;
        var elevated = from is UserRole.Admin or UserRole.HR || to is UserRole.Admin or UserRole.HR;
        return !elevated && await _permissions.HasAsync(Permissions.UsersAssignRoles);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
    {
        var users = await _dbContext.Users
            .Include(u => u.Department)
            .OrderBy(u => u.Name)
            .ToListAsync();
        var invited = await PendingInvitationUserIdsAsync();

        return Ok(users.Select(u => ToDto(u, invited.Contains(u.Id))));
    }

    [HttpGet("email-status")]
    public ActionResult<EmailStatusDto> GetEmailStatus([FromServices] IEmailSender emailSender) =>
        Ok(new EmailStatusDto(emailSender.IsEnabled, emailSender.RedirectTo));

    [HttpPost("import/preview")]
    [RequirePermission(Permissions.UsersImport)]
    [RequestSizeLimit(ImportMaxBytes)]
    public async Task<ActionResult<UserImportPreviewDto>> PreviewImport([FromForm] UserImportRequest request)
    {
        var (plan, error) = await BuildImportPlanAsync(request);
        return error ?? Ok(WorkspaceUserImportService.ToPreview(plan!));
    }

    [HttpPost("import")]
    [RequirePermission(Permissions.UsersImport)]
    [RequestSizeLimit(ImportMaxBytes)]
    public async Task<ActionResult<UserImportResultDto>> ApplyImport([FromForm] UserImportRequest request)
    {
        var (plan, error) = await BuildImportPlanAsync(request);
        if (error != null)
            return error;

        var result = await _importer.ApplyAsync(plan!, _currentUser.UserId!.Value, request.File!.FileName);
        _logger.LogInformation(
            "Importação de usuários por {UserId}: {Created} criados, {Updated} atualizados, {Deactivated} desativados, {Skipped} ignorados",
            _currentUser.UserId, result.Created, result.Updated, result.Deactivated, result.Skipped);
        await _audit.LogAsync(AuditService.Actions.UsersImported,
            $"Importou {request.File!.FileName}: {result.Created} criados, {result.Updated} atualizados, " +
            $"{result.Deactivated} desativados, {result.Skipped} ignorados.");
        return Ok(result);
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentOptionDto>>> GetDepartments()
    {
        // Nome com o caminho na estrutura ("Unidade › Departamento › Setor"), para distinguir homônimos.
        var tree = await AreaTree.LoadAsync(_dbContext);
        return Ok(tree.All
            .Select(d => new DepartmentOptionDto(d.Id, tree.PathOf(d.Id)))
            .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase));
    }

    [HttpPost]
    [RequirePermission(Permissions.UsersInvite)]
    public async Task<ActionResult<AdminUserDto>> Invite([FromBody] InviteUserRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var jobTitle = request.JobTitle?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (name.Length is 0 or > 150)
            return BadRequest(new { message = "Informe o nome (até 150 caracteres)." });
        if (jobTitle.Length is 0 or > 100)
            return BadRequest(new { message = "Informe o cargo (até 100 caracteres)." });
        if (email.Length > 200 || !MailAddress.TryCreate(email, out _))
            return BadRequest(new { message = "Informe um e-mail válido." });
        if (!TryParseRole(request.Role, out var role))
            return BadRequest(new { message = "Papel inválido." });
        if (!await CanAssignRoleAsync(UserRole.Employee, role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não pode cadastrar alguém com esse papel." });
        if (request.DepartmentId.HasValue && !await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return BadRequest(new { message = "Departamento não encontrado." });

        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Já existe um usuário com esse e-mail." });

        var organization = await _dbContext.GetOrganizationAsync();
        var user = new User
        {
            Name = name,
            Email = email,
            JobTitle = jobTitle,
            Role = role,
            DepartmentId = request.DepartmentId,
            HireDate = request.HireDate,
            CoinsAvailableToGive = organization.MonthlyCoinsQuota
        };
        _dbContext.Users.Add(user);
        if (role == UserRole.HR)
            GrantHrDefaults(user);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.UserInvited,
            $"Cadastrou {user.Email} como {RoleLabel(role)}.", user);

        // Com o envio desligado (ou o destinatário fora da lista liberada), só cadastra: fica "Nunca acessou".
        if (!_accountTokens.CanEmail(user))
        {
            await _dbContext.Entry(user).Reference(u => u.Department).LoadAsync();
            return CreatedAtAction(nameof(GetUsers), ToDto(user, invitePending: false));
        }

        if (!await TrySendInvitationAsync(user, organization.Name))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Usuário criado, mas o e-mail de convite não pôde ser enviado. Confira a configuração de e-mail e use \"Reenviar convite\"."
            });
        }

        await _dbContext.Entry(user).Reference(u => u.Department).LoadAsync();
        return CreatedAtAction(nameof(GetUsers), ToDto(user, invitePending: true));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.UsersEdit)]
    public async Task<ActionResult<AdminUserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var name = request.Name?.Trim() ?? string.Empty;
        var jobTitle = request.JobTitle?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 150)
            return BadRequest(new { message = "Informe o nome (até 150 caracteres)." });
        // Cargo pode ficar vazio na edição: usuários importados nem sempre têm cargo no diretório.
        if (jobTitle.Length > 100)
            return BadRequest(new { message = "O cargo pode ter até 100 caracteres." });
        if (!TryParseRole(request.Role, out var role))
            return BadRequest(new { message = "Papel inválido." });
        if (request.DepartmentId.HasValue && !await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return BadRequest(new { message = "Departamento não encontrado." });

        if (user.Id != _currentUser.UserId && !await CanActOnAsync(user))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só administradores alteram contas de administradores e de gestores de RH." });
        if (user.Id == _currentUser.UserId && role != user.Role)
            return BadRequest(new { message = "Você não pode alterar o seu próprio papel." });
        if (!await CanAssignRoleAsync(user.Role, role))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não pode dar esse papel." });

        var changes = new List<string>();
        if (user.Name != name) changes.Add($"nome \"{user.Name}\" → \"{name}\"");
        if (user.JobTitle != jobTitle) changes.Add($"cargo \"{user.JobTitle}\" → \"{jobTitle}\"");
        if (user.DepartmentId != request.DepartmentId) changes.Add("departamento");
        var previousRole = user.Role;

        user.Name = name;
        user.JobTitle = jobTitle;
        user.Role = role;
        user.DepartmentId = request.DepartmentId;
        if (previousRole != role)
            await ApplyRolePermissionsAsync(user, previousRole);
        await _dbContext.SaveChangesAsync();

        if (changes.Count > 0)
            await _audit.LogAsync(AuditService.Actions.UserUpdated, $"Editou {user.Email}: {string.Join(", ", changes)}.", user);
        if (previousRole != role)
            await _audit.LogAsync(AuditService.Actions.UserRoleChanged,
                $"Mudou o papel de {user.Email}: {RoleLabel(previousRole)} → {RoleLabel(role)}.", user);

        await _dbContext.Entry(user).Reference(u => u.Department).LoadAsync();
        return Ok(ToDto(user, await HasPendingInvitationAsync(user.Id)));
    }

    [HttpPost("{id:guid}/resend-invite")]
    [RequirePermission(Permissions.UsersInvite)]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (!user.IsActive || user.ActivatedAt != null)
            return BadRequest(new { message = "Só é possível enviar convite para quem ainda não acessou." });
        if (!await CanActOnAsync(user))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só administradores convidam administradores e gestores de RH." });
        if (!_accountTokens.CanEmail(user))
            return Conflict(new { message = "O envio de e-mails está desligado nesta instalação. Nenhum convite foi enviado." });

        if (!await TrySendInvitationAsync(user, (await _dbContext.GetOrganizationAsync()).Name))
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "O e-mail de convite não pôde ser enviado. Confira a configuração de e-mail." });

        await _audit.LogAsync(AuditService.Actions.UserInviteResent, $"Reenviou o convite de {user.Email}.", user);
        return NoContent();
    }

    // Envio em lote. O front manda no máximo BulkInviteMax por chamada e repete, mostrando o progresso:
    // centenas de e-mails numa requisição só estourariam o tempo limite e o limite do servidor SMTP.
    public const int BulkInviteMax = 25;

    [HttpPost("invitations")]
    [RequirePermission(Permissions.UsersInvite)]
    public async Task<ActionResult<BulkInviteResultDto>> SendInvitations(
        [FromBody] BulkInviteRequest request,
        [FromServices] IEmailSender emailSender)
    {
        if (!emailSender.IsEnabled)
            return Conflict(new { message = "O envio de e-mails está desligado nesta instalação. Nenhum convite foi enviado." });

        var ids = (request.UserIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return BadRequest(new { message = "Nenhum usuário selecionado." });
        if (ids.Count > BulkInviteMax)
            return BadRequest(new { message = $"Envie no máximo {BulkInviteMax} convites por vez." });

        var users = await _dbContext.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        var organizationName = (await _dbContext.GetOrganizationAsync()).Name;
        int sent = 0, blocked = 0, skipped = ids.Count - users.Count;
        var failed = new List<string>();
        var isAdmin = await IsAdminAsync();

        foreach (var user in users)
        {
            // Mesmas regras do convite individual: só quem nunca acessou, e quem não é Admin não convida Admin nem RH.
            if (!user.IsActive || user.ActivatedAt != null || (!isAdmin && user.Role is UserRole.Admin or UserRole.HR))
            {
                skipped++;
                continue;
            }
            if (!_accountTokens.CanEmail(user))
            {
                blocked++;
                continue;
            }
            if (await TrySendInvitationAsync(user, organizationName))
                sent++;
            else
                failed.Add(user.Email);
        }

        if (sent > 0)
            await _audit.LogAsync(AuditService.Actions.UserInvitesBulk, $"Enviou {sent} convite(s) em lote.");
        return Ok(new BulkInviteResultDto(sent, skipped, blocked, failed.Count, failed));
    }

    [RequirePermission(Permissions.UsersRevoke)]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<AdminUserDto>> Deactivate(Guid id, [FromBody] AccessChangeRequest? request)
    {
        if (id == _currentUser.UserId)
            return BadRequest(new { message = "Você não pode desativar a sua própria conta." });

        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (!await CanActOnAsync(user))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só administradores revogam o acesso de administradores e gestores de RH." });

        user.IsActive = false;
        await _dbContext.SaveChangesAsync();
        await _sessions.RevokeAllAsync(user.Id);
        await _audit.LogAsync(AuditService.Actions.UserRevoked, $"Revogou o acesso de {user.Email}.", user, CleanReason(request?.Reason));
        return Ok(ToDto(user, await HasPendingInvitationAsync(user.Id)));
    }

    [RequirePermission(Permissions.UsersAuthorize)]
    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<AdminUserDto>> Reactivate(Guid id, [FromBody] AccessChangeRequest? request)
    {
        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (!await CanActOnAsync(user))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Só administradores liberam o acesso de administradores e gestores de RH." });

        user.IsActive = true;
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.UserAuthorized, $"Liberou o acesso de {user.Email}.", user, CleanReason(request?.Reason));
        return Ok(ToDto(user, await HasPendingInvitationAsync(user.Id)));
    }

    // Ao virar RH, recebe o conjunto padrão (o Admin ajusta depois); ao deixar de ser RH, perde todas.
    private async Task ApplyRolePermissionsAsync(User user, UserRole previousRole)
    {
        if (previousRole == UserRole.HR)
            _dbContext.UserPermissions.RemoveRange(await _dbContext.UserPermissions.Where(p => p.UserId == user.Id).ToListAsync());
        if (user.Role == UserRole.HR && !await _dbContext.UserPermissions.AnyAsync(p => p.UserId == user.Id))
            GrantHrDefaults(user);
    }

    private void GrantHrDefaults(User user)
    {
        foreach (var permission in Permissions.HrDefaults)
            _dbContext.UserPermissions.Add(new UserPermission { UserId = user.Id, Permission = permission, GrantedById = _currentUser.UserId });
    }

    private static string? CleanReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    public static string RoleLabel(UserRole role) => role switch
    {
        UserRole.Admin => "Administrador",
        UserRole.HR => "Gestor de RH",
        UserRole.Leader => "Líder",
        _ => "Colaborador"
    };

    private async Task<bool> TrySendInvitationAsync(User user, string organizationName)
    {
        try
        {
            await _accountTokens.SendInvitationAsync(user, organizationName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar convite para o usuário {UserId}", user.Id);
            return false;
        }
    }

    private async Task<(ImportPlan? Plan, ActionResult? Error)> BuildImportPlanAsync(UserImportRequest request)
    {
        if (request.File == null || request.File.Length == 0)
            return (null, BadRequest(new { message = "Envie o arquivo JSON exportado do Admin Console." }));
        if (request.DeactivateSuspended && !await _permissions.HasAsync(Permissions.UsersRevoke))
            return (null, StatusCode(StatusCodes.Status403Forbidden, new { message = "Desativar usuários na importação exige a permissão de revogar acesso." }));

        List<DirectoryEntry> entries;
        try
        {
            await using var stream = request.File.OpenReadStream();
            entries = WorkspaceExportParser.Parse(stream);
        }
        catch (FormatException ex)
        {
            return (null, BadRequest(new { message = ex.Message }));
        }
        if (entries.Count == 0)
            return (null, BadRequest(new { message = "O arquivo não tem nenhum usuário." }));

        return (await _importer.BuildPlanAsync(entries, request, _currentUser.UserId), null);
    }

    private async Task<HashSet<Guid>> PendingInvitationUserIdsAsync()
    {
        var now = DateTime.UtcNow;
        var ids = await _dbContext.UserTokens
            .Where(t => t.Purpose == UserTokenPurpose.Invitation && t.UsedAt == null && t.ExpiresAt > now)
            .Select(t => t.UserId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    private Task<bool> HasPendingInvitationAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        return _dbContext.UserTokens.AnyAsync(t =>
            t.UserId == userId && t.Purpose == UserTokenPurpose.Invitation && t.UsedAt == null && t.ExpiresAt > now);
    }

    private static bool TryParseRole(string? value, out UserRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    // Pending = nunca acessou e sem convite válido; Invited = convite enviado e ainda válido.
    private static AdminUserDto ToDto(User user, bool invitePending) => new(
        user.Id,
        user.Name,
        user.Email,
        user.JobTitle,
        user.Role.ToString(),
        user.DepartmentId,
        user.Department?.Name,
        !user.IsActive ? "Inactive"
            : user.ActivatedAt != null ? "Active"
            : invitePending ? "Invited" : "Pending",
        user.LastLoginAt,
        user.CreatedAt);
}
