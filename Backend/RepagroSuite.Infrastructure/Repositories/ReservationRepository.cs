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
        => await _dbSet.AsNoTracking().AnyAsync(r =>
            !r.IsDeleted &&
            r.RoomId == roomId &&
            (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
            (excludeReservationId == null || r.Id != excludeReservationId) &&
            r.StartDateTime < end && r.EndDateTime > start,
            cancellationToken);

    public async Task<bool> HasApprovedConflictAsync(Guid roomId, DateTime start, DateTime end, Guid? excludeReservationId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().AnyAsync(r =>
            !r.IsDeleted &&
            r.RoomId == roomId &&
            r.Status == ReservationStatus.Approved &&
            (excludeReservationId == null || r.Id != excludeReservationId) &&
            r.StartDateTime < end && r.EndDateTime > start,
            cancellationToken);

    public async Task<IEnumerable<Reservation>> GetPendingDueAsync(DateTime startThreshold, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == ReservationStatus.Pending && r.StartDateTime <= startThreshold)
            .OrderBy(r => r.StartDateTime)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Guid> Keys, int Total)> GetAuditGroupKeysAsync(Guid? userId, Guid? roomId, ReservationStatus? status, bool sortDescending, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.AsNoTracking().AsQueryable();
        if (userId.HasValue) q = q.Where(r => r.UserId == userId.Value);
        if (roomId.HasValue) q = q.Where(r => r.RoomId == roomId.Value);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        // Clave de grupo = RecurrenceGroupId (serie) o el propio Id (reserva individual).
        var groups = q
            .GroupBy(r => r.RecurrenceGroupId ?? r.Id)
            .Select(g => new { Key = g.Key, MinStart = g.Min(x => x.StartDateTime), MaxStart = g.Max(x => x.StartDateTime) });

        var total = await groups.CountAsync(cancellationToken);

        var ordered = sortDescending
            ? groups.OrderByDescending(g => g.MaxStart)
            : groups.OrderBy(g => g.MinStart);

        var keys = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        return (keys, total);
    }

    public async Task<IEnumerable<Reservation>> GetByGroupKeysAsync(IReadOnlyList<Guid> keys, CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return new List<Reservation>();
        var keyList = keys.ToList();
        return await _dbSet.AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.User)
            .Where(r => (r.RecurrenceGroupId != null && keyList.Contains(r.RecurrenceGroupId.Value))
                     || (r.RecurrenceGroupId == null && keyList.Contains(r.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetByRecurrenceGroupAsync(Guid recurrenceGroupId, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.User)
            .Where(r => r.RecurrenceGroupId == recurrenceGroupId)
            .OrderBy(r => r.StartDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Reservation>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.RoomId == roomId);

        if (from.HasValue) query = query.Where(r => r.StartDateTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.EndDateTime <= to.Value);

        return await query.OrderBy(r => r.StartDateTime).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reservation>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .AsNoTracking()
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
        bool sortDescending = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.User)
            .AsQueryable();

        if (userId.HasValue) query = query.Where(r => r.UserId == userId.Value);
        if (roomId.HasValue) query = query.Where(r => r.RoomId == roomId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (from.HasValue) query = query.Where(r => r.StartDateTime >= from.Value);
        if (to.HasValue) query = query.Where(r => r.EndDateTime <= to.Value);

        // Orden por fecha de inicio: descendente = más nuevas primero; ascendente = más viejas primero.
        query = sortDescending
            ? query.OrderByDescending(r => r.StartDateTime)
            : query.OrderBy(r => r.StartDateTime);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IEnumerable<CalendarReservationProjection>> GetForCalendarAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default)
    {
        // Proyección directa a DTO plano: el SQL devuelve sólo las 9 columnas que el frontend necesita.
        // Antes traía Reservation+Room+User completos y hacía Include múltiple (cartesiano).
        var query = _dbSet
            .AsNoTracking()
            .Where(r =>
                (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
                r.StartDateTime < to && r.EndDateTime > from);

        if (roomId.HasValue) query = query.Where(r => r.RoomId == roomId.Value);

        return await query
            .OrderBy(r => r.StartDateTime)
            .Select(r => new CalendarReservationProjection(
                r.Id,
                r.StartDateTime,
                r.EndDateTime,
                r.Status,
                r.RoomId,
                r.Room.Name,
                r.Room.Color,
                r.UserId,
                r.User.FullName))
            .ToListAsync(cancellationToken);
    }
}
