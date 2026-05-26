using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class Reservation : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public int PeopleCount { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? AdminComment { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsDirectAdminReservation { get; set; } = false;

    // Enlaza todas las ocurrencias de una misma reserva periódica (mismo Guid para toda la serie).
    // null = reserva individual (no periódica).
    public Guid? RecurrenceGroupId { get; set; }
}
