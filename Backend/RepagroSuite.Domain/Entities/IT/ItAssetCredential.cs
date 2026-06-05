using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Credencial operativa asociada a un activo (bóveda por equipo): N cuentas con usuario y
/// contraseña (AnyDesk, Windows, correo, router, etc.). El secreto se guarda CIFRADO en reposo
/// (ISecretProtector / Data Protection); nunca en texto plano y nunca se devuelve en listados.
/// A diferencia de las contraseñas de login (hash BCrypt, irreversibles), estas SON recuperables
/// a propósito por personal autorizado, por eso se cifran (no se hashean).
/// </summary>
public class ItAssetCredential : BaseEntity
{
    public Guid AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    public ItCredentialType Type { get; set; } = ItCredentialType.Other;
    public string Label { get; set; } = string.Empty;   // "Cuenta" — etiqueta legible
    public string? Username { get; set; }
    public string? SecretEncrypted { get; set; }         // contraseña cifrada (prefijo "enc:v1:")
    public string? Host { get; set; }                    // IP/URL opcional (router, portal…)
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
