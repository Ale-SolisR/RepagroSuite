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
        if (activeOnly == true) query = query.Where(e => e.IsActive);
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
}
