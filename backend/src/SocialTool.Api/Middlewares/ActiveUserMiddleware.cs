using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SocialTool.Infrastructure.Persistence;

namespace SocialTool.Api.Middlewares;

// Confere, a cada requisição autenticada, que o usuário do token continua ativo.
// É o que faz a desativação cortar o acesso na hora, sem esperar o access token vencer.
public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var active = Guid.TryParse(userClaim, out var userId)
                && await dbContext.Users.AnyAsync(u => u.Id == userId && u.IsActive);

            if (!active)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Sessão inválida. Entre novamente." });
                return;
            }
        }

        await _next(context);
    }
}
