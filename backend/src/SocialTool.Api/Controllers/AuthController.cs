using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        ApplicationDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _dbContext.Users
            .Include(u => u.Tenant)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.IsActive);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        var token = _jwtTokenService.GenerateToken(user, user.Tenant.Subdomain);

        var profile = new UserProfileDto(
            user.Id,
            user.TenantId,
            user.Name,
            user.Email,
            user.JobTitle,
            user.Role.ToString(),
            user.AvatarUrl,
            user.CoinsAvailableToGive,
            user.CoinsBalanceToSpend,
            user.Department?.Name
        );

        var tenantDto = new TenantDto(
            user.Tenant.Id,
            user.Tenant.Name,
            user.Tenant.Subdomain,
            user.Tenant.CurrencyName,
            user.Tenant.MonthlyCoinsQuota
        );

        return Ok(new LoginResponse(token, profile, tenantDto));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        if (!_currentUserService.UserId.HasValue)
            return Unauthorized();

        var user = await _dbContext.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value);

        if (user == null)
            return NotFound();

        return Ok(new UserProfileDto(
            user.Id,
            user.TenantId,
            user.Name,
            user.Email,
            user.JobTitle,
            user.Role.ToString(),
            user.AvatarUrl,
            user.CoinsAvailableToGive,
            user.CoinsBalanceToSpend,
            user.Department?.Name
        ));
    }

    [HttpGet("demo-users")]
    public async Task<ActionResult<IEnumerable<object>>> GetDemoUsers()
    {
        var users = await _dbContext.Users
            .Include(u => u.Department)
            .OrderBy(u => u.Role)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.JobTitle,
                Role = u.Role.ToString(),
                u.AvatarUrl,
                Department = u.Department != null ? u.Department.Name : null,
                DefaultPassword = "123456"
            })
            .ToListAsync();

        return Ok(users);
    }
}
