using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Foto del activo (hasta 5). Se guarda como BINARIO en la BD (varbinary(max)), nunca como URL/ruta externa,
/// para que la evidencia no se pueda romper. Es descargable tal cual desde el módulo.
/// </summary>
public class ItAssetPhoto : BaseEntity
{
    public Guid AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    /// <summary>Orden de visualización (0 = principal/miniatura).</summary>
    public int SortOrder { get; set; }

    /// <summary>Bytes de la imagen (la imagen vive completa en la BD).</summary>
    public byte[] Content { get; set; } = [];
    public string MimeType { get; set; } = "image/jpeg";
    public int SizeBytes { get; set; }
    public string? FileName { get; set; }
}
