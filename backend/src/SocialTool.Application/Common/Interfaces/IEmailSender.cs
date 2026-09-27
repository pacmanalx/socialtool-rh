namespace SocialTool.Application.Common.Interfaces;

public record EmailMessage(string ToAddress, string ToName, string Subject, string HtmlBody, string TextBody);

// Único caminho de saída de e-mail da aplicação (implementado pelo GuardedEmailSender).
public interface IEmailSender
{
    // Se algum e-mail sai desta instalação (trava geral Email:Enabled e, com desvio ligado, destino configurado).
    bool IsEnabled { get; }

    // Destino para onde TODO e-mail é desviado; null quando o desvio está desligado (envio real aos usuários).
    string? RedirectTo { get; }

    bool CanSendTo(string address);

    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailDeliveryBlockedException : InvalidOperationException
{
    public EmailDeliveryBlockedException(string address)
        : base($"O envio de e-mail para {address} está bloqueado nesta instalação (Email:Enabled / Email:Redirect).")
    {
    }
}
