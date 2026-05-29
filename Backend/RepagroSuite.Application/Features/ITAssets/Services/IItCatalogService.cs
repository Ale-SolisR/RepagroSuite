using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItCatalogService
{
    Task<ItCatalogsDto> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateBrandAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateLocationAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItCatalogItemDto> CreateDepartmentAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default);
}
