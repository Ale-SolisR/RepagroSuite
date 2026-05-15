using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class RoomBlock : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public BlockType BlockType { get; set; }
    public string? Reason { get; set; }
    public bool IsRecurring { get; set; } = false;
    public DayOfWeek? RecurringDayOfWeek { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public DateTime? SpecificDate { get; set; }
    public DateTime? SpecificStartDateTime { get; set; }
    public DateTime? SpecificEndDateTime { get; set; }
    public bool IsActive { get; set; } = true;
}
