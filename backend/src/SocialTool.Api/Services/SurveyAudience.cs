using Microsoft.EntityFrameworkCore;
using SocialTool.Domain.Entities;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Services;

// Quem faz parte do público de uma enquete: empresa inteira, ou quem está nas áreas escolhidas e em
// qualquer área abaixo delas. Quem não tem área só entra em enquetes para a empresa inteira.
public static class SurveyAudience
{
    public static HashSet<Guid>? AreasOf(Survey survey, AreaTree tree) =>
        survey.AudienceAll ? null : tree.WithDescendants(survey.Audience.Select(a => a.DepartmentId));

    public static bool Includes(Survey survey, Guid? userAreaId, AreaTree tree)
    {
        var areas = AreasOf(survey, tree);
        return areas == null || (userAreaId is { } area && areas.Contains(area));
    }

    public static Task<int> CountAsync(ApplicationDbContext db, Survey survey, AreaTree tree)
    {
        var areas = AreasOf(survey, tree);
        var users = db.Users.Where(u => u.IsActive);
        return areas == null
            ? users.CountAsync()
            : users.CountAsync(u => u.DepartmentId != null && areas.Contains(u.DepartmentId.Value));
    }

    public static IReadOnlyList<string> Labels(Survey survey, AreaTree tree) =>
        survey.AudienceAll
            ? ["Empresa inteira"]
            : survey.Audience.Select(a => tree.PathOf(a.DepartmentId)).OrderBy(p => p).ToList();
}
