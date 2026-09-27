using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// O lado de quem responde: só vê enquetes abertas das quais faz parte do público.
[ApiController]
[Route("api/surveys")]
public class SurveysController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;

    public SurveysController(ApplicationDbContext dbContext, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MySurveyDto>>> GetMine()
    {
        var (userId, areaId) = await CurrentAsync();
        var now = DateTime.UtcNow;
        var candidates = await _dbContext.Surveys
            .Include(s => s.Audience)
            .AsNoTracking()
            .Where(s => s.PublishedAt != null && s.ClosedAt == null && s.StartsAt <= now && s.EndsAt > now)
            .OrderBy(s => s.EndsAt)
            .ToListAsync();
        var tree = await AreaTree.LoadAsync(_dbContext);
        var mine = candidates.Where(s => SurveyAudience.Includes(s, areaId, tree)).ToList();
        var ids = mine.Select(s => s.Id).ToList();
        var answered = (await _dbContext.SurveyParticipations
            .Where(p => p.UserId == userId && ids.Contains(p.SurveyId))
            .Select(p => p.SurveyId)
            .ToListAsync()).ToHashSet();
        var questionCounts = await _dbContext.SurveyQuestions.Where(q => ids.Contains(q.SurveyId))
            .GroupBy(q => q.SurveyId).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return Ok(mine.Select(s => new MySurveyDto(
            s.Id, s.Title, s.Description, s.EndsAt, s.IsAnonymous, questionCounts.GetValueOrDefault(s.Id), answered.Contains(s.Id))));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SurveyToAnswerDto>> Get(Guid id)
    {
        var (userId, areaId) = await CurrentAsync();
        var survey = await LoadOpenForAsync(id, areaId);
        if (survey == null)
            return NotFound(new { message = "Enquete não encontrada ou fora do prazo." });

        var answered = await _dbContext.SurveyParticipations.AnyAsync(p => p.SurveyId == id && p.UserId == userId);
        return Ok(new SurveyToAnswerDto(
            survey.Id, survey.Title, survey.Description, survey.EndsAt, survey.IsAnonymous, answered,
            survey.Questions.OrderBy(q => q.Order).Select(AdminSurveysController.ToDto).ToList()));
    }

    [HttpPost("{id:guid}/responses")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitSurveyRequest request)
    {
        var (userId, areaId) = await CurrentAsync();
        var survey = await LoadOpenForAsync(id, areaId);
        if (survey == null)
            return NotFound(new { message = "Enquete não encontrada ou fora do prazo." });
        if (await _dbContext.SurveyParticipations.AnyAsync(p => p.SurveyId == id && p.UserId == userId))
            return Conflict(new { message = "Você já respondeu esta enquete." });

        // Todas as perguntas são obrigatórias, uma resposta por pergunta, e a opção precisa ser da pergunta.
        var given = (request.Answers ?? []).GroupBy(a => a.QuestionId).ToDictionary(g => g.Key, g => g.First().OptionId);
        var answers = new List<SurveyAnswer>();
        foreach (var question in survey.Questions.OrderBy(q => q.Order))
        {
            if (!given.TryGetValue(question.Id, out var optionId))
                return BadRequest(new { message = $"Responda a pergunta {question.Order + 1}: {question.Text}" });
            var option = question.Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null)
                return BadRequest(new { message = $"Resposta inválida na pergunta {question.Order + 1}." });
            answers.Add(new SurveyAnswer { QuestionId = question.Id, OptionId = option.Id, Value = option.Value });
        }

        var now = DateTime.UtcNow;
        // Anônima: sem pessoa e só com a data (na resposta e em cada item), para não cruzar com o horário da participação.
        var stamp = survey.IsAnonymous ? now.Date : now;
        answers.ForEach(x => x.CreatedAt = stamp);
        _dbContext.SurveyParticipations.Add(new SurveyParticipation { SurveyId = id, UserId = userId, CreatedAt = now });
        _dbContext.SurveyResponses.Add(new SurveyResponse
        {
            SurveyId = id,
            UserId = survey.IsAnonymous ? null : userId,
            CreatedAt = stamp,
            AreaId = areaId,
            Answers = answers
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Duas abas enviando ao mesmo tempo: o índice único da participação barra a segunda.
            return Conflict(new { message = "Você já respondeu esta enquete." });
        }
        return NoContent();
    }

    private async Task<(Guid UserId, Guid? AreaId)> CurrentAsync()
    {
        var userId = _currentUser.UserId!.Value;
        var areaId = await _dbContext.Users.Where(u => u.Id == userId).Select(u => u.DepartmentId).FirstOrDefaultAsync();
        return (userId, areaId);
    }

    private async Task<Survey?> LoadOpenForAsync(Guid id, Guid? areaId)
    {
        var survey = await _dbContext.Surveys
            .Include(s => s.Audience)
            .Include(s => s.Questions).ThenInclude(q => q.Options)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
        if (survey == null || survey.StatusAt(DateTime.UtcNow) != SurveyStatus.Open)
            return null;
        return SurveyAudience.Includes(survey, areaId, await AreaTree.LoadAsync(_dbContext)) ? survey : null;
    }
}
