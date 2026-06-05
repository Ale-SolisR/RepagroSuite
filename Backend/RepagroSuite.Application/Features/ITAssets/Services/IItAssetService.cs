using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItAssetService
{
    Task<PagedResult<ItAssetListDto>> GetPagedAsync(int page, int pageSize, string? search,
        ItAssetStatus? status, Guid? assetTypeId, Guid? departmentId, Guid? holderId, CancellationToken cancellationToken = default);
    Task<ItAssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItAssetPhotoFileDto?> GetPhotoFileAsync(Guid assetId, Guid photoId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ItAssetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItAssetDto> CreateAsync(CreateItAssetDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItAssetDto> UpdateAsync(Guid id, UpdateItAssetDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    /// <summary>Reactiva un activo inactivo (Dañado/Perdido/Robado/Inactivo) → Disponible. Solo admin (propuesta §13).</summary>
    Task<ItAssetDto> ReactivateAsync(Guid id, ReactivateAssetDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default);
    Task<ItDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportInventoryExcelAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateDashboardPdfAsync(CancellationToken cancellationToken = default);
}
