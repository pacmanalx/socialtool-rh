using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SocialTool.Api.Authorization;
using SocialTool.Api.DTOs;
using SocialTool.Api.Services;
using SocialTool.Domain.Entities;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Controllers;

// Configurações da organização desta instalação: quem instala ajusta aqui, pela interface.
[ApiController]
[Route("api/admin/organization")]
[RequireAdmin]
public partial class AdminOrganizationController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AdminOrganizationController(ApplicationDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationSettingsDto>> Get() =>
        Ok(ToDto(await _dbContext.GetOrganizationAsync()));

    [HttpPut]
    public async Task<ActionResult<OrganizationSettingsDto>> Update([FromBody] UpdateOrganizationSettingsRequest request, [FromServices] AuditService audit)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var currency = request.CurrencyName?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 150)
            return BadRequest(new { message = "Informe o nome da organização (até 150 caracteres)." });
        if (currency.Length is 0 or > 50)
            return BadRequest(new { message = "Informe o nome da moeda (até 50 caracteres)." });
        if (request.MonthlyCoinsQuota is < 0 or > 100_000)
            return BadRequest(new { message = "A cota mensal precisa estar entre 0 e 100000." });

        var domains = (request.GoogleWorkspaceDomains ?? [])
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => d.Length > 0)
            .Distinct()
            .ToList();
        var invalid = domains.FirstOrDefault(d => !DomainPattern().IsMatch(d));
        if (invalid != null)
            return BadRequest(new { message = $"\"{invalid}\" não é um domínio válido. Informe só o domínio, por exemplo: empresa.com.br" });
        if (domains.Count > 20 || string.Join(',', domains).Length > 1000)
            return BadRequest(new { message = "Domínios demais: informe no máximo 20." });

        var organization = await _dbContext.GetOrganizationAsync();
        organization.Name = name;
        organization.CurrencyName = currency;
        organization.MonthlyCoinsQuota = request.MonthlyCoinsQuota;
        organization.SetGoogleWorkspaceDomains(domains);
        await _dbContext.SaveChangesAsync();
        await audit.LogAsync(AuditService.Actions.OrganizationUpdated,
            $"Atualizou a organização: nome \"{name}\", moeda \"{currency}\", cota {request.MonthlyCoinsQuota}, domínios Google: {(domains.Count == 0 ? "nenhum" : string.Join(", ", domains))}.");
        return Ok(ToDto(organization));
    }

    private OrganizationSettingsDto ToDto(Organization organization) => new(
        organization.Name,
        organization.CurrencyName,
        organization.MonthlyCoinsQuota,
        organization.GetGoogleWorkspaceDomains(),
        !string.IsNullOrWhiteSpace(_configuration["Auth:Google:ClientId"]));

    [GeneratedRegex(@"^(?=.{1,200}$)([a-z0-9]([a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$")]
    private static partial Regex DomainPattern();
}
