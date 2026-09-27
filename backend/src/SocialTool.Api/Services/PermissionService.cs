using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Authorization;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

// Responde "o usuário atual pode fazer X?". Lê do banco uma vez por requisição, e não do token:
// conceder ou revogar vale na hora, sem esperar o access token vencer.
public class PermissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private (UserRole Role, IReadOnlySet<string> Granted)? _cached;

    public PermissionService(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<bool> IsAdminAsync() => (await LoadAsync())?.Role == UserRole.Admin;

    public async Task<bool> HasAsync(string permission)
    {
        var loaded = await LoadAsync();
        return loaded != null && (loaded.Value.Role == UserRole.Admin || loaded.Value.Granted.Contains(permission));
    }

    // Administradores recebem o catálogo inteiro; os demais, só o que foi concedido.
    public async Task<IReadOnlyCollection<string>> GetEffectiveAsync()
    {
        var loaded = await LoadAsync();
        if (loaded == null)
            return [];
        return loaded.Value.Role == UserRole.Admin ? Permissions.All.ToList() : loaded.Value.Granted.ToList();
    }

    public static async Task<List<string>> GetEffectiveForAsync(ApplicationDbContext dbContext, Guid userId, UserRole role)
    {
        if (role == UserRole.Admin)
            return Permissions.All.ToList();
        return await GrantedQuery(dbContext, userId).ToListAsync();
    }

    private async Task<(UserRole Role, IReadOnlySet<string> Granted)?> LoadAsync()
    {
        if (_cached != null)
            return _cached;
        if (_currentUser.UserId is not { } userId)
            return null;

        // O papel também vem do banco: um RH rebaixado perde os poderes na hora, mesmo com o token antigo.
        var role = await _dbContext.Users.Where(u => u.Id == userId && u.IsActive).Select(u => (UserRole?)u.Role).FirstOrDefaultAsync();
        if (role == null)
            return null;

        var granted = role == UserRole.Admin
            ? new HashSet<string>()
            : (await GrantedQuery(_dbContext, userId).ToListAsync()).ToHashSet();
        _cached = (role.Value, granted);
        return _cached;
    }

    // Só vale para quem é RH: se o papel mudar, as concessões param de valer mesmo antes de serem apagadas.
    private static IQueryable<string> GrantedQuery(ApplicationDbContext dbContext, Guid userId) =>
        dbContext.UserPermissions
            .Where(p => p.UserId == userId && p.User.Role == UserRole.HR)
            .Select(p => p.Permission);
}
