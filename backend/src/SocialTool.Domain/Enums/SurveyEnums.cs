namespace SocialTool.Domain.Enums;

// Tipo do nó na estrutura da empresa. Só rótulo: a árvore aceita qualquer combinação e profundidade.
public enum AreaKind
{
    Unit,
    Department,
    Sector
}

// Situação da enquete. Não é gravada: sai das datas (ver Survey.StatusAt).
public enum SurveyStatus
{
    Draft,
    Scheduled,
    Open,
    Closed
}
