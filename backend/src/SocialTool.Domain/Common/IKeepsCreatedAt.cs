namespace SocialTool.Domain.Common;

// Marca entidades cujo CreatedAt é definido por quem cria e não pode ser regravado ao salvar
// (ex.: resposta de enquete anônima, que guarda só a data para não revelar quem respondeu).
public interface IKeepsCreatedAt
{
}
