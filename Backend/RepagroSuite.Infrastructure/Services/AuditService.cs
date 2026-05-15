using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        Guid? userId, string action, string? entityName = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, string? module = null,
        bool success = true, string? errorMessage = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var audit = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            Module = module,
            Success = success,
            ErrorMessage = errorMessage,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(audit);

        try { await _context.SaveChangesAsync(cancellationToken); }
        catch { /* audit must never block the main flow */ }
    }
}
