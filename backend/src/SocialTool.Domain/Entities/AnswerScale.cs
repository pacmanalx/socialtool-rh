using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Modelo de respostas reutilizável ("dimensão"): opções com rótulo e valor numérico, para o resultado
// sair em números (média, distribuição, eNPS) e não em palavras. As do sistema vêm prontas e não se apagam.
public class AnswerScale : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsNps { get; set; }
    public bool IsSystem { get; set; }
    public Guid? CreatedById { get; set; }

    public ICollection<AnswerScaleOption> Options { get; set; } = new List<AnswerScaleOption>();
}

public class AnswerScaleOption : BaseEntity
{
    public Guid ScaleId { get; set; }
    public int Order { get; set; }
    public string Label { get; set; } = string.Empty;
    // Nulo = opção só categórica (ex.: "Terça", "Quarta"): conta na distribuição, fica fora da média.
    public decimal? Value { get; set; }

    public AnswerScale Scale { get; set; } = null!;
}
