using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SocialTool.Application.Common.Interfaces;

namespace SocialTool.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Email:Smtp:Host não configurado.");
        var port = int.TryParse(_configuration["Email:Smtp:Port"], out var p) ? p : 587;
        var useSsl = bool.TryParse(_configuration["Email:Smtp:UseSsl"], out var ssl) && ssl;
        var user = _configuration["Email:Smtp:User"];
        var password = _configuration["Email:Smtp:Password"] ?? string.Empty;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(
            _configuration["Email:FromName"] ?? "SocialTool RH",
            _configuration["Email:FromAddress"] ?? "no-reply@socialtool.local"));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = useSsl
            ? SecureSocketOptions.SslOnConnect
            : port == 25 || port == 1025 ? SecureSocketOptions.None : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(host, port, socketOptions, cancellationToken);
        if (!string.IsNullOrEmpty(user))
            await client.AuthenticateAsync(user, password, cancellationToken);
        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
