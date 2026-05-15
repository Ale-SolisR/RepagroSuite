using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.InApp;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Module { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
}
