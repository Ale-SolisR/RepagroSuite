using Microsoft.EntityFrameworkCore;
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
        Guid? assetTypeId, Guid? departmentId, CancellationToken cancellationToken = default)
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
                (a.AssetTag != null && a.AssetTag.Contains(s)));
        }
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (assetTypeId.HasValue) query = query.Where(a => a.AssetTypeId == assetTypeId.Value);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);

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
            .Include(a => a.Spec)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

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
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItAsset>> GetAllForExportAsync(CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(a => a.AssetType)
            .Include(a => a.Department)
            .Include(a => a.Brand)
            .Include(a => a.Location)
            .Include(a => a.Holder)
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
}
