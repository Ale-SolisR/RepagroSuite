using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class RoomAvailability : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsAvailable { get; set; } = true;
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public int MinReservationMinutes { get; set; } = 30;
    public int MaxReservationMinutes { get; set; } = 480;
    public int SlotIntervalMinutes { get; set; } = 30;
}
