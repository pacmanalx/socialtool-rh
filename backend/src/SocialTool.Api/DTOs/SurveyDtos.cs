namespace SocialTool.Api.DTOs;

// ---- Escalas de resposta ----
public record ScaleOptionDto(string Label, decimal? Value);

public record ScaleDto(Guid Id, string Name, bool IsNps, bool IsSystem, IReadOnlyList<ScaleOptionDto> Options);

public record SaveScaleRequest(string? Name, bool IsNps, IReadOnlyList<ScaleOptionDto>? Options);

// ---- Administração de enquetes ----
public record SurveyOptionDto(Guid Id, string Label, decimal? Value);

public record SurveyQuestionDto(Guid Id, string Text, string ScaleName, bool IsNps, IReadOnlyList<SurveyOptionDto> Options);

public record SurveySummaryDto(
    Guid Id,
    string Title,
    string Status,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAnonymous,
    bool AudienceAll,
    IReadOnlyList<string> AudienceLabels,
    int QuestionCount,
    int Responses,
    int AudienceSize,
    string CreatedByName);

public record SurveyDetailDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAnonymous,
    bool AudienceAll,
    IReadOnlyList<Guid> AudienceAreaIds,
    string Status,
    int Responses,
    bool Locked,
    IReadOnlyList<SurveyQuestionDto> Questions);

// A pergunta chega com a cópia das opções (o front preenche a partir da escala escolhida).
public record SaveQuestionRequest(string? Text, string? ScaleName, bool IsNps, IReadOnlyList<ScaleOptionDto>? Options);

public record SaveSurveyRequest(
    string? Title,
    string? Description,
    DateTime StartsAt,
    DateTime EndsAt,
    bool IsAnonymous,
    bool AudienceAll,
    IReadOnlyList<Guid>? AudienceAreaIds,
    IReadOnlyList<SaveQuestionRequest>? Questions);

// ---- Resultado ----
public record OptionResultDto(string Label, decimal? Value, int Count, decimal Percent);

public record NpsResultDto(decimal Score, int Promoters, int Passives, int Detractors);

public record QuestionResultDto(
    Guid Id,
    string Text,
    string ScaleName,
    bool IsNps,
    int Answered,
    decimal? Average,
    NpsResultDto? Nps,
    IReadOnlyList<OptionResultDto> Options);

public record AreaResponsesDto(Guid? AreaId, string Label, int Responses);

public record SurveyResultsDto(
    Guid SurveyId,
    string Title,
    string Status,
    bool IsAnonymous,
    int AudienceSize,
    int Participants,
    decimal ParticipationPercent,
    Guid? AreaId,
    string? AreaLabel,
    int MinimumGroup,
    bool Suppressed,
    int Responses,
    IReadOnlyList<QuestionResultDto> Questions,
    IReadOnlyList<AreaResponsesDto> Areas);

public record IndividualAccessRequest(string? Reason);

public record IndividualAnswerDto(string Question, string Answer, decimal? Value);

public record IndividualResponseDto(string Name, string Email, string? Area, DateTime SubmittedAt, IReadOnlyList<IndividualAnswerDto> Answers);

// ---- Quem responde ----
public record MySurveyDto(Guid Id, string Title, string? Description, DateTime EndsAt, bool IsAnonymous, int QuestionCount, bool Answered);

public record SurveyToAnswerDto(
    Guid Id,
    string Title,
    string? Description,
    DateTime EndsAt,
    bool IsAnonymous,
    bool Answered,
    IReadOnlyList<SurveyQuestionDto> Questions);

public record SubmitAnswerRequest(Guid QuestionId, Guid OptionId);

public record SubmitSurveyRequest(IReadOnlyList<SubmitAnswerRequest>? Answers);
