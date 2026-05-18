using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

// Fachada pública. Los servicios HTTP llaman aquí; los envíos no SMTP-test van por cola.
// TestConnectionAsync y TestAndSendAsync sí ejecutan SMTP sincrónico (uso desde panel Settings).
public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;
    private readonly ISecretProtector _protector;
    private readonly IEmailQueue _queue;

    public EmailService(
        ApplicationDbContext context,
        ILogger<EmailService> logger,
        ISecretProtector protector,
        IEmailQueue queue)
    {
        _context = context;
        _logger = logger;
        _protector = protector;
        _queue = queue;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(new EmailMessage(to, subject, htmlBody, null, null));
        return Task.CompletedTask;
    }

    public Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(new EmailMessage(to, null, null, templateName, variables));
        return Task.CompletedTask;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = await EmailTemplates.GetSmtpConfigAsync(_context, _protector, cancellationToken);
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            if (!string.IsNullOrEmpty(config.Username))
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    public async Task<bool> TestAndSendAsync(string to, CancellationToken cancellationToken = default)
    {
        var config = await EmailTemplates.GetSmtpConfigAsync(_context, _protector, cancellationToken);
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = "Prueba de conexión SMTP — RepagroSuite";
            msg.Body = new TextPart("html") { Text = "<h2>¡Conexión exitosa!</h2><p>El servidor SMTP de <strong>RepagroSuite</strong> está configurado correctamente.</p>" };

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            if (!string.IsNullOrEmpty(config.Username))
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            await client.SendAsync(msg, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP test send failed to {To}", to);
            return false;
        }
    }
}

// Helpers compartidos entre EmailService y EmailWorker.
internal static class EmailTemplates
{
    public class SmtpConfig
    {
        public bool Enabled { get; set; }
        public string FromName { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public static async Task<SmtpConfig> GetSmtpConfigAsync(ApplicationDbContext ctx, ISecretProtector protector, CancellationToken ct)
    {
        var settings = await ctx.SystemSettings
            .AsNoTracking()
            .Where(s => s.Module == "EMAIL" && !s.IsDeleted)
            .ToListAsync(ct);

        string Get(string key) => settings.FirstOrDefault(s => s.Key == key)?.Value ?? string.Empty;
        bool GetBool(string key) => Get(key).Equals("true", StringComparison.OrdinalIgnoreCase);
        int GetInt(string key, int def) => int.TryParse(Get(key), out var v) ? v : def;

        return new SmtpConfig
        {
            Enabled = GetBool("EMAIL.ENABLED"),
            FromName = Get("EMAIL.FROM_NAME"),
            FromAddress = Get("EMAIL.FROM_ADDRESS"),
            SmtpHost = Get("EMAIL.SMTP_HOST"),
            SmtpPort = GetInt("EMAIL.SMTP_PORT", 587),
            UseSsl = GetBool("EMAIL.SMTP_USE_SSL"),
            Username = Get("EMAIL.SMTP_USERNAME"),
            Password = protector.Unprotect(Get("EMAIL.SMTP_PASSWORD")) ?? string.Empty,
        };
    }

    public static string GetSubject(string templateName) => templateName switch
    {
        "user_approved" => "Su cuenta ha sido aprobada - RepagroSuite",
        "user_rejected" => "Su solicitud de registro fue rechazada - RepagroSuite",
        "password_reset" => "Recuperación de contraseña - RepagroSuite",
        "password_changed" => "Su contraseña fue actualizada - RepagroSuite",
        "reservation_approved" => "Reserva aprobada - RepagroSuite",
        "reservation_rejected" => "Reserva rechazada - RepagroSuite",
        _ => "Notificación de RepagroSuite"
    };

    public static string BuildBody(string templateName, Dictionary<string, string> vars)
    {
        string Get(string key) => vars.TryGetValue(key, out var v) ? v : string.Empty;
        return templateName switch
        {
            "user_approved" => $@"
                <h2>¡Bienvenido a RepagroSuite!</h2>
                <p>Estimado/a {Get("fullName")},</p>
                <p>Su cuenta ha sido aprobada. Sus credenciales de acceso son:</p>
                <ul><li><strong>Correo:</strong> {Get("email")}</li><li><strong>Contraseña temporal:</strong> {Get("tempPassword")}</li></ul>
                <p>Por seguridad, deberá cambiar su contraseña al iniciar sesión por primera vez.</p>
                <p>La contraseña temporal expira en {Get("expiryHours")} horas.</p>",

            "user_rejected" => $@"
                <h2>Solicitud de registro rechazada</h2>
                <p>Estimado/a {Get("fullName")},</p>
                <p>Su solicitud de registro no pudo ser aprobada.</p>
                <p><strong>Motivo:</strong> {Get("reason")}</p>
                <p>Si tiene dudas, contacte al administrador.</p>",

            "password_reset" => $@"
                <h2>Recuperación de contraseña</h2>
                <p>Hemos recibido una solicitud para restablecer la contraseña de su cuenta.</p>
                <p>Haga clic en el siguiente enlace (válido por {Get("expiryHours")} horas):</p>
                <p><a href='{Get("resetLink")}'>Restablecer contraseña</a></p>
                <p>Si no solicitó este cambio, ignore este correo.</p>",

            "password_changed" => $@"
                <h2>Contraseña actualizada</h2>
                <p>Estimado/a {Get("fullName")},</p>
                <p>Su contraseña fue actualizada exitosamente el {Get("date")}.</p>
                <p>Si no realizó este cambio, contacte al administrador inmediatamente.</p>",

            "reservation_approved" => $@"
                <h2>Reserva aprobada</h2>
                <p>Su reserva para <strong>{Get("roomName")}</strong> fue aprobada.</p>
                <ul>
                    <li><strong>Fecha:</strong> {Get("date")}</li>
                    <li><strong>Hora:</strong> {Get("startTime")} - {Get("endTime")}</li>
                    <li><strong>Motivo:</strong> {Get("purpose")}</li>
                </ul>",

            "reservation_rejected" => $@"
                <h2>Reserva rechazada</h2>
                <p>Su solicitud de reserva para <strong>{Get("roomName")}</strong> no pudo ser aprobada.</p>
                <p><strong>Motivo:</strong> {Get("adminComment")}</p>",

            _ => "<p>Notificación del sistema RepagroSuite.</p>"
        };
    }
}
