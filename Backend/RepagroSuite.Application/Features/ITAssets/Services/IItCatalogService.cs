using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItCatalogService
{
    Task<ItCatalogsDto> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateBrandAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateLocationAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateDepartmentAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateSupplierAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);

    // Mantenimiento (CRUD) de departamentos desde el formulario de activo.
    Task<IEnumerable<ItDepartmentDto>> GetDepartmentsAdminAsync(string? search, CancellationToken cancellationToken = default);
    Task<ItDepartmentDto> UpdateDepartmentAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<ItDepartmentDto> SetDepartmentStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default);

    // Mantenimiento (CRUD) de marcas desde el formulario de activo.
    Task<IEnumerable<ItBrandDto>> GetBrandsAdminAsync(string? search, CancellationToken cancellationToken = default);
    Task<ItBrandDto> UpdateBrandAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<ItBrandDto> SetBrandStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default);

    // Mantenimiento (CRUD) de proveedores desde el formulario de activo.
    Task<IEnumerable<ItSupplierDto>> GetSuppliersAdminAsync(string? search, CancellationToken cancellationToken = default);
    Task<ItSupplierDto> UpdateSupplierAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<ItSupplierDto> SetSupplierStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default);
}
