using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Rastreo.Api.Services;

public class EmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _log;

    public EmailService(IConfiguration cfg, ILogger<EmailService> log)
    {
        _cfg = cfg;
        _log = log;
    }

    public async Task EnviarAsync(
        IEnumerable<string> destinatarios,
        string asunto,
        string cuerpoHtml,
        IList<(string nombre, byte[] contenido, string mime)>? adjuntos = null,
        CancellationToken ct = default)
    {
        var host = _cfg["Rastreo:Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.Parse(_cfg["Rastreo:Email:SmtpPort"] ?? "587");
        var user = _cfg["Rastreo:Email:Username"] ?? throw new InvalidOperationException("Rastreo:Email:Username");
        var pass = _cfg["Rastreo:Email:Password"] ?? throw new InvalidOperationException("Rastreo:Email:Password");
        var fromName = _cfg["Rastreo:Email:FromName"] ?? "RASTREO";
        var fromAddr = _cfg["Rastreo:Email:FromAddress"] ?? user;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, fromAddr));
        foreach (var d in destinatarios)
        {
            if (!string.IsNullOrWhiteSpace(d))
                msg.To.Add(MailboxAddress.Parse(d.Trim()));
        }
        msg.Subject = asunto;

        var builder = new BodyBuilder { HtmlBody = cuerpoHtml };
        if (adjuntos != null)
        {
            foreach (var a in adjuntos)
            {
                builder.Attachments.Add(a.nombre, a.contenido, MimeKit.ContentType.Parse(a.mime));
            }
        }
        msg.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(user, pass, ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);

        _log.LogInformation("Correo enviado a {Recipients} asunto={Subject}",
            string.Join(",", msg.To.Mailboxes.Select(m => m.Address)), asunto);
    }
}
