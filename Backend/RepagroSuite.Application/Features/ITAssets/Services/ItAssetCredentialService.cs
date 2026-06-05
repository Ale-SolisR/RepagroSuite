using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

/// <summary>
/// Bóveda de credenciales por activo. Cifra los secretos en reposo con <see cref="ISecretProtector"/>
/// (Data Protection) y audita las lecturas/escrituras con <see cref="IAuditService"/>.
/// </summary>
public class ItAssetCredentialService : IItAssetCredentialService
{
    private readonly IUnitOfWork _uow;
    private readonly ISecretProtector _protector;
    private readonly IAuditService _audit;

    public ItAssetCredentialService(IUnitOfWork uow, ISecretProtector protector, IAuditService audit)
    {
        _uow = uow;
        _protector = protector;
        _audit = audit;
    }

    public async Task<IEnumerable<ItAssetCredentialDto>> GetForAssetAsync(Guid assetId, CancellationToken ct = default)
    {
        var creds = await _uow.Repository<ItAssetCredential>().FindAsync(c => c.AssetId == assetId, ct);
        return creds.OrderBy(c => c.SortOrder).ThenBy(c => c.Label).Select(MapToDto);
    }

    public async Task<ItAssetCredentialDto> CreateAsync(Guid assetId, CreateItAssetCredentialDto dto, Guid userId, CancellationToken ct = default)
    {
        var asset = await _uow.Repository<ItAsset>().GetByIdAsync(assetId, ct)
            ?? throw new KeyNotFoundException("Activo no encontrado.");

        var label = dto.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("La cuenta (etiqueta) es obligatoria.");

        var existing = await _uow.Repository<ItAssetCredential>().FindAsync(c => c.AssetId == assetId, ct);
        var nextOrder = existing.Any() ? existing.Max(c => c.SortOrder) + 1 : 0;

        var cred = new ItAssetCredential
        {
            AssetId = asset.Id,
            Type = dto.Type,
            Label = label,
            Username = dto.Username?.Trim(),
            SecretEncrypted = string.IsNullOrEmpty(dto.Secret) ? null : _protector.Protect(dto.Secret),
            Host = dto.Host?.Trim(),
            Notes = dto.Notes?.Trim(),
            SortOrder = nextOrder,
            CreatedBy = userId,
        };
        await _uow.Repository<ItAssetCredential>().AddAsync(cred, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(userId, "CREDENTIAL_CREATE", nameof(ItAssetCredential), cred.Id.ToString(),
            newValues: new { cred.AssetId, cred.Type, cred.Label, cred.Username }, module: "TI", cancellationToken: ct);

        return MapToDto(cred);
    }

    public async Task<ItAssetCredentialDto> UpdateAsync(Guid assetId, Guid credentialId, UpdateItAssetCredentialDto dto, Guid userId, CancellationToken ct = default)
    {
        var cred = await _uow.Repository<ItAssetCredential>().GetByIdAsync(credentialId, ct);
        if (cred is null || cred.AssetId != assetId) throw new KeyNotFoundException("Credencial no encontrada.");

        var label = dto.Label?.Trim();
        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("La cuenta (etiqueta) es obligatoria.");

        cred.Type = dto.Type;
        cred.Label = label;
        cred.Username = dto.Username?.Trim();
        cred.Host = dto.Host?.Trim();
        cred.Notes = dto.Notes?.Trim();
        // Secreto: borrar / reemplazar / conservar.
        if (dto.ClearSecret) cred.SecretEncrypted = null;
        else if (!string.IsNullOrEmpty(dto.Secret)) cred.SecretEncrypted = _protector.Protect(dto.Secret);
        cred.UpdatedBy = userId;

        _uow.Repository<ItAssetCredential>().Update(cred);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(userId, "CREDENTIAL_UPDATE", nameof(ItAssetCredential), cred.Id.ToString(),
            newValues: new { cred.AssetId, cred.Type, cred.Label, cred.Username }, module: "TI", cancellationToken: ct);

        return MapToDto(cred);
    }

    public async Task DeleteAsync(Guid assetId, Guid credentialId, Guid userId, CancellationToken ct = default)
    {
        var cred = await _uow.Repository<ItAssetCredential>().GetByIdAsync(credentialId, ct);
        if (cred is null || cred.AssetId != assetId) throw new KeyNotFoundException("Credencial no encontrada.");

        _uow.Repository<ItAssetCredential>().SoftDelete(cred, userId);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(userId, "CREDENTIAL_DELETE", nameof(ItAssetCredential), cred.Id.ToString(),
            oldValues: new { cred.AssetId, cred.Label }, module: "TI", cancellationToken: ct);
    }

    public async Task<ItAssetCredentialSecretDto> RevealSecretAsync(Guid assetId, Guid credentialId, Guid userId,
        string? ipAddress, string? userAgent, CancellationToken ct = default)
    {
        var cred = await _uow.Repository<ItAssetCredential>().GetByIdAsync(credentialId, ct);
        if (cred is null || cred.AssetId != assetId) throw new KeyNotFoundException("Credencial no encontrada.");

        // Auditoría OBLIGATORIA: queda registrado quién reveló qué credencial y cuándo (nunca el valor).
        await _audit.LogAsync(userId, "CREDENTIAL_REVEAL", nameof(ItAssetCredential), cred.Id.ToString(),
            newValues: new { cred.AssetId, cred.Label, cred.Username }, module: "TI",
            ipAddress: ipAddress, userAgent: userAgent, cancellationToken: ct);

        return new ItAssetCredentialSecretDto { Id = cred.Id, Secret = _protector.Unprotect(cred.SecretEncrypted) };
    }

    private static ItAssetCredentialDto MapToDto(ItAssetCredential c) => new()
    {
        Id = c.Id,
        Type = c.Type,
        TypeName = TypeName(c.Type),
        Label = c.Label,
        Username = c.Username,
        HasSecret = !string.IsNullOrEmpty(c.SecretEncrypted),
        Host = c.Host,
        Notes = c.Notes,
        SortOrder = c.SortOrder,
    };

    private static string TypeName(ItCredentialType t) => t switch
    {
        ItCredentialType.AnyDesk => "AnyDesk",
        ItCredentialType.Windows => "Windows",
        ItCredentialType.Microsoft365 => "Microsoft 365",
        ItCredentialType.Email => "Correo",
        ItCredentialType.Bios => "BIOS",
        ItCredentialType.Network => "Red / Router",
        ItCredentialType.Application => "Aplicación",
        _ => "Otro",
    };
}
