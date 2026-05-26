using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

// Proyección plana para calendario — evita materializar Reservation+Room+User completos.
public record CalendarReservationProjection(
    Guid Id,
    DateTime StartDateTime,
    DateTime EndDateTime,
    ReservationStatus Status,
    Guid RoomId,
    string RoomName,
    string? RoomColor,
    Guid UserId,
    string UserFullName);

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<bool> HasConflictAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeReservationId = null, CancellationToken cancellationToken = default);
    // Conflicto solo contra reservas YA aprobadas (no pendientes). Lo usa la auto-aprobación:
    // dos pendientes que se solapan no deben rechazarse entre sí, solo contra una aprobada.
    Task<bool> HasApprovedConflictAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeReservationId = null, CancellationToken cancellationToken = default);
    // Reservas pendientes cuya hora de inicio ya está dentro de la ventana de auto-aprobación.
    Task<IEnumerable<Reservation>> GetPendingDueAsync(DateTime startThreshold, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Reservation?> GetWithDetailsAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Reservation> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, bool sortDescending = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<CalendarReservationProjection>> GetForCalendarAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default);

    // ─── Auditoría agrupada por serie ───
    // Claves de grupo paginadas: cada clave es RecurrenceGroupId (serie) o el Id (reserva individual).
    Task<(IReadOnlyList<Guid> Keys, int Total)> GetAuditGroupKeysAsync(Guid? userId, Guid? roomId, ReservationStatus? status, bool sortDescending, int page, int pageSize, CancellationToken cancellationToken = default);
    // Reservas (con Sala y Usuario) cuya clave de grupo esté en la lista dada.
    Task<IEnumerable<Reservation>> GetByGroupKeysAsync(IReadOnlyList<Guid> keys, CancellationToken cancellationToken = default);
    // Todas las ocurrencias de una serie periódica, ordenadas por fecha.
    Task<IEnumerable<Reservation>> GetByRecurrenceGroupAsync(Guid recurrenceGroupId, CancellationToken cancellationToken = default);
}
