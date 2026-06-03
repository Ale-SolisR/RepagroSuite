using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.DTOs;

// ─── Lectura ────────────────────────────────────────────────────────────────────

/// <summary>Fila ligera para el listado/tabla de inventario.</summary>
public class ItAssetListDto
{
    public Guid Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string AssetTypeName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public ItAssetStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public string? DepartmentName { get; set; }
    public string? CurrentHolderName { get; set; }
}

/// <summary>Ficha completa del activo (detalle + edición).</summary>
public class ItAssetDto : ItAssetListDto
{
    public Guid AssetTypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string? AssetTag { get; set; }
    public PhysicalCondition PhysicalCondition { get; set; }
    public string PhysicalConditionName { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public string? LocationDetail { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? CurrentHolderEmployeeId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public bool HasWarranty { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Notes { get; set; }
    public ItAssetSpecDto? Spec { get; set; }
    public List<ItAssetPhotoDto> Photos { get; set; } = [];   // Galería completa (binario en BD).
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

/// <summary>Bytes de una foto para descarga directa (streaming binario).</summary>
public class ItAssetPhotoFileDto
{
    public byte[] Content { get; set; } = [];
    public string MimeType { get; set; } = "image/jpeg";
    public string FileName { get; set; } = "foto.jpg";
}

/// <summary>Foto del activo para lectura: data URL para previsualizar + metadata para descargar.</summary>
public class ItAssetPhotoDto
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    /// <summary>Data URL (base64) generada al leer desde el binario; sólo para mostrar en el navegador.</summary>
    public string Url { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/jpeg";
    public int SizeBytes { get; set; }
    public string? FileName { get; set; }
}

public class ItAssetSpecDto
{
    public string? OperatingSystem { get; set; }
    public string? Processor { get; set; }
    public int? RamGb { get; set; }
    public int? DiskGb { get; set; }
    public string? MacEthernet { get; set; }
    public string? MacWifi { get; set; }
    public string? IpAddress { get; set; }
    public string? DomainName { get; set; }
    public string? AnyDeskId { get; set; }
    public string? Microsoft365User { get; set; }
    public string? AntivirusStatus { get; set; }
    public string? TechNotes { get; set; }
}

public class ItAssetHistoryDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public ItAssetStatus? FromStatus { get; set; }
    public ItAssetStatus? ToStatus { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
}

// ─── Escritura ──────────────────────────────────────────────────────────────────

public class CreateItAssetDto
{
    public string InternalCode { get; set; } = string.Empty;
    public Guid AssetTypeId { get; set; }
    public Guid? BrandId { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }
    public PhysicalCondition PhysicalCondition { get; set; } = PhysicalCondition.Good;
    public Guid? LocationId { get; set; }
    public string? LocationDetail { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? CurrentHolderEmployeeId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }
    public bool HasWarranty { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Notes { get; set; }
    /// <summary>Galería de fotos a guardar (hasta 5 data URLs base64; el servidor las almacena como binario). Si viene, reemplaza las fotos del activo.</summary>
    public List<string>? Photos { get; set; }
    public ItAssetSpecDto? Spec { get; set; }
}

public class UpdateItAssetDto : CreateItAssetDto
{
    public string? RowVersion { get; set; }
}

public class ChangeItAssetStatusDto
{
    public ItAssetStatus Status { get; set; }
    public string? Reason { get; set; }
}
