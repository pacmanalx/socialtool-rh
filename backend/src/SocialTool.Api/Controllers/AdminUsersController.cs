using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// RH convida e edita dados cadastrais; papel, desativação e qualquer ação sobre administradores ficam com o Admin.
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin,HR")]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly AccountTokenService _accountTokens;
    private readonly SessionService _sessions;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        ApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        AccountTokenService accountTokens,
        SessionService sessions,
        ILogger<AdminUsersController> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _accountTokens = accountTokens;
        _sessions = sessions;
        _logger = logger;
    }

    private bool IsAdmin => _currentUser.Role == nameof(UserRole.Admin);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsers()
    {
        var users = await _dbContext.Users
            .Include(u => u.Department)
            .OrderBy(u => u.Name)
            .ToListAsync();

        return Ok(users.Select(ToDto));
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IEnumerable<DepartmentOptionDto>>> GetDepartments()
    {
        var departments = await _dbContext.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionDto(d.Id, d.Name))
            .ToListAsync();

        return Ok(departments);
    }

    [HttpPost]
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
        if (role == UserRole.Admin && !IsAdmin)
            return Forbid();
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
            HireDate = request.HireDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            CoinsAvailableToGive = organization.MonthlyCoinsQuota
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        if (!await TrySendInvitationAsync(user, organization.Name))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Usuário criado, mas o e-mail de convite não pôde ser enviado. Confira a configuração de e-mail e use \"Reenviar convite\"."
            });
        }

        await _dbContext.Entry(user).Reference(u => u.Department).LoadAsync();
        return CreatedAtAction(nameof(GetUsers), ToDto(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var name = request.Name?.Trim() ?? string.Empty;
        var jobTitle = request.JobTitle?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 150)
            return BadRequest(new { message = "Informe o nome (até 150 caracteres)." });
        if (jobTitle.Length is 0 or > 100)
            return BadRequest(new { message = "Informe o cargo (até 100 caracteres)." });
        if (!TryParseRole(request.Role, out var role))
            return BadRequest(new { message = "Papel inválido." });
        if (request.DepartmentId.HasValue && !await _dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return BadRequest(new { message = "Departamento não encontrado." });

        if (!IsAdmin && (user.Role == UserRole.Admin || role != user.Role))
            return Forbid();
        if (user.Id == _currentUser.UserId && role != user.Role)
            return BadRequest(new { message = "Você não pode alterar o seu próprio papel." });

        user.Name = name;
        user.JobTitle = jobTitle;
        user.Role = role;
        user.DepartmentId = request.DepartmentId;
        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(user).Reference(u => u.Department).LoadAsync();
        return Ok(ToDto(user));
    }

    [HttpPost("{id:guid}/resend-invite")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (!user.IsActive || user.ActivatedAt != null)
            return BadRequest(new { message = "Só é possível reenviar convite para quem ainda não entrou." });
        if (user.Role == UserRole.Admin && !IsAdmin)
            return Forbid();

        if (!await TrySendInvitationAsync(user, (await _dbContext.GetOrganizationAsync()).Name))
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "O e-mail de convite não pôde ser enviado. Confira a configuração de e-mail." });

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<AdminUserDto>> Deactivate(Guid id)
    {
        if (id == _currentUser.UserId)
            return BadRequest(new { message = "Você não pode desativar a sua própria conta." });

        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        user.IsActive = false;
        await _dbContext.SaveChangesAsync();
        await _sessions.RevokeAllAsync(user.Id);
        return Ok(ToDto(user));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<AdminUserDto>> Reactivate(Guid id)
    {
        var user = await _dbContext.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        user.IsActive = true;
        await _dbContext.SaveChangesAsync();
        return Ok(ToDto(user));
    }

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

    private static bool TryParseRole(string? value, out UserRole role) =>
        Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    private static AdminUserDto ToDto(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.JobTitle,
        user.Role.ToString(),
        user.DepartmentId,
        user.Department?.Name,
        !user.IsActive ? "Inactive" : user.ActivatedAt == null ? "Invited" : "Active",
        user.LastLoginAt,
        user.CreatedAt);
}
