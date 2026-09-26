using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public UsersController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers()
    {
        var users = await _dbContext.Users
            .Include(u => u.Department)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Name,
                u.Email,
                u.JobTitle,
                u.AvatarUrl,
                u.Department != null ? u.Department.Name : null
            ))
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpPost("mood")]
    public async Task<ActionResult> SubmitDailyMood([FromBody] MoodCheckinRequest request)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUser.UserId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await _dbContext.DailyMoods
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == today);

        if (existing != null)
        {
            existing.Score = (MoodScore)request.Score;
            existing.Note = request.Note;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var mood = new DailyMood
            {
                UserId = userId,
                Score = (MoodScore)request.Score,
                Note = request.Note,
                Date = today,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.DailyMoods.Add(mood);
        }

        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Humor registrado com sucesso!" });
    }

    [Authorize]
    [HttpGet("mood/today")]
    public async Task<ActionResult<MoodSummaryDto?>> GetTodayMood()
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var userId = _currentUser.UserId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var mood = await _dbContext.DailyMoods
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Date == today);

        if (mood == null)
            return Ok(null);

        return Ok(new MoodSummaryDto((int)mood.Score, mood.Note, mood.Date));
    }
}
