using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Reservations.Services;

public interface IReservationService
{
    // callerCanApprove = true cuando quien crea es admin/master (su reserva queda aprobada directa).
    Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto, bool callerCanApprove = false, CancellationToken cancellationToken = default);
    Task<RecurringReservationResultDto> CreateRecurringAsync(Guid userId, CreateRecurringReservationDto dto, bool callerCanApprove = false, CancellationToken cancellationToken = default);
    Task<ReservationDto> AdminDirectCreateAsync(Guid adminId, AdminDirectReservationDto dto, CancellationToken cancellationToken = default);
    Task<ReservationDto> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<PagedResult<ReservationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, bool sortDescending = true, CancellationToken cancellationToken = default);
    Task<ReservationDto> ApproveAsync(Guid reservationId, ApproveReservationDto dto, Guid approvedBy, CancellationToken cancellationToken = default);
    Task<ReservationDto> RejectAsync(Guid reservationId, RejectReservationDto dto, Guid rejectedBy, CancellationToken cancellationToken = default);
    // canManageAny = true para admin/master: puede cancelar reservas de cualquier usuario.
    Task<ReservationDto> CancelAsync(Guid reservationId, CancelReservationDto dto, Guid cancelledBy, bool canManageAny = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default);
    // Proceso en segundo plano: aprueba (o rechaza por conflicto) las pendientes dentro de la ventana de 30 min.
    Task<int> AutoApproveDueAsync(CancellationToken cancellationToken = default);

    // ─── Auditoría agrupada por serie periódica ───
    Task<PagedResult<ReservationGroupDto>> GetAuditGroupsAsync(int page, int pageSize, Guid? userId, Guid? roomId, ReservationStatus? status, bool sortDescending, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReservationDto>> GetGroupOccurrencesAsync(Guid recurrenceGroupId, CancellationToken cancellationToken = default);
    Task<BulkActionResultDto> ApproveGroupAsync(Guid recurrenceGroupId, Guid approvedBy, CancellationToken cancellationToken = default);
    Task<BulkActionResultDto> RejectGroupAsync(Guid recurrenceGroupId, RejectReservationDto dto, Guid rejectedBy, CancellationToken cancellationToken = default);
}
