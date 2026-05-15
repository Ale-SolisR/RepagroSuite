namespace RepagroSuite.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<bool> TestAndSendAsync(string to, CancellationToken cancellationToken = default);
}
