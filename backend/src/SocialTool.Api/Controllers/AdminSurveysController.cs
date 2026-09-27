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

// Criar, programar, acompanhar e encerrar enquetes, e manter as escalas de resposta.
[ApiController]
[Route("api/admin")]
[RequirePermission(Permissions.SurveysManage)]
public class AdminSurveysController : ControllerBase
{
    // Recorte por área só com pelo menos este número de respostas: abaixo disso daria para identificar pessoas.
    public const int MinimumGroup = 4;
    private const int MaxQuestions = 50;

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly AuditService _audit;

    public AdminSurveysController(ApplicationDbContext dbContext, ICurrentUserService currentUser, AuditService audit)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _audit = audit;
    }

    // ---------------------------------------------------------------- escalas

    [HttpGet("answer-scales")]
    public async Task<ActionResult<IEnumerable<ScaleDto>>> GetScales() =>
        Ok((await _dbContext.AnswerScales.Include(s => s.Options).AsNoTracking().ToListAsync())
            .OrderByDescending(s => s.IsSystem).ThenBy(s => s.Name)
            .Select(ToDto));

    [HttpPost("answer-scales")]
    public async Task<ActionResult<ScaleDto>> CreateScale([FromBody] SaveScaleRequest request)
    {
        var (name, options, error) = ValidateScale(request);
        if (error != null)
            return error;
        if (await _dbContext.AnswerScales.AnyAsync(s => s.Name == name))
            return Conflict(new { message = "Já existe uma escala com esse nome." });

        var scale = new AnswerScale { Name = name, IsNps = request.IsNps, CreatedById = _currentUser.UserId, Options = options };
        _dbContext.AnswerScales.Add(scale);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.ScaleChanged, $"Criou a escala \"{name}\" ({DescribeOptions(options)}).");
        return Ok(ToDto(scale));
    }

    // Mudar uma escala não mexe nas enquetes que já a usaram: cada pergunta guarda a sua cópia das opções.
    [HttpPut("answer-scales/{id:guid}")]
    public async Task<ActionResult<ScaleDto>> UpdateScale(Guid id, [FromBody] SaveScaleRequest request)
    {
        var scale = await _dbContext.AnswerScales.Include(s => s.Options).FirstOrDefaultAsync(s => s.Id == id);
        if (scale == null)
            return NotFound();
        if (scale.IsSystem)
            return BadRequest(new { message = "As escalas do sistema não podem ser alteradas. Crie uma nova a partir dela." });
        var (name, options, error) = ValidateScale(request);
        if (error != null)
            return error;
        if (await _dbContext.AnswerScales.AnyAsync(s => s.Name == name && s.Id != id))
            return Conflict(new { message = "Já existe uma escala com esse nome." });

        _dbContext.AnswerScaleOptions.RemoveRange(scale.Options);
        scale.Name = name;
        scale.IsNps = request.IsNps;
        scale.Options = options;
        scale.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.ScaleChanged, $"Alterou a escala \"{name}\" ({DescribeOptions(options)}).");
        return Ok(ToDto(scale));
    }

    [HttpDelete("answer-scales/{id:guid}")]
    public async Task<IActionResult> DeleteScale(Guid id)
    {
        var scale = await _dbContext.AnswerScales.FirstOrDefaultAsync(s => s.Id == id);
        if (scale == null)
            return NotFound();
        if (scale.IsSystem)
            return BadRequest(new { message = "As escalas do sistema não podem ser apagadas." });

        _dbContext.AnswerScales.Remove(scale);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.ScaleChanged, $"Apagou a escala \"{scale.Name}\".");
        return NoContent();
    }

    // ---------------------------------------------------------------- enquetes

    [HttpGet("surveys")]
    public async Task<ActionResult<IEnumerable<SurveySummaryDto>>> GetSurveys()
    {
        var surveys = await _dbContext.Surveys
            .Include(s => s.Audience)
            .Include(s => s.CreatedBy)
            .AsNoTracking()
            .OrderByDescending(s => s.StartsAt)
            .ToListAsync();
        var questionCounts = await _dbContext.SurveyQuestions.GroupBy(q => q.SurveyId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var responses = await _dbContext.SurveyParticipations.GroupBy(p => p.SurveyId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var tree = await AreaTree.LoadAsync(_dbContext);
        var now = DateTime.UtcNow;

        var result = new List<SurveySummaryDto>();
        foreach (var s in surveys)
        {
            result.Add(new SurveySummaryDto(
                s.Id, s.Title, s.StatusAt(now).ToString(), s.StartsAt, s.EndsAt, s.IsAnonymous, s.AudienceAll,
                SurveyAudience.Labels(s, tree), questionCounts.GetValueOrDefault(s.Id), responses.GetValueOrDefault(s.Id),
                await SurveyAudience.CountAsync(_dbContext, s, tree), s.CreatedBy.Name));
        }
        return Ok(result);
    }

    [HttpGet("surveys/{id:guid}")]
    public async Task<ActionResult<SurveyDetailDto>> GetSurvey(Guid id)
    {
        var survey = await LoadSurveyAsync(id);
        return survey == null ? NotFound() : Ok(await ToDetailAsync(survey));
    }

    [HttpPost("surveys")]
    public async Task<ActionResult<SurveyDetailDto>> CreateSurvey([FromBody] SaveSurveyRequest request)
    {
        var error = await ValidateSurveyAsync(request, existing: null);
        if (error != null)
            return error;

        var survey = new Survey { CreatedById = _currentUser.UserId!.Value };
        ApplyEditable(survey, request);
        ApplyStructure(survey, request);
        _dbContext.Surveys.Add(survey);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.SurveyCreated, $"Criou a enquete \"{survey.Title}\" (rascunho).");
        return Ok(await ToDetailAsync((await LoadSurveyAsync(survey.Id))!));
    }

    // Depois da primeira resposta, só título, descrição e data de término mudam: perguntas, público e
    // anonimato ficam travados para não distorcer o que já foi respondido.
    [HttpPut("surveys/{id:guid}")]
    public async Task<ActionResult<SurveyDetailDto>> UpdateSurvey(Guid id, [FromBody] SaveSurveyRequest request)
    {
        var survey = await LoadSurveyAsync(id, tracking: true);
        if (survey == null)
            return NotFound();
        if (survey.StatusAt(DateTime.UtcNow) == SurveyStatus.Closed)
            return Conflict(new { message = "A enquete já foi encerrada e não pode mais ser alterada." });

        var locked = await _dbContext.SurveyParticipations.AnyAsync(p => p.SurveyId == id);
        var error = await ValidateSurveyAsync(request, survey, locked);
        if (error != null)
            return error;

        ApplyEditable(survey, request, keepStart: locked || survey.StatusAt(DateTime.UtcNow) == SurveyStatus.Open);
        if (!locked)
        {
            _dbContext.SurveyAudienceAreas.RemoveRange(survey.Audience);
            _dbContext.SurveyQuestions.RemoveRange(survey.Questions);
            survey.Audience = new List<SurveyAudienceArea>();
            survey.Questions = new List<SurveyQuestion>();
            ApplyStructure(survey, request);
        }
        survey.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.SurveyUpdated, $"Alterou a enquete \"{survey.Title}\".");
        return Ok(await ToDetailAsync((await LoadSurveyAsync(id))!));
    }

    [HttpPost("surveys/{id:guid}/publish")]
    public async Task<ActionResult<SurveyDetailDto>> Publish(Guid id)
    {
        var survey = await LoadSurveyAsync(id, tracking: true);
        if (survey == null)
            return NotFound();
        if (survey.PublishedAt != null)
            return Conflict(new { message = "A enquete já foi publicada." });
        if (survey.Questions.Count == 0)
            return BadRequest(new { message = "Inclua pelo menos uma pergunta antes de publicar." });
        if (survey.EndsAt <= DateTime.UtcNow)
            return BadRequest(new { message = "A data de término já passou. Ajuste as datas antes de publicar." });

        survey.PublishedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        var when = survey.StartsAt <= DateTime.UtcNow ? "aberta agora" : "agendada";
        await _audit.LogAsync(AuditService.Actions.SurveyPublished, $"Publicou a enquete \"{survey.Title}\" ({when}).");
        return Ok(await ToDetailAsync((await LoadSurveyAsync(id))!));
    }

    [HttpPost("surveys/{id:guid}/close")]
    public async Task<ActionResult<SurveyDetailDto>> Close(Guid id)
    {
        var survey = await LoadSurveyAsync(id, tracking: true);
        if (survey == null)
            return NotFound();
        var status = survey.StatusAt(DateTime.UtcNow);
        if (status is not (SurveyStatus.Open or SurveyStatus.Scheduled))
            return Conflict(new { message = "Só enquetes abertas ou agendadas podem ser encerradas." });

        survey.ClosedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.SurveyClosed,
            status == SurveyStatus.Scheduled ? $"Cancelou a enquete agendada \"{survey.Title}\"." : $"Encerrou antes do prazo a enquete \"{survey.Title}\".");
        return Ok(await ToDetailAsync((await LoadSurveyAsync(id))!));
    }

    [HttpDelete("surveys/{id:guid}")]
    public async Task<IActionResult> DeleteSurvey(Guid id)
    {
        var survey = await _dbContext.Surveys.FirstOrDefaultAsync(s => s.Id == id);
        if (survey == null)
            return NotFound();
        if (await _dbContext.SurveyParticipations.AnyAsync(p => p.SurveyId == id))
            return Conflict(new { message = "A enquete já tem respostas e não pode ser apagada. Encerre-a em vez disso." });

        _dbContext.Surveys.Remove(survey);
        await _dbContext.SaveChangesAsync();
        await _audit.LogAsync(AuditService.Actions.SurveyDeleted, $"Apagou a enquete \"{survey.Title}\".");
        return NoContent();
    }

    // ---------------------------------------------------------------- resultado

    [HttpGet("surveys/{id:guid}/results")]
    public async Task<ActionResult<SurveyResultsDto>> GetResults(Guid id, [FromQuery] Guid? areaId = null)
    {
        var survey = await LoadSurveyAsync(id);
        if (survey == null)
            return NotFound();
        var tree = await AreaTree.LoadAsync(_dbContext);
        if (areaId is { } a && !tree.Exists(a))
            return BadRequest(new { message = "Área não encontrada." });

        var responses = await _dbContext.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == id)
            .Select(r => new { r.Id, r.AreaId })
            .ToListAsync();
        var participants = await _dbContext.SurveyParticipations.CountAsync(p => p.SurveyId == id);
        var audienceSize = await SurveyAudience.CountAsync(_dbContext, survey, tree);

        // Respostas por área (a área da pessoa quando respondeu). Grupos pequenos não aparecem nominalmente.
        var byArea = responses.GroupBy(r => r.AreaId).Select(g => new { AreaId = g.Key, Count = g.Count() }).ToList();
        var areas = byArea.Where(g => g.Count >= MinimumGroup && g.AreaId != null)
            .Select(g => new AreaResponsesDto(g.AreaId, tree.PathOf(g.AreaId!.Value), g.Count))
            .OrderBy(g => g.Label)
            .ToList();
        var small = byArea.Where(g => g.Count < MinimumGroup || g.AreaId == null).Sum(g => g.Count);
        if (small > 0)
            areas.Add(new AreaResponsesDto(null, $"Outras áreas e sem área (grupos com menos de {MinimumGroup})", small));

        var selected = responses;
        string? areaLabel = null;
        if (areaId is { } area)
        {
            var scope = tree.WithDescendants([area]);
            selected = responses.Where(r => r.AreaId is { } ra && scope.Contains(ra)).ToList();
            areaLabel = tree.PathOf(area);
        }
        var suppressed = areaId != null && selected.Count < MinimumGroup;
        var selectedIds = selected.Select(r => r.Id).ToHashSet();

        var answers = suppressed
            ? []
            : (await _dbContext.SurveyAnswers.AsNoTracking()
                .Where(x => x.Response.SurveyId == id)
                .Select(x => new { x.ResponseId, x.QuestionId, x.OptionId, x.Value })
                .ToListAsync())
                .Where(x => selectedIds.Contains(x.ResponseId))
                .ToList();

        var questions = survey.Questions.OrderBy(q => q.Order).Select(q =>
        {
            var qa = answers.Where(x => x.QuestionId == q.Id).ToList();
            var valued = qa.Where(x => x.Value != null).Select(x => x.Value!.Value).ToList();
            var options = q.Options.OrderBy(o => o.Order).Select(o =>
            {
                var count = qa.Count(x => x.OptionId == o.Id);
                return new OptionResultDto(o.Label, o.Value, count, Percent(count, qa.Count));
            }).ToList();

            NpsResultDto? nps = null;
            if (q.IsNps && valued.Count > 0)
            {
                int promoters = valued.Count(v => v >= 9), detractors = valued.Count(v => v <= 6);
                nps = new NpsResultDto(Math.Round((promoters - detractors) * 100m / valued.Count, 1),
                    promoters, valued.Count - promoters - detractors, detractors);
            }

            return new QuestionResultDto(q.Id, q.Text, q.ScaleName, q.IsNps, qa.Count,
                valued.Count > 0 ? Math.Round(valued.Average(), 2) : null, nps, options);
        }).ToList();

        return Ok(new SurveyResultsDto(
            survey.Id, survey.Title, survey.StatusAt(DateTime.UtcNow).ToString(), survey.IsAnonymous,
            audienceSize, participants, Percent(participants, audienceSize),
            areaId, areaLabel, MinimumGroup, suppressed, suppressed ? 0 : selected.Count, questions, areas));
    }

    // Respostas de cada pessoa, só em enquete identificada. Exige a permissão de dado individual e um motivo,
    // que fica na auditoria. Em enquete anônima não existe essa ligação no banco.
    [HttpPost("surveys/{id:guid}/individual")]
    [RequirePermission(Permissions.SensitiveViewIndividual)]
    public async Task<ActionResult<IEnumerable<IndividualResponseDto>>> GetIndividual(Guid id, [FromBody] IndividualAccessRequest request)
    {
        var survey = await LoadSurveyAsync(id);
        if (survey == null)
            return NotFound();
        if (survey.IsAnonymous)
            return BadRequest(new { message = "Enquete anônima: as respostas não são ligadas a ninguém." });
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 10)
            return BadRequest(new { message = "Informe o motivo do acesso (pelo menos 10 caracteres). Ele fica registrado na auditoria." });

        var tree = await AreaTree.LoadAsync(_dbContext);
        var questions = survey.Questions.ToDictionary(q => q.Id);
        var options = survey.Questions.SelectMany(q => q.Options).ToDictionary(o => o.Id);
        var responses = await _dbContext.SurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == id && r.UserId != null)
            .Include(r => r.User)
            .Include(r => r.Answers)
            .OrderBy(r => r.User!.Name)
            .ToListAsync();

        await _audit.LogAsync(AuditService.Actions.SensitiveAccessed,
            $"Abriu as respostas individuais da enquete \"{survey.Title}\" ({responses.Count} pessoas).", reason: reason);

        return Ok(responses.Select(r => new IndividualResponseDto(
            r.User!.Name, r.User.Email, r.AreaId is { } a ? tree.PathOf(a) : null, r.CreatedAt,
            r.Answers
                .Where(x => questions.ContainsKey(x.QuestionId))
                .OrderBy(x => questions[x.QuestionId].Order)
                .Select(x => new IndividualAnswerDto(questions[x.QuestionId].Text,
                    options.TryGetValue(x.OptionId, out var o) ? o.Label : "?", x.Value))
                .ToList())));
    }

    // ---------------------------------------------------------------- apoio

    private async Task<Survey?> LoadSurveyAsync(Guid id, bool tracking = false)
    {
        var query = _dbContext.Surveys
            .Include(s => s.Audience)
            .Include(s => s.Questions).ThenInclude(q => q.Options)
            .AsSplitQuery();
        return await (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(s => s.Id == id);
    }

    private async Task<SurveyDetailDto> ToDetailAsync(Survey s)
    {
        var responses = await _dbContext.SurveyParticipations.CountAsync(p => p.SurveyId == s.Id);
        return new SurveyDetailDto(
            s.Id, s.Title, s.Description, s.StartsAt, s.EndsAt, s.IsAnonymous, s.AudienceAll,
            s.Audience.Select(a => a.DepartmentId).ToList(), s.StatusAt(DateTime.UtcNow).ToString(), responses, responses > 0,
            s.Questions.OrderBy(q => q.Order).Select(ToDto).ToList());
    }

    public static SurveyQuestionDto ToDto(SurveyQuestion q) => new(
        q.Id, q.Text, q.ScaleName, q.IsNps,
        q.Options.OrderBy(o => o.Order).Select(o => new SurveyOptionDto(o.Id, o.Label, o.Value)).ToList());

    private static ScaleDto ToDto(AnswerScale s) => new(
        s.Id, s.Name, s.IsNps, s.IsSystem,
        s.Options.OrderBy(o => o.Order).Select(o => new ScaleOptionDto(o.Label, o.Value)).ToList());

    private static void ApplyEditable(Survey survey, SaveSurveyRequest request, bool keepStart = false)
    {
        survey.Title = request.Title!.Trim();
        survey.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (!keepStart)
            survey.StartsAt = ToUtc(request.StartsAt);
        survey.EndsAt = ToUtc(request.EndsAt);
    }

    private static void ApplyStructure(Survey survey, SaveSurveyRequest request)
    {
        survey.IsAnonymous = request.IsAnonymous;
        survey.AudienceAll = request.AudienceAll;
        if (!request.AudienceAll)
        {
            foreach (var areaId in (request.AudienceAreaIds ?? []).Distinct())
                survey.Audience.Add(new SurveyAudienceArea { DepartmentId = areaId });
        }
        var order = 0;
        foreach (var q in request.Questions ?? [])
        {
            survey.Questions.Add(new SurveyQuestion
            {
                Order = order++,
                Text = q.Text!.Trim(),
                ScaleName = q.ScaleName!.Trim(),
                IsNps = q.IsNps,
                Options = q.Options!.Select((o, i) => new SurveyQuestionOption { Order = i, Label = o.Label.Trim(), Value = o.Value }).ToList()
            });
        }
    }

    private async Task<ActionResult?> ValidateSurveyAsync(SaveSurveyRequest request, Survey? existing, bool locked = false)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is 0 or > 200)
            return BadRequest(new { message = "Informe o título (até 200 caracteres)." });
        if ((request.Description?.Length ?? 0) > 2000)
            return BadRequest(new { message = "A descrição pode ter até 2000 caracteres." });
        var starts = existing != null && (locked || existing.StatusAt(DateTime.UtcNow) == SurveyStatus.Open) ? existing.StartsAt : ToUtc(request.StartsAt);
        if (ToUtc(request.EndsAt) <= starts)
            return BadRequest(new { message = "O término precisa ser depois do início." });
        if (existing?.PublishedAt != null && ToUtc(request.EndsAt) <= DateTime.UtcNow)
            return BadRequest(new { message = "O término de uma enquete publicada precisa ficar no futuro. Para encerrar agora, use Encerrar." });
        if (locked)
            return null;

        if (!request.AudienceAll)
        {
            var ids = (request.AudienceAreaIds ?? []).Distinct().ToList();
            if (ids.Count == 0)
                return BadRequest(new { message = "Escolha pelo menos uma área, ou marque a empresa inteira." });
            if (await _dbContext.Departments.CountAsync(d => ids.Contains(d.Id)) != ids.Count)
                return BadRequest(new { message = "Alguma das áreas escolhidas não existe mais." });
        }

        var questions = request.Questions ?? [];
        if (questions.Count > MaxQuestions)
            return BadRequest(new { message = $"Uma enquete pode ter até {MaxQuestions} perguntas." });
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var text = q.Text?.Trim() ?? string.Empty;
            if (text.Length is 0 or > 500)
                return BadRequest(new { message = $"Pergunta {i + 1}: informe o texto (até 500 caracteres)." });
            if (string.IsNullOrWhiteSpace(q.ScaleName) || q.ScaleName.Trim().Length > 100)
                return BadRequest(new { message = $"Pergunta {i + 1}: escolha a escala de respostas." });
            var optionError = ValidateOptions(q.Options, q.IsNps);
            if (optionError != null)
                return BadRequest(new { message = $"Pergunta {i + 1}: {optionError}" });
        }
        return null;
    }

    private (string Name, List<AnswerScaleOption> Options, ActionResult? Error) ValidateScale(SaveScaleRequest request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 100)
            return (name, [], BadRequest(new { message = "Informe o nome da escala (até 100 caracteres)." }));
        var error = ValidateOptions(request.Options, request.IsNps);
        if (error != null)
            return (name, [], BadRequest(new { message = error }));
        return (name, request.Options!.Select((o, i) => new AnswerScaleOption { Order = i, Label = o.Label.Trim(), Value = o.Value }).ToList(), null);
    }

    private static string? ValidateOptions(IReadOnlyList<ScaleOptionDto>? options, bool isNps)
    {
        if (options == null || options.Count is < 2 or > 11)
            return "a escala precisa ter de 2 a 11 opções.";
        if (options.Any(o => string.IsNullOrWhiteSpace(o.Label) || o.Label.Trim().Length > 100))
            return "toda opção precisa de um rótulo (até 100 caracteres).";
        if (options.Select(o => o.Label.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            return "as opções precisam ter rótulos diferentes.";
        if (options.Any(o => o.Value is < -1000 or > 1000))
            return "os valores precisam ficar entre -1000 e 1000.";
        if (isNps && options.Any(o => o.Value is null or < 0 or > 10))
            return "no eNPS toda opção precisa de valor de 0 a 10.";
        return null;
    }

    private static string DescribeOptions(IEnumerable<AnswerScaleOption> options) =>
        string.Join(", ", options.OrderBy(o => o.Order).Select(o => o.Value is { } v ? $"{o.Label}={v:0.##}" : o.Label));

    private static decimal Percent(int part, int total) => total == 0 ? 0 : Math.Round(part * 100m / total, 1);

    // O front manda ISO com fuso; sem fuso, assume UTC.
    private static DateTime ToUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
}
