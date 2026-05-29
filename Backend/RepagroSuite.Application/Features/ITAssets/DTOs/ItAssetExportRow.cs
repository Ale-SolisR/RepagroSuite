namespace RepagroSuite.Application.Features.ITAssets.DTOs;

/// <summary>Fila plana de un activo para exportación a Excel. Aplana catálogos y especificaciones.
/// NO incluye contraseñas (política de seguridad §11).</summary>
public class ItAssetExportRow
{
    public string InternalCode { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? LocationDetail { get; set; }
    public string? Department { get; set; }
    public string? Holder { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public string? Supplier { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public string Warranty { get; set; } = string.Empty;
    public DateTime? WarrantyEndDate { get; set; }

    public string? OperatingSystem { get; set; }
    public string? Processor { get; set; }
    public int? RamGb { get; set; }
    public int? DiskGb { get; set; }
    public string? IpAddress { get; set; }
    public string? AnyDeskId { get; set; }
    public string? Microsoft365User { get; set; }
    public string? Notes { get; set; }
}
