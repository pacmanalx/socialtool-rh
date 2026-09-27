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

// Conceder e retirar permissões é exclusivo do Admin, e só vale para gestores de RH.
[ApiController]
[Route("api/admin/permissions")]
[RequireAdmin]
public class AdminPermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly AuditService _audit;

    public AdminPermissionsController(ApplicationDbContext dbContext, ICurrentUserService currentUser, AuditService audit)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _audit = audit;
    }

    [HttpGet("catalog")]
    public ActionResult<IEnumerable<PermissionDefinitionDto>> GetCatalog() =>
        Ok(Permissions.Catalog.Select(p => new PermissionDefinitionDto(p.Key, p.Group, p.Label, p.Description, p.Available)));

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<UserPermissionsDto>> GetForUser(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("users/{id:guid}")]
    public async Task<ActionResult<UserPermissionsDto>> UpdateForUser(Guid id, [FromBody] UpdateUserPermissionsRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (user.Role != UserRole.HR)
            return BadRequest(new { message = "Permissões administrativas só valem para gestores de RH. Mude o papel da pessoa primeiro." });

        var requested = (request.Permissions ?? []).Distinct().ToList();
        var unknown = requested.FirstOrDefault(p => !Permissions.IsKnown(p));
        if (unknown != null)
            return BadRequest(new { message = $"Permissão desconhecida: {unknown}." });

        var current = await _dbContext.UserPermissions.Where(p => p.UserId == id).ToListAsync();
        var toRemove = current.Where(p => !requested.Contains(p.Permission)).ToList();
        var toAdd = requested.Where(p => current.All(c => c.Permission != p)).ToList();
        if (toRemove.Count == 0 && toAdd.Count == 0)
            return Ok(await ToDtoAsync(user));

        _dbContext.UserPermissions.RemoveRange(toRemove);
        foreach (var permission in toAdd)
            _dbContext.UserPermissions.Add(new UserPermission { UserId = id, Permission = permission, GrantedById = _currentUser.UserId });
        await _dbContext.SaveChangesAsync();

        var parts = new List<string>();
        if (toAdd.Count > 0) parts.Add("concedeu " + string.Join(", ", toAdd.Select(Label)));
        if (toRemove.Count > 0) parts.Add("retirou " + string.Join(", ", toRemove.Select(p => Label(p.Permission))));
        await _audit.LogAsync(AuditService.Actions.PermissionsChanged, $"Permissões de {user.Email}: {string.Join("; ", parts)}.", user);

        return Ok(await ToDtoAsync(user));
    }

    private async Task<UserPermissionsDto> ToDtoAsync(User user) => new(
        user.Id,
        user.Role.ToString(),
        user.Role == UserRole.HR,
        await PermissionService.GetEffectiveForAsync(_dbContext, user.Id, user.Role));

    private static string Label(string key) =>
        Permissions.Catalog.FirstOrDefault(p => p.Key == key)?.Label ?? key;
}
