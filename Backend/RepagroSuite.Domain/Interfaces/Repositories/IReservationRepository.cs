using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<bool> HasConflictAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeReservationId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Reservation?> GetWithDetailsAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Reservation> Items, int Total)> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reservation>> GetForCalendarAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default);
}
