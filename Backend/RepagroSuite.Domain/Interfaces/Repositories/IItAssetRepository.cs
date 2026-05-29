using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IItAssetRepository : IGenericRepository<ItAsset>
{
    /// <summary>Listado paginado con detalles (tipo, marca, ubicación, depto, responsable) ya incluidos.</summary>
    Task<(IReadOnlyList<ItAsset> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, ItAssetStatus? status,
        Guid? assetTypeId, Guid? departmentId, CancellationToken cancellationToken = default);

    /// <summary>Activo con todas sus relaciones (incluye Spec) para la ficha de detalle/edición.</summary>
    Task<ItAsset?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItAssetHistory>> GetHistoryAsync(Guid assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAllInternalCodesAsync(CancellationToken cancellationToken = default);
    Task<bool> InternalCodeExistsAsync(string internalCode, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> SerialExistsAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Todos los activos vivos con Tipo, Depto, Marca, Ubicación y Responsable, para KPIs del dashboard.</summary>
    Task<IReadOnlyList<ItAsset>> GetAllForDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>Todos los activos con relaciones completas (incluye Spec) ordenados por código, para exportación a Excel.</summary>
    Task<IReadOnlyList<ItAsset>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    // Catálogos
    Task<IReadOnlyList<ItAssetType>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItBrand>> GetBrandsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItLocation>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
}
