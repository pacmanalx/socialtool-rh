using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public const string RateLimitPolicy = "auth";

    // Hash de referência para gastar o mesmo tempo quando o e-mail não existe (evita descobrir contas por tempo de resposta).
    private static readonly Lazy<string> DummyPasswordHash = new(() => BCrypt.Net.BCrypt.HashPassword("socialtool-timing-dummy"));

    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly SessionService _sessions;
    private readonly AccountTokenService _accountTokens;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        SessionService sessions,
        AccountTokenService accountTokens,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _sessions = sessions;
        _accountTokens = accountTokens;
        _configuration = configuration;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public ActionResult<AuthConfigDto> GetConfig([FromServices] IEmailSender emailSender) =>
        Ok(new AuthConfigDto(GoogleClientId(), emailSender.IsEnabled));

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("login")]
    public async Task<ActionResult<SessionResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await FindActiveUserByEmailAsync(request.Email);

        var passwordOk = _passwordHasher.VerifyPassword(
            request.Password ?? string.Empty,
            user?.PasswordHash ?? DummyPasswordHash.Value);

        if (user?.PasswordHash == null || !passwordOk)
            return Unauthorized(new { message = "E-mail ou senha incorretos." });

        return Ok(await _sessions.StartAsync(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("google")]
    public async Task<ActionResult<SessionResponse>> LoginWithGoogle([FromBody] GoogleLoginRequest request)
    {
        var clientId = GoogleClientId();
        if (clientId == null)
            return NotFound(new { message = "O login com Google não está habilitado." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.Credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Credencial Google recusada: {Reason}", ex.Message);
            return Unauthorized(new { message = "Não foi possível validar a conta Google." });
        }

        const string notAllowed = "Esta conta Google não tem acesso. Peça um convite ao RH ou ao administrador.";
        if (!payload.EmailVerified || string.IsNullOrEmpty(payload.Email))
            return Unauthorized(new { message = notAllowed });

        var user = await FindActiveUserByEmailAsync(payload.Email);
        var allowedDomains = (await _dbContext.GetOrganizationAsync()).GetGoogleWorkspaceDomains();
        var emailDomain = payload.Email[(payload.Email.LastIndexOf('@') + 1)..].ToLowerInvariant();
        // hd vazio = conta Google pessoal: sempre recusada. Aceita o hd ou o domínio do e-mail, para cobrir
        // contas de domínios secundários do mesmo Workspace.
        var domainAllowed = !string.IsNullOrEmpty(payload.HostedDomain)
            && (allowedDomains.Contains(payload.HostedDomain.ToLowerInvariant()) || allowedDomains.Contains(emailDomain));
        if (user == null
            || !domainAllowed
            || (user.GoogleSubject != null && user.GoogleSubject != payload.Subject))
        {
            return Unauthorized(new { message = notAllowed });
        }

        user.GoogleSubject ??= payload.Subject;
        user.ActivatedAt ??= DateTime.UtcNow;
        return Ok(await _sessions.StartAsync(user));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<SessionResponse>> Refresh()
    {
        var session = await _sessions.RefreshAsync();
        return session == null
            ? Unauthorized(new { message = "Sessão expirada. Entre novamente." })
            : Ok(session);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _sessions.EndCurrentAsync();
        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpGet("invitations/{token}")]
    public async Task<ActionResult<InvitationInfoDto>> GetInvitation(string token)
    {
        var invitation = await _accountTokens.FindUsableAsync(token, UserTokenPurpose.Invitation);
        if (invitation == null)
            return NotFound(new { message = "Convite inválido ou expirado. Peça um novo convite ao RH." });

        var organization = await _dbContext.GetOrganizationAsync();
        return Ok(new InvitationInfoDto(invitation.User.Name, invitation.User.Email, organization.Name));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("invitations/accept")]
    public async Task<ActionResult<SessionResponse>> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var error = PasswordPolicy.Validate(request.Password);
        if (error != null)
            return BadRequest(new { message = error });

        var invitation = await _accountTokens.FindUsableAsync(request.Token, UserTokenPurpose.Invitation);
        if (invitation == null)
            return NotFound(new { message = "Convite inválido ou expirado. Peça um novo convite ao RH." });

        var user = invitation.User;
        user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        user.ActivatedAt ??= DateTime.UtcNow;
        await _accountTokens.ConsumeAsync(invitation);
        return Ok(await _sessions.StartAsync(user));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("password/forgot")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // A resposta é a mesma exista ou não a conta, para não revelar quais e-mails estão cadastrados.
        var user = await FindActiveUserByEmailAsync(request.Email);
        if (user != null && !_accountTokens.CanEmail(user))
        {
            _logger.LogInformation("Redefinição de senha pedida com o envio de e-mail desligado; nada foi enviado ao usuário {UserId}", user.Id);
        }
        else if (user != null)
        {
            try
            {
                await _accountTokens.SendPasswordResetAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de redefinição de senha para o usuário {UserId}", user.Id);
            }
        }
        return Accepted(new { message = "Se o e-mail estiver cadastrado, você vai receber um link para definir uma nova senha." });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicy)]
    [HttpPost("password/reset")]
    public async Task<ActionResult<SessionResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var error = PasswordPolicy.Validate(request.Password);
        if (error != null)
            return BadRequest(new { message = error });

        var reset = await _accountTokens.FindUsableAsync(request.Token, UserTokenPurpose.PasswordReset);
        if (reset == null)
            return NotFound(new { message = "Link inválido ou expirado. Peça uma nova redefinição de senha." });

        var user = reset.User;
        user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        user.ActivatedAt ??= DateTime.UtcNow;
        await _accountTokens.ConsumeAsync(reset);
        await _sessions.RevokeAllAsync(user.Id);
        return Ok(await _sessions.StartAsync(user));
    }

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);
        if (user == null)
            return Unauthorized();

        if (user.PasswordHash != null &&
            !_passwordHasher.VerifyPassword(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            return BadRequest(new { message = "A senha atual não confere." });
        }

        var error = PasswordPolicy.Validate(request.NewPassword);
        if (error != null)
            return BadRequest(new { message = error });

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        await _sessions.RevokeAllAsync(user.Id, keepCurrent: true);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var user = await _dbContext.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId);

        return user == null ? NotFound() : Ok(SessionService.ToProfile(user));
    }

    private async Task<User?> FindActiveUserByEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalized = email.Trim().ToLowerInvariant();
        return await _dbContext.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive);
    }

    private string? GoogleClientId()
    {
        var clientId = _configuration["Auth:Google:ClientId"];
        return string.IsNullOrWhiteSpace(clientId) ? null : clientId;
    }
}
