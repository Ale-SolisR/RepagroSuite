using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItImportService
{
    /// <summary>Importa activos válidos desde la bitácora. Idempotente por código interno (omite existentes).</summary>
    Task<ItImportResultDto> ImportAssetsAsync(IEnumerable<ItAssetImportRow> rows, Guid actorUserId, CancellationToken cancellationToken = default);
}
