using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;

namespace SocialTool.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user, string tenantSubdomain)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "CHANGE-ME-IN-PRODUCTION-use-a-32-byte-random-secret-not-this";
        var issuer = _configuration["Jwt:Issuer"] ?? "SocialToolRh";
        var audience = _configuration["Jwt:Audience"] ?? "SocialToolRhClients";
        var expirationHours = int.TryParse(_configuration["Jwt:ExpirationHours"], out var h) ? h : 24;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new("Subdomain", tenantSubdomain),
            new("JobTitle", user.JobTitle)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
