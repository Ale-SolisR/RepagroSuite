using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Reservations.DTOs;

public class ReservationDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string? RoomColor { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PeopleCount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ReservationStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? AdminComment { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsDirectAdminReservation { get; set; }
    public Guid? RecurrenceGroupId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Entrada de auditoría: una reserva individual O el resumen de una serie periódica completa.
public class ReservationGroupDto
{
    public string GroupKey { get; set; } = string.Empty;   // RecurrenceGroupId o el Id de la reserva individual
    public bool IsRecurring { get; set; }
    public Guid? RecurrenceGroupId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime FirstStart { get; set; }
    public DateTime LastStart { get; set; }
    public int OccurrenceCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int CancelledCount { get; set; }
    // Solo para entradas individuales (no recurrentes): la reserva completa, para acciones en línea.
    public ReservationDto? Single { get; set; }
}

public class BulkActionResultDto
{
    public int Affected { get; set; }
    public int Skipped { get; set; }
}

public class CreateReservationDto
{
    public Guid RoomId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PeopleCount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AdminDirectReservationDto
{
    public Guid RoomId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PeopleCount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// Reserva fija/recurrente semanal: misma franja horaria, mismo día de la semana
// (derivado de StartDate), repetida cada semana hasta EndDate inclusive.
public class CreateRecurringReservationDto
{
    public Guid RoomId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string StartTime { get; set; } = string.Empty; // "HH:mm"
    public string EndTime { get; set; } = string.Empty;   // "HH:mm"
    public int PeopleCount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class RecurringReservationResultDto
{
    public int CreatedCount { get; set; }
    public int TotalOccurrences { get; set; }
    public List<SkippedOccurrenceDto> Skipped { get; set; } = [];
}

public class SkippedOccurrenceDto
{
    public DateTime Date { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ApproveReservationDto
{
    public string? AdminComment { get; set; }
}

public class RejectReservationDto
{
    public string AdminComment { get; set; } = string.Empty;
}

public class CancelReservationDto
{
    public string Reason { get; set; } = string.Empty;
}

public class CalendarEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Color { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
