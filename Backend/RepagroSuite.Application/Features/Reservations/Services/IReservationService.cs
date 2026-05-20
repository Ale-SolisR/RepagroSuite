using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Reservations.Services;

public interface IReservationService
{
    Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto, CancellationToken cancellationToken = default);
    Task<RecurringReservationResultDto> CreateRecurringAsync(Guid userId, CreateRecurringReservationDto dto, CancellationToken cancellationToken = default);
    Task<ReservationDto> AdminDirectCreateAsync(Guid adminId, AdminDirectReservationDto dto, CancellationToken cancellationToken = default);
    Task<ReservationDto> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<PagedResult<ReservationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<ReservationDto> ApproveAsync(Guid reservationId, ApproveReservationDto dto, Guid approvedBy, CancellationToken cancellationToken = default);
    Task<ReservationDto> RejectAsync(Guid reservationId, RejectReservationDto dto, Guid rejectedBy, CancellationToken cancellationToken = default);
    Task<ReservationDto> CancelAsync(Guid reservationId, CancelReservationDto dto, Guid cancelledBy, CancellationToken cancellationToken = default);
    Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default);
}
