using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class ItTicketRepository : GenericRepository<ItTicket>, IItTicketRepository
{
    public ItTicketRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<ItTicket> Items, int Total)> GetPagedAsync(
        int page, int pageSize, ItTicketType? type, ItTicketStatus? status, string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(t => t.Employee)
            .Include(t => t.ItResponsible)
            .AsNoTracking()
            .AsQueryable();

        if (type.HasValue) query = query.Where(t => t.TicketType == type.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => t.TicketNumber.Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ItTicket?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(t => t.Employee)
            .Include(t => t.ItResponsible)
            .Include(t => t.Details).ThenInclude(d => d.Asset)
            .Include(t => t.Photos)
            .Include(t => t.Signatures)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<ItAssignment?> GetActiveAssignmentAsync(Guid assetId, CancellationToken cancellationToken = default)
        => await _context.ItAssignments
            .Include(a => a.Asset)
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.AssetId == assetId && a.Status == AssignmentStatus.Activa, cancellationToken);
}
