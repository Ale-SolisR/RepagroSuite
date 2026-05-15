using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class ReservationRepository : GenericRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<bool> HasConflictAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeReservationId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(r =>
            !r.IsDeleted &&
            r.RoomId == roomId &&
            (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
            (excludeReservationId == null || r.Id != excludeReservationId) &&
            r.StartDateTime < end && r.EndDateTime > start,
            cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.User)
            .Where(r => r.RoomId == roomId);

        if (from.HasValue) query = query.Where(r => r.StartDateTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.EndDateTime <= to.Value);

        return await query.OrderBy(r => r.StartDateTime).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.Room)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartDateTime)
            .ToListAsync(cancellationToken);

    public async Task<Reservation?> GetWithDetailsAsync(Guid reservationId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.Room).ThenInclude(room => room.RoomFeatures).ThenInclude(rf => rf.Feature)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

    public async Task<(IEnumerable<Reservation> Items, int Total)> GetPagedAsync(
        int page, int pageSize, Guid? userId = null, Guid? roomId = null,
        ReservationStatus? status = null, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Room)
            .Include(r => r.User)
            .AsQueryable();

        if (userId.HasValue) query = query.Where(r => r.UserId == userId.Value);
        if (roomId.HasValue) query = query.Where(r => r.RoomId == roomId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (from.HasValue) query = query.Where(r => r.StartDateTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.EndDateTime <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IEnumerable<Reservation>> GetForCalendarAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(r => r.Room)
            .Include(r => r.User)
            .Where(r =>
                (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
                r.StartDateTime < to && r.EndDateTime > from);

        if (roomId.HasValue) query = query.Where(r => r.RoomId == roomId.Value);

        return await query.OrderBy(r => r.StartDateTime).ToListAsync(cancellationToken);
    }
}
