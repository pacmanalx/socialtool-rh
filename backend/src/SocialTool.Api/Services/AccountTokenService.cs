using System.Net;
using Microsoft.EntityFrameworkCore;
using SocialTool.Application.Common.Interfaces;
using SocialTool.Domain.Entities;
using SocialTool.Domain.Enums;
using SocialTool.Infrastructure.Persistence;
using SocialTool.Infrastructure.Services;

namespace SocialTool.Api.Services;

// Tokens de uso único enviados por e-mail: convite e redefinição de senha.
public class AccountTokenService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AccountTokenService(ApplicationDbContext db, IEmailSender emailSender, IConfiguration configuration)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task SendInvitationAsync(User user, string organizationName, CancellationToken ct = default)
    {
        var hours = int.TryParse(_configuration["Auth:InvitationHours"], out var h) ? h : 72;
        var raw = await IssueAsync(user.Id, UserTokenPurpose.Invitation, TimeSpan.FromHours(hours), ct);
        var link = $"{PublicUrl()}/convite?token={Uri.EscapeDataString(raw)}";
        var name = WebUtility.HtmlEncode(user.Name);
        var organization = WebUtility.HtmlEncode(organizationName);

        await _emailSender.SendAsync(new EmailMessage(
            user.Email,
            user.Name,
            $"Você foi convidado para o SocialTool RH — {organizationName}",
            $"""
            <p>Olá, {name}!</p>
            <p>Você foi convidado para participar do <strong>SocialTool RH</strong> da <strong>{organization}</strong>.</p>
            <p><a href="{link}">Aceitar o convite e criar minha senha</a></p>
            <p>O link vale por {hours} horas. Se você não esperava este convite, ignore este e-mail.</p>
            """,
            $"Olá, {user.Name}!\n\nVocê foi convidado para o SocialTool RH da {organizationName}.\n" +
            $"Aceite o convite e crie sua senha em: {link}\n\nO link vale por {hours} horas."), ct);
    }

    public async Task SendPasswordResetAsync(User user, CancellationToken ct = default)
    {
        var minutes = int.TryParse(_configuration["Auth:PasswordResetMinutes"], out var m) ? m : 60;
        var raw = await IssueAsync(user.Id, UserTokenPurpose.PasswordReset, TimeSpan.FromMinutes(minutes), ct);
        var link = $"{PublicUrl()}/redefinir-senha?token={Uri.EscapeDataString(raw)}";
        var name = WebUtility.HtmlEncode(user.Name);

        await _emailSender.SendAsync(new EmailMessage(
            user.Email,
            user.Name,
            "Redefinição de senha — SocialTool RH",
            $"""
            <p>Olá, {name}!</p>
            <p>Recebemos um pedido para redefinir a sua senha no SocialTool RH.</p>
            <p><a href="{link}">Definir uma nova senha</a></p>
            <p>O link vale por {minutes} minutos. Se não foi você, ignore este e-mail — sua senha continua a mesma.</p>
            """,
            $"Olá, {user.Name}!\n\nPara definir uma nova senha no SocialTool RH, acesse: {link}\n\n" +
            $"O link vale por {minutes} minutos. Se não foi você, ignore este e-mail."), ct);
    }

    // Devolve o token ainda válido, com o usuário carregado, ou null.
    public Task<UserToken?> FindUsableAsync(string rawToken, UserTokenPurpose purpose, CancellationToken ct = default)
    {
        var hash = SecureToken.Hash(rawToken);
        var now = DateTime.UtcNow;
        return _db.UserTokens
            .Include(t => t.User).ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == hash &&
                t.Purpose == purpose &&
                t.UsedAt == null &&
                t.ExpiresAt > now &&
                t.User.IsActive, ct);
    }

    // Marca o token como usado e invalida os demais tokens pendentes do usuário (convites e redefinições).
    public async Task ConsumeAsync(UserToken token, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var pending = await _db.UserTokens
            .Where(t => t.UserId == token.UserId && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in pending)
            t.UsedAt = now;
        token.UsedAt = now;
    }

    private async Task<string> IssueAsync(Guid userId, UserTokenPurpose purpose, TimeSpan lifetime, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var previous = await _db.UserTokens
            .Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in previous)
            t.UsedAt = now;

        var raw = SecureToken.Generate();
        _db.UserTokens.Add(new UserToken
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = SecureToken.Hash(raw),
            ExpiresAt = now.Add(lifetime)
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    private string PublicUrl() =>
        (_configuration["App:PublicUrl"] ?? "http://localhost:3000").TrimEnd('/');
}
