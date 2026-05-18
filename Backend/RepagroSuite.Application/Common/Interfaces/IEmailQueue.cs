namespace RepagroSuite.Application.Common.Interfaces;

// Cola de envío de correos. El productor (servicios HTTP) encola y vuelve al instante.
// Un BackgroundService consume y envía vía SMTP en segundo plano.
public interface IEmailQueue
{
    void Enqueue(EmailMessage message);
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken);
}

public record EmailMessage(
    string To,
    string? Subject,
    string? HtmlBody,
    string? TemplateName,
    Dictionary<string, string>? TemplateVars
);
