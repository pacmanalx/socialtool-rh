using System.Net;
using Microsoft.Extensions.Configuration;
using SocialTool.Application.Common.Interfaces;

namespace SocialTool.Infrastructure.Services;

// Wrapper de TODO e-mail de saída. Duas travas, ambas lidas da configuração:
//   Email:Enabled          — trava geral; false = nada sai (padrão).
//   Email:Redirect:Enabled — desvio; true = todo e-mail vai para Email:Redirect:To, nunca ao destinatário real.
//                            É true também quando a chave não existe; com o destino vazio, nada sai (falha fechada).
// Só com Email:Redirect:Enabled=false explícito um e-mail chega a um usuário real.
public class GuardedEmailSender : IEmailSender
{
    private readonly SmtpTransport _transport;
    private readonly IConfiguration _configuration;

    public GuardedEmailSender(SmtpTransport transport, IConfiguration configuration)
    {
        _transport = transport;
        _configuration = configuration;
    }

    private bool DeliveryEnabled => bool.TryParse(_configuration["Email:Enabled"], out var enabled) && enabled;

    private bool RedirectEnabled => !bool.TryParse(_configuration["Email:Redirect:Enabled"], out var redirect) || redirect;

    private string? RedirectTarget =>
        _configuration["Email:Redirect:To"]?.Trim() is { Length: > 0 } target ? target : null;

    public string? RedirectTo => RedirectEnabled ? RedirectTarget : null;

    public bool IsEnabled => DeliveryEnabled && (!RedirectEnabled || RedirectTarget != null);

    public bool CanSendTo(string address) => IsEnabled;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            throw new EmailDeliveryBlockedException(message.ToAddress);

        if (!RedirectEnabled)
            return _transport.SendAsync(message, cancellationToken);

        var original = message.ToAddress;
        var encoded = WebUtility.HtmlEncode(original);
        var redirected = message with
        {
            ToAddress = RedirectTarget!,
            ToName = $"Desviado de {original}",
            Subject = $"[Desviado de {original}] {message.Subject}",
            HtmlBody =
                $"<p style=\"padding:8px;border:1px solid #f59e0b;background:#fffbeb;font-family:sans-serif;font-size:13px\">" +
                $"<strong>E-mail desviado (Email:Redirect).</strong> O destinatário real seria <strong>{encoded}</strong>. " +
                $"Nenhum usuário recebeu esta mensagem.</p>" + message.HtmlBody,
            TextBody =
                $"[E-mail desviado (Email:Redirect). O destinatário real seria {original}. Nenhum usuário recebeu esta mensagem.]\n\n" +
                message.TextBody
        };
        return _transport.SendAsync(redirected, cancellationToken);
    }
}
