using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Module { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
