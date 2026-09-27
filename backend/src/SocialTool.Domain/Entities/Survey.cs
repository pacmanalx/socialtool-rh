using SocialTool.Domain.Common;
using SocialTool.Domain.Enums;

namespace SocialTool.Domain.Entities;

public class Survey : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    // Anônima: a resposta não guarda quem respondeu nem o horário exato (ver SurveyResponse).
    public bool IsAnonymous { get; set; } = true;
    // true = empresa inteira; false = só quem está nas áreas de Audience (e em tudo abaixo delas).
    public bool AudienceAll { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;
    public ICollection<SurveyAudienceArea> Audience { get; set; } = new List<SurveyAudienceArea>();
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();

    public SurveyStatus StatusAt(DateTime now) =>
        PublishedAt == null ? SurveyStatus.Draft
        : ClosedAt != null || now >= EndsAt ? SurveyStatus.Closed
        : now < StartsAt ? SurveyStatus.Scheduled
        : SurveyStatus.Open;
}

public class SurveyAudienceArea : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid DepartmentId { get; set; }

    public Survey Survey { get; set; } = null!;
    public Department Department { get; set; } = null!;
}

// A pergunta guarda uma CÓPIA das opções da escala: mudar a escala depois não altera enquetes já feitas.
public class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public string ScaleName { get; set; } = string.Empty;
    public bool IsNps { get; set; }

    public Survey Survey { get; set; } = null!;
    public ICollection<SurveyQuestionOption> Options { get; set; } = new List<SurveyQuestionOption>();
}

public class SurveyQuestionOption : BaseEntity
{
    public Guid QuestionId { get; set; }
    public int Order { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal? Value { get; set; }

    public SurveyQuestion Question { get; set; } = null!;
}

// Quem já respondeu (para não responder duas vezes). Nunca aponta para a resposta.
public class SurveyParticipation : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }

    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Na enquete anônima: UserId nulo e CreatedAt truncado para a data, para não cruzar com o horário da participação.
// AreaId é a área da pessoa no momento da resposta, usada só no recorte por área (com quórum mínimo).
public class SurveyResponse : BaseEntity, IKeepsCreatedAt
{
    public Guid SurveyId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AreaId { get; set; }

    public Survey Survey { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}

public class SurveyAnswer : BaseEntity, IKeepsCreatedAt
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid OptionId { get; set; }
    public decimal? Value { get; set; }

    public SurveyResponse Response { get; set; } = null!;
}
