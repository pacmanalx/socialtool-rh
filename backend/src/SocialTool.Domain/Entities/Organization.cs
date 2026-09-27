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
    // Domínio Google Workspace aceito no login com Google (ex.: "acme.com"). Null desliga o login com Google.
    public string? GoogleWorkspaceDomain { get; set; }
}
