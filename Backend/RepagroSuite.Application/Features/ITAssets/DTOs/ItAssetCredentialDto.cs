using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.DTOs;

/// <summary>Lectura para la lista de credenciales. NUNCA incluye el secreto, solo si existe.</summary>
public class ItAssetCredentialDto
{
    public Guid Id { get; set; }
    public ItCredentialType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Username { get; set; }
    public bool HasSecret { get; set; }
    public string? Host { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Valor descifrado del secreto. Solo se devuelve por el endpoint de "revelar" (permiso + auditoría).</summary>
public class ItAssetCredentialSecretDto
{
    public Guid Id { get; set; }
    public string? Secret { get; set; }
}

public class CreateItAssetCredentialDto
{
    public ItCredentialType Type { get; set; } = ItCredentialType.Other;
    public string Label { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Secret { get; set; }   // texto plano entrante; el servidor lo cifra al guardar
    public string? Host { get; set; }
    public string? Notes { get; set; }
}

public class UpdateItAssetCredentialDto : CreateItAssetCredentialDto
{
    // Manejo del secreto en edición:
    //  - Secret nulo/omitido  → se conserva el actual.
    //  - Secret con valor      → se reemplaza (cifrado).
    //  - ClearSecret = true    → se borra el secreto.
    public bool ClearSecret { get; set; }
}
