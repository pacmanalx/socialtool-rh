using SocialTool.Domain.Common;

namespace SocialTool.Domain.Entities;

// Configuração da organização dona desta instalação. Cada instalação atende uma única organização,
// então esta tabela tem sempre exatamente uma linha (criada na primeira subida).
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string CurrencyName { get; set; } = "SocialCoins";
    public int MonthlyCoinsQuota { get; set; } = 100;
    // Domínios Google Workspace aceitos no login com Google, separados por vírgula
    // (um Workspace pode ter vários, ex.: "acme.com,acme.com.br"). Vazio desliga o login com Google.
    public string? GoogleWorkspaceDomains { get; set; }

    public IReadOnlyList<string> GetGoogleWorkspaceDomains() =>
        (GoogleWorkspaceDomains ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => d.ToLowerInvariant())
            .Distinct()
            .ToList();

    public void SetGoogleWorkspaceDomains(IEnumerable<string> domains)
    {
        var list = domains
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => d.Length > 0)
            .Distinct()
            .ToList();
        GoogleWorkspaceDomains = list.Count == 0 ? null : string.Join(',', list);
    }
}
