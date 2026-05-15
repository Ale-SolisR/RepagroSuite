namespace RepagroSuite.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? entityName = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, string? module = null,
        bool success = true, string? errorMessage = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);
}
