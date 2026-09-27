using SocialTool.Domain.Entities;

namespace SocialTool.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(User user);
}
