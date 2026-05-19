using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

// BackgroundService que consume la cola de correos y envía SMTP en segundo plano.
// Crea su propio scope por mensaje para resolver DbContext (Scoped) y SecretProtector (Singleton).
// Si un envío falla, se loguea y continúa con el siguiente (no bloquea la cola).
public class EmailWorker : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailWorker> _logger;

    public EmailWorker(IEmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await SendOneAsync(msg, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailWorker: error enviando correo a {To}", msg.To);
            }
        }
    }

    private async Task SendOneAsync(EmailMessage msg, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var cache = scope.ServiceProvider.GetRequiredService<IAppCache>();

        var config = await EmailTemplates.GetSmtpConfigAsync(ctx, protector, cache, ct);
        if (!config.Enabled)
        {
            _logger.LogInformation("Email service disabled. Skipping send to {To}", msg.To);
            return;
        }

        string subject = msg.Subject ?? string.Empty;
        string body = msg.HtmlBody ?? string.Empty;
        if (!string.IsNullOrEmpty(msg.TemplateName))
        {
            subject = EmailTemplates.GetSubject(msg.TemplateName);
            body = EmailTemplates.BuildBody(msg.TemplateName, msg.TemplateVars ?? new());
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        mime.To.Add(MailboxAddress.Parse(msg.To));
        mime.Subject = subject;
        mime.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
            config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
        if (!string.IsNullOrEmpty(config.Username))
            await client.AuthenticateAsync(config.Username, config.Password, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }
}
