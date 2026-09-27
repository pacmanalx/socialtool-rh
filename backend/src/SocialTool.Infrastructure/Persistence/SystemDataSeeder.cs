using Microsoft.EntityFrameworkCore;
using SocialTool.Domain.Entities;

namespace SocialTool.Infrastructure.Persistence;

// Dados de sistema que toda instalação precisa (não são dados de exemplo): roda em toda subida, idempotente.
public static class SystemDataSeeder
{
    private static readonly (string Name, bool IsNps, (string Label, decimal? Value)[] Options)[] Scales =
    [
        ("Conceitual de 5 pontos", false, [("Excelente", 5), ("Bom", 4), ("Neutro", 3), ("Ruim", 2), ("Péssimo", 1)]),
        ("Concordância", false, [("Concordo totalmente", 5), ("Concordo", 4), ("Neutro", 3), ("Discordo", 2), ("Discordo totalmente", 1)]),
        ("Nota de 1 a 5", false, Numeric(1, 5)),
        ("Nota de 1 a 10", false, Numeric(1, 10)),
        ("eNPS (0 a 10)", true, Numeric(0, 10)),
        ("Sim ou não", false, [("Sim", 1), ("Não", 0)]),
    ];

    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var existing = await db.AnswerScales.Where(s => s.IsSystem).Select(s => s.Name).ToListAsync();
        foreach (var (name, isNps, options) in Scales.Where(s => !existing.Contains(s.Name)))
        {
            db.AnswerScales.Add(new AnswerScale
            {
                Name = name,
                IsNps = isNps,
                IsSystem = true,
                Options = options.Select((o, i) => new AnswerScaleOption { Order = i, Label = o.Label, Value = o.Value }).ToList()
            });
        }
        await db.SaveChangesAsync();
    }

    // Do maior para o menor, como nas escalas conceituais.
    private static (string, decimal?)[] Numeric(int min, int max) =>
        Enumerable.Range(min, max - min + 1).Reverse().Select(n => (n.ToString(), (decimal?)n)).ToArray();
}
