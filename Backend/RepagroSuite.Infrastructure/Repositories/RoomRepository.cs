using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class RoomRepository : GenericRepository<Room>, IRoomRepository
{
    public RoomRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Room?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<Room?> GetWithDetailsAsync(Guid roomId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(r => r.RoomFeatures).ThenInclude(rf => rf.Feature)
            .Include(r => r.Availabilities)
            .Include(r => r.Blocks.Where(b => b.IsActive && !b.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeRoomId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(r => r.Code == code && (excludeRoomId == null || r.Id != excludeRoomId), cancellationToken);

    public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime start, DateTime end, int? minCapacity = null, CancellationToken cancellationToken = default)
    {
        var conflictingRoomIds = await _context.Reservations
            .Where(r => !r.IsDeleted &&
                        (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
                        r.StartDateTime < end && r.EndDateTime > start)
            .Select(r => r.RoomId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = _dbSet
            .Include(r => r.RoomFeatures).ThenInclude(rf => rf.Feature)
            .Where(r => r.Status == RoomStatus.Available && !conflictingRoomIds.Contains(r.Id));

        if (minCapacity.HasValue)
            query = query.Where(r => r.Capacity >= minCapacity.Value);

        return await query.OrderBy(r => r.Name).ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Room> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search = null, RoomStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(r => r.RoomFeatures).ThenInclude(rf => rf.Feature).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToUpper();
            query = query.Where(r => r.Name.ToUpper().Contains(s) || r.Code.ToUpper().Contains(s) || (r.Location != null && r.Location.ToUpper().Contains(s)));
        }

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IEnumerable<RoomAvailability>> GetAvailabilitiesAsync(Guid roomId, CancellationToken cancellationToken = default)
        => await _context.Set<RoomAvailability>()
            .Where(a => a.RoomId == roomId && !a.IsDeleted)
            .OrderBy(a => a.DayOfWeek)
            .ToListAsync(cancellationToken);

    public async Task ReplaceAvailabilitiesAsync(Guid roomId, IEnumerable<RoomAvailability> newAvailabilities, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<RoomAvailability>()
            .Where(a => a.RoomId == roomId && !a.IsDeleted)
            .ToListAsync(cancellationToken);

        _context.Set<RoomAvailability>().RemoveRange(existing);
        await _context.Set<RoomAvailability>().AddRangeAsync(newAvailabilities, cancellationToken);
    }
}
