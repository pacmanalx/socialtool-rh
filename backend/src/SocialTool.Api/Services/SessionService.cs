using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Infrastructure.Persistence;
using SocialTool.Infrastructure.Services;

namespace SocialTool.Api.Services;

// Emite o access token (curto, vai no corpo) e o refresh token (rotativo, vai em cookie httpOnly).
public class SessionService
{
    public const string RefreshCookieName = "st_refresh";
    private const string RefreshCookiePath = "/api/auth";
    // Duas abas renovando ao mesmo tempo apresentam o mesmo token: dentro dessa janela não é tratado como roubo.
    private static readonly TimeSpan ConcurrentRefreshGrace = TimeSpan.FromSeconds(30);

    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(
        ApplicationDbContext db,
        IJwtTokenService jwt,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _jwt = jwt;
        _configuration = configuration;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext Http => _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("SessionService usado fora de uma requisição.");

    // O usuário precisa vir com Department carregado.
    public async Task<SessionResponse> StartAsync(User user)
    {
        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        var raw = AddRefreshToken(user.Id, now);
        await _db.SaveChangesAsync();
        return await BuildResponseAsync(user, raw.Value, raw.Key.ExpiresAt);
    }

    public async Task<SessionResponse?> RefreshAsync()
    {
        var presented = Http.Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(presented))
            return null;

        var now = DateTime.UtcNow;
        var hash = SecureToken.Hash(presented);
        var current = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (current == null || current.ExpiresAt <= now || !current.User.IsActive)
        {
            ClearCookie();
            return null;
        }

        if (current.RevokedAt != null)
        {
            var rotatedMomentsAgo = current.ReplacedByTokenId != null && now - current.RevokedAt.Value < ConcurrentRefreshGrace;
            if (!rotatedMomentsAgo)
            {
                // Token já rotacionado reapresentado: trata como vazamento e derruba todas as sessões do usuário.
                await RevokeAllAsync(current.UserId);
                ClearCookie();
                return null;
            }
        }
        else
        {
            current.RevokedAt = now;
        }

        var replacement = AddRefreshToken(current.UserId, now);
        current.ReplacedByTokenId ??= replacement.Key.Id;
        await _db.SaveChangesAsync();
        return await BuildResponseAsync(current.User, replacement.Value, replacement.Key.ExpiresAt);
    }

    public async Task EndCurrentAsync()
    {
        var presented = Http.Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(presented))
        {
            var hash = SecureToken.Hash(presented);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null);
            if (token != null)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        ClearCookie();
    }

    // Derruba as sessões do usuário, opcionalmente preservando a sessão da requisição atual.
    public async Task RevokeAllAsync(Guid userId, bool keepCurrent = false)
    {
        var presented = keepCurrent ? Http.Request.Cookies[RefreshCookieName] : null;
        var keepHash = string.IsNullOrEmpty(presented) ? null : SecureToken.Hash(presented);
        var now = DateTime.UtcNow;

        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync();
        foreach (var token in active.Where(t => t.TokenHash != keepHash))
            token.RevokedAt = now;

        await _db.SaveChangesAsync();
    }

    public static UserProfileDto ToProfile(User user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.JobTitle,
        user.Role.ToString(),
        user.AvatarUrl,
        user.CoinsAvailableToGive,
        user.CoinsBalanceToSpend,
        user.Department?.Name,
        user.PasswordHash != null);

    private KeyValuePair<RefreshToken, string> AddRefreshToken(Guid userId, DateTime now)
    {
        var days = int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var d) ? d : 14;
        var raw = SecureToken.Generate();
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = SecureToken.Hash(raw),
            ExpiresAt = now.AddDays(days),
            CreatedByIp = Http.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(Http.Request.Headers.UserAgent.ToString(), 300)
        };
        _db.RefreshTokens.Add(token);
        return new KeyValuePair<RefreshToken, string>(token, raw);
    }

    private async Task<SessionResponse> BuildResponseAsync(User user, string rawRefreshToken, DateTime refreshExpiresAt)
    {
        Http.Response.Cookies.Append(RefreshCookieName, rawRefreshToken, CookieOptions(refreshExpiresAt));

        var access = _jwt.GenerateAccessToken(user);
        var organization = await _db.GetOrganizationAsync();
        return new SessionResponse(
            access.Token,
            access.ExpiresAt,
            ToProfile(user),
            new OrganizationDto(organization.Name, organization.CurrencyName, organization.MonthlyCoinsQuota));
    }

    private void ClearCookie() =>
        Http.Response.Cookies.Delete(RefreshCookieName, CookieOptions(null));

    private CookieOptions CookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = Http.Request.IsHttps || !_environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = RefreshCookiePath,
        Expires = expiresAt
    };

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];
}
