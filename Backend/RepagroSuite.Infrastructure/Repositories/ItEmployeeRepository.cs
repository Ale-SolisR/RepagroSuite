using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class ItEmployeeRepository : GenericRepository<ItEmployee>, IItEmployeeRepository
{
    public ItEmployeeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<ItEmployee> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (activeOnly.HasValue) query = query.Where(e => e.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(e => e.FullName.Contains(s) || e.IdentificationNumber.Contains(s) || (e.Position != null && e.Position.Contains(s)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<ItEmployee>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync(cancellationToken);

    public async Task<ItEmployee?> GetByNormalizedIdAsync(string normalizedId, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(e => e.NormalizedIdentificationNumber == normalizedId, cancellationToken);

    public async Task<IReadOnlyList<ItAssignment>> GetAssignmentsWithDetailsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await _context.ItAssignments
            .AsNoTracking()
            .Include(a => a.Asset).ThenInclude(x => x!.AssetType)
            .Include(a => a.Asset).ThenInclude(x => x!.Photos)
            .Include(a => a.AssignedTicket)
            .Include(a => a.ReturnTicket)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItTicket>> GetTicketsAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await _context.ItTickets
            .AsNoTracking()
            .Include(t => t.Details)
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync(cancellationToken);
}
