using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItAssetCredentialService
{
    Task<IEnumerable<ItAssetCredentialDto>> GetForAssetAsync(Guid assetId, CancellationToken ct = default);
    Task<ItAssetCredentialDto> CreateAsync(Guid assetId, CreateItAssetCredentialDto dto, Guid userId, CancellationToken ct = default);
    Task<ItAssetCredentialDto> UpdateAsync(Guid assetId, Guid credentialId, UpdateItAssetCredentialDto dto, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(Guid assetId, Guid credentialId, Guid userId, CancellationToken ct = default);

    /// <summary>Devuelve el secreto descifrado. Debe gatearse por permiso en el controlador y se audita.</summary>
    Task<ItAssetCredentialSecretDto> RevealSecretAsync(Guid assetId, Guid credentialId, Guid userId,
        string? ipAddress, string? userAgent, CancellationToken ct = default);
}
