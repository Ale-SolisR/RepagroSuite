using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Rooms.DTOs;

public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public RoomStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public IEnumerable<FeatureDto> Features { get; set; } = [];
    public IEnumerable<RoomAvailabilityDto> Availabilities { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    // Token de concurrencia optimista — el cliente lo devuelve en UpdateRoomDto para que
    // SaveChanges detecte si otro admin modificó la sala entre el GET y el PUT.
    public string? RowVersion { get; set; }
}

public class RoomAvailabilityDto
{
    public Guid Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public string CloseTime { get; set; } = string.Empty;
    public int MinReservationMinutes { get; set; }
    public int MaxReservationMinutes { get; set; }
    public int SlotIntervalMinutes { get; set; }
}

public class RoomBlockDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public BlockType BlockType { get; set; }
    public string BlockTypeName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsRecurring { get; set; }
    public DayOfWeek? RecurringDayOfWeek { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? SpecificDate { get; set; }
    public DateTime? SpecificStartDateTime { get; set; }
    public DateTime? SpecificEndDateTime { get; set; }
}

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IconName { get; set; }
}

public class CreateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public List<Guid> FeatureIds { get; set; } = [];
    public List<UpsertRoomAvailabilityDto> Availabilities { get; set; } = [];
}

public class UpdateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }
    public List<Guid> FeatureIds { get; set; } = [];
    public List<UpsertRoomAvailabilityDto> Availabilities { get; set; } = [];
    // Si llega null, no se valida concurrencia (compatible con clientes que no lo envían aún).
    // Si llega valor, EF Core compara contra la versión en BD y lanza DbUpdateConcurrencyException si difiere.
    public string? RowVersion { get; set; }
}

public class UpsertRoomAvailabilityDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsAvailable { get; set; }
    public string OpenTime { get; set; } = "08:00";
    public string CloseTime { get; set; } = "17:00";
    public int MinReservationMinutes { get; set; } = 30;
    public int MaxReservationMinutes { get; set; } = 480;
    public int SlotIntervalMinutes { get; set; } = 30;
}

public class CreateRoomBlockDto
{
    public Guid RoomId { get; set; }
    public BlockType BlockType { get; set; }
    public string? Reason { get; set; }
    public bool IsRecurring { get; set; }
    public DayOfWeek? RecurringDayOfWeek { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? SpecificDate { get; set; }
    public DateTime? SpecificStartDateTime { get; set; }
    public DateTime? SpecificEndDateTime { get; set; }
}

public class RoomAvailabilityCheckDto
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid? ExcludeReservationId { get; set; }
}

public class AvailableSlotDto
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
}
