namespace RepagroSuite.Domain.Enums;

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Read = 4
}

public enum NotificationType
{
    Email = 1,
    InApp = 2,
    Both = 3
}
