using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class ItAssetRepository : GenericRepository<ItAsset>, IItAssetRepository
{
    public ItAssetRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<ItAsset> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, ItAssetStatus? status,
        Guid? assetTypeId, Guid? departmentId, Guid? holderId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(a => a.AssetType)
            .Include(a => a.Brand)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Include(a => a.Holder)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a =>
                a.InternalCode.Contains(s) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(s)) ||
                (a.Model != null && a.Model.Contains(s)) ||
                (a.AssetTag != null && a.AssetTag.Contains(s)) ||
                (a.Holder != null && a.Holder.FullName.Contains(s)) ||
                (a.Holder != null && a.Holder.IdentificationNumber.Contains(s)));
        }
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (assetTypeId.HasValue) query = query.Where(a => a.AssetTypeId == assetTypeId.Value);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);
        if (holderId.HasValue) query = query.Where(a => a.CurrentHolderEmployeeId == holderId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<ItAsset?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(a => a.AssetType)
            .Include(a => a.Brand)
            .Include(a => a.Location)
            .Include(a => a.Department)
            .Include(a => a.Holder)
            .Include(a => a.Supplier)
            .Include(a => a.Spec)
            .Include(a => a.Photos)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<ItAsset?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(a => a.Spec)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task ReplacePhotosAsync(Guid assetId, IEnumerable<ItAssetPhoto> photos, CancellationToken cancellationToken = default)
    {
        await _context.Set<ItAssetPhoto>()
            .Where(p => p.AssetId == assetId)
            .ExecuteDeleteAsync(cancellationToken);

        var list = photos.ToList();
        if (list.Count > 0)
            await _context.Set<ItAssetPhoto>().AddRangeAsync(list, cancellationToken);
    }

    public async Task<int> ReleaseAssetsFromVoidedAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _context.ItAssignments
            .Include(a => a.Asset)
            .Include(a => a.AssignedTicket)
            .Where(a => a.Status == AssignmentStatus.Activa
                && a.AssignedTicket != null
                && a.AssignedTicket.Status == ItTicketStatus.Anulada)
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var ticket = assignment.AssignedTicket!;
            var actor = ticket.VoidedBy ?? assignment.UpdatedBy ?? assignment.CreatedBy;
            var closedAt = ticket.VoidedAt ?? BusinessClock.Now;

            assignment.Status = AssignmentStatus.Cerrada;
            assignment.ReturnedAt = closedAt;
            assignment.ClosedReason = "Anulacion";
            assignment.ReturnNotes = ticket.VoidReason;
            assignment.UpdatedAt = BusinessClock.Now;
            assignment.UpdatedBy = actor;

            if (assignment.Asset is { } asset
                && (asset.Status == ItAssetStatus.Assigned || asset.CurrentHolderEmployeeId == assignment.EmployeeId))
            {
                var from = asset.Status;
                asset.Status = ItAssetStatus.Available;
                asset.CurrentHolderEmployeeId = null;
                asset.UpdatedAt = BusinessClock.Now;
                asset.UpdatedBy = actor;

                _context.ItAssetHistory.Add(new ItAssetHistory
                {
                    AssetId = asset.Id,
                    EventType = "STATUS_CHANGED",
                    FromStatus = from,
                    ToStatus = ItAssetStatus.Available,
                    Description = $"Liberado por anulacion de boleta {ticket.TicketNumber}.",
                    OccurredAt = closedAt,
                    PerformedBy = actor,
                    TicketId = ticket.Id,
                    CreatedBy = actor
                });
            }
        }

        return assignments.Count;
    }

    public async Task<IReadOnlyList<ItAssetHistory>> GetHistoryAsync(Guid assetId, CancellationToken cancellationToken = default)
        => await _context.ItAssetHistory
            .AsNoTracking()
            .Where(h => h.AssetId == assetId)
            .OrderByDescending(h => h.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetAllInternalCodesAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Select(a => a.InternalCode).ToListAsync(cancellationToken);

    public async Task<bool> InternalCodeExistsAsync(string internalCode, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(a => a.InternalCode == internalCode && (excludeId == null || a.Id != excludeId), cancellationToken);

    public async Task<bool> SerialExistsAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(a => a.SerialNumber == serialNumber && (excludeId == null || a.Id != excludeId), cancellationToken);

    public async Task<IReadOnlyList<ItAsset>> GetAllForDashboardAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(a => a.AssetType)
            .Include(a => a.Department)
            .Include(a => a.Brand)
            .Include(a => a.Location)
            .Include(a => a.Holder)
            .Include(a => a.Supplier)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItAsset>> GetAllForExportAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(a => a.AssetType)
            .Include(a => a.Department)
            .Include(a => a.Brand)
            .Include(a => a.Location)
            .Include(a => a.Holder)
            .Include(a => a.Supplier)
            .Include(a => a.Spec)
            .AsNoTracking()
            .OrderBy(a => a.InternalCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItAssetType>> GetTypesAsync(CancellationToken cancellationToken = default)
        => await _context.ItAssetTypes.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItBrand>> GetBrandsAsync(CancellationToken cancellationToken = default)
        => await _context.ItBrands.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItLocation>> GetLocationsAsync(CancellationToken cancellationToken = default)
        => await _context.ItLocations.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        => await _context.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default)
        => await _context.Suppliers.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Department>> GetAllDepartmentsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.Name.Contains(s) || (d.Code != null && d.Code.Contains(s)));
        }
        // Activos primero, luego por nombre (el filtro global ya excluye los eliminados lógicamente).
        return await query.OrderByDescending(d => d.IsActive).ThenBy(d => d.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetDepartmentAssetCountsAsync(CancellationToken cancellationToken = default)
        => await _context.ItAssets.AsNoTracking()
            .Where(a => a.DepartmentId != null)
            .GroupBy(a => a.DepartmentId!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count, cancellationToken);

    public async Task<IReadOnlyList<ItBrand>> GetAllBrandsAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.ItBrands.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(b => b.Name.Contains(s));
        }
        return await query.OrderByDescending(b => b.IsActive).ThenBy(b => b.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetBrandAssetCountsAsync(CancellationToken cancellationToken = default)
        => await _context.ItAssets.AsNoTracking()
            .Where(a => a.BrandId != null)
            .GroupBy(a => a.BrandId!.Value)
            .Select(g => new { BrandId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BrandId, x => x.Count, cancellationToken);

    public async Task<IReadOnlyList<Supplier>> GetAllSuppliersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p => p.Name.Contains(s));
        }
        return await query.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetSupplierAssetCountsAsync(CancellationToken cancellationToken = default)
        => await _context.ItAssets.AsNoTracking()
            .Where(a => a.SupplierId != null)
            .GroupBy(a => a.SupplierId!.Value)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count, cancellationToken);
}
