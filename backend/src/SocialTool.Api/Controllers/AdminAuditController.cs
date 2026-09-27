using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.Authorization;
using SocialTool.Api.DTOs;
using SocialTool.Domain.Authorization;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// Consulta do registro de auditoria. Não há endpoint para alterar ou apagar registros.
[ApiController]
[Route("api/admin/audit")]
[RequirePermission(Permissions.AuditView)]
public class AdminAuditController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly ApplicationDbContext _dbContext;

    public AdminAuditController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<AuditPageDto>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? search = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.StartsWith(action));
        // Envolvida na ação: quem fez ou sobre quem foi feito.
        if (userId.HasValue)
            query = query.Where(a => a.ActorId == userId || a.TargetUserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.Summary.Contains(term) || a.ActorName.Contains(term)
                || (a.TargetName != null && a.TargetName.Contains(term)) || (a.Reason != null && a.Reason.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.CreatedAt, a.ActorId, a.ActorName, a.Action, a.TargetUserId, a.TargetName, a.Summary, a.Reason))
            .ToListAsync();

        return Ok(new AuditPageDto(items, total, page, pageSize));
    }
}
