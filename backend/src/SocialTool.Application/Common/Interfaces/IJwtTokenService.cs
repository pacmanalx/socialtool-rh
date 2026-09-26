using SocialTool.Domain.Entities;

namespace SocialTool.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user, string tenantSubdomain);
}
