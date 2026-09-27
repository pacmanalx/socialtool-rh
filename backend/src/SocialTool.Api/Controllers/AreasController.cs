using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.Authorization;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Domain.Authorization;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// Estrutura da empresa: unidades, departamentos e setores em árvore, e quem está em cada área.
// Consultar a árvore é livre para quem está logado; mexer exige "Gerenciar estrutura".
[ApiController]
public class AreasController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AuditService _audit;

    public AreasController(ApplicationDbContext dbContext, AuditService audit)
    {
        _dbContext = dbContext;
        _audit = audit;
    }

    [HttpGet("api/areas")]
    public async Task<ActionResult<IEnumerable<AreaDto>>> GetAll()
    {
        var tree = await AreaTree.LoadAsync(_dbContext);
        var direct = await _dbContext.Users
            .Where(u => u.IsActive && u.DepartmentId != null)
            .GroupBy(u => u.DepartmentId!.Value)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return Ok(tree.All
            .Select(n => new AreaDto(
                n.Id, n.Name, n.Kind.ToString(), n.ParentId, tree.PathOf(n.Id),
                direct.GetValueOrDefault(n.Id),
                tree.WithDescendants([n.Id]).Sum(id => direct.GetValueOrDefault(id))))
            .OrderBy(a => a.Path, StringComparer.CurrentCultureIgnoreCase));
    }

    [HttpPost("api/admin/areas")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<ActionResult<AreaDto>> Create([FromBody] SaveAreaRequest request)
    {
        var tree = await AreaTree.LoadAsync(_dbContext);
        var (name, kind, error) = Validate(request, tree);
        if (error != null)
            return error;
        if (await NameTakenAsync(name, request.ParentId, null))
            return Conflict(new { message = "Já existe uma área com esse nome no mesmo lugar da estrutura." });

        var area = new Department { Name = name, Kind = kind, ParentDepartmentId = request.ParentId };
        _dbContext.Departments.Add(area);
        await _dbContext.SaveChangesAsync();

        tree = await AreaTree.LoadAsync(_dbContext);
        await _audit.LogAsync(AuditService.Actions.AreaCreated, $"Criou a área {tree.PathOf(area.Id)} ({KindLabel(kind)}).");
        return Ok(new AreaDto(area.Id, area.Name, kind.ToString(), area.ParentDepartmentId, tree.PathOf(area.Id), 0, 0));
    }

    [HttpPut("api/admin/areas/{id:guid}")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<ActionResult<AreaDto>> Update(Guid id, [FromBody] SaveAreaRequest request)
    {
        var area = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (area == null)
            return NotFound();

        var tree = await AreaTree.LoadAsync(_dbContext);
        var (name, kind, error) = Validate(request, tree);
        if (error != null)
            return error;
        if (request.ParentId == id || tree.WouldCycle(id, request.ParentId))
            return BadRequest(new { message = "Uma área não pode ficar dentro dela mesma nem de uma área abaixo dela." });
        if (await NameTakenAsync(name, request.ParentId, id))
            return Conflict(new { message = "Já existe uma área com esse nome no mesmo lugar da estrutura." });

        var before = tree.PathOf(id);
        area.Name = name;
        area.Kind = kind;
        area.ParentDepartmentId = request.ParentId;
        area.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        tree = await AreaTree.LoadAsync(_dbContext);
        var after = tree.PathOf(id);
        await _audit.LogAsync(AuditService.Actions.AreaUpdated,
            before == after ? $"Alterou a área {after} ({KindLabel(kind)})." : $"Alterou a área {before} → {after} ({KindLabel(kind)}).");

        var members = await _dbContext.Users.CountAsync(u => u.IsActive && u.DepartmentId == id);
        return Ok(new AreaDto(id, area.Name, kind.ToString(), area.ParentDepartmentId, after, members, members));
    }

    [HttpDelete("api/admin/areas/{id:guid}")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var area = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (area == null)
            return NotFound();
        if (await _dbContext.Departments.AnyAsync(d => d.ParentDepartmentId == id))
            return Conflict(new { message = "Mova ou apague antes as áreas que estão dentro desta." });
        if (await _dbContext.Users.AnyAsync(u => u.DepartmentId == id))
            return Conflict(new { message = "Há pessoas nesta área (inclusive desativadas). Mova-as antes de apagar." });
        if (await _dbContext.SurveyAudienceAreas.AnyAsync(a => a.DepartmentId == id))
            return Conflict(new { message = "Esta área é público de alguma enquete e não pode ser apagada." });

        var path = (await AreaTree.LoadAsync(_dbContext)).PathOf(id);
        _dbContext.Departments.Remove(area);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.AreaDeleted, $"Apagou a área {path}.");
        return NoContent();
    }

    [HttpGet("api/admin/areas/{id:guid}/members")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<ActionResult<IEnumerable<AreaMemberDto>>> GetMembers(Guid id) =>
        Ok(await _dbContext.Users
            .Where(u => u.DepartmentId == id)
            .OrderBy(u => u.Name)
            .Select(u => new AreaMemberDto(u.Id, u.Name, u.Email, u.JobTitle, u.IsActive))
            .ToListAsync());

    // Coloca as pessoas nesta área (saem da área em que estavam).
    [HttpPost("api/admin/areas/{id:guid}/members")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<IActionResult> AddMembers(Guid id, [FromBody] AreaMembersRequest request)
    {
        if (!await _dbContext.Departments.AnyAsync(d => d.Id == id))
            return NotFound();
        var ids = (request.UserIds ?? []).Distinct().ToList();
        if (ids.Count is 0 or > 500)
            return BadRequest(new { message = "Selecione de 1 a 500 pessoas." });

        var users = await _dbContext.Users.Where(u => ids.Contains(u.Id) && u.DepartmentId != id).ToListAsync();
        users.ForEach(u => u.DepartmentId = id);
        await _dbContext.SaveChangesAsync();

        if (users.Count > 0)
        {
            var path = (await AreaTree.LoadAsync(_dbContext)).PathOf(id);
            var names = string.Join(", ", users.Take(10).Select(u => u.Name)) + (users.Count > 10 ? $" e mais {users.Count - 10}" : "");
            await _audit.LogAsync(AuditService.Actions.AreaMembersChanged, $"Colocou em {path}: {names}.");
        }
        return NoContent();
    }

    [HttpDelete("api/admin/areas/{id:guid}/members/{userId:guid}")]
    [RequirePermission(Permissions.StructureManage)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DepartmentId == id);
        if (user == null)
            return NotFound();

        user.DepartmentId = null;
        await _dbContext.SaveChangesAsync();
        var path = (await AreaTree.LoadAsync(_dbContext)).PathOf(id);
        await _audit.LogAsync(AuditService.Actions.AreaMembersChanged, $"Tirou {user.Name} de {path}.", user);
        return NoContent();
    }

    private (string Name, AreaKind Kind, ActionResult? Error) Validate(SaveAreaRequest request, AreaTree tree)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 100)
            return (name, default, BadRequest(new { message = "Informe o nome da área (até 100 caracteres)." }));
        if (!Enum.TryParse<AreaKind>(request.Kind, ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return (name, default, BadRequest(new { message = "Tipo de área inválido." }));
        if (request.ParentId is { } parent && !tree.Exists(parent))
            return (name, kind, BadRequest(new { message = "A área de cima não existe." }));
        return (name, kind, null);
    }

    private Task<bool> NameTakenAsync(string name, Guid? parentId, Guid? exceptId) =>
        _dbContext.Departments.AnyAsync(d => d.ParentDepartmentId == parentId && d.Name == name && d.Id != exceptId);

    public static string KindLabel(AreaKind kind) => kind switch
    {
        AreaKind.Unit => "unidade",
        AreaKind.Sector => "setor",
        _ => "departamento"
    };
}
