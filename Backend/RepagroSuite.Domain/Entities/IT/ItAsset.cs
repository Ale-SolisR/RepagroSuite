using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Activo tecnológico. Núcleo del módulo TI. Reemplaza la fila de tblBitacora del Excel,
/// pero normalizado (FKs a catálogos), con número de serie, estado de ciclo de vida y auditoría.
/// No almacena contraseñas (política de seguridad: ver propuesta §11).
/// </summary>
public class ItAsset : BaseEntity
{
    // Identificación
    public string InternalCode { get; set; } = string.Empty;
    public Guid AssetTypeId { get; set; }
    public ItAssetType? AssetType { get; set; }
    public Guid? BrandId { get; set; }
    public ItBrand? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? AssetTag { get; set; }   // Placa/etiqueta

    // Estado
    public ItAssetStatus Status { get; set; } = ItAssetStatus.Available;
    public PhysicalCondition PhysicalCondition { get; set; } = PhysicalCondition.Good;

    // Ubicación / organización
    public Guid? LocationId { get; set; }
    public ItLocation? Location { get; set; }
    public string? LocationDetail { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? CurrentHolderUserId { get; set; }   // Responsable actual (colaborador = User)
    public User? CurrentHolder { get; set; }

    // Compra / garantía
    public DateTime? PurchaseDate { get; set; }
    public string? Supplier { get; set; }
    public decimal? Cost { get; set; }
    public string? Currency { get; set; }   // CRC | USD
    public bool HasWarranty { get; set; }
    public DateTime? WarrantyEndDate { get; set; }

    // Otros
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }   // URL externa o data URL base64 (igual que Room.ImageUrl)

    // Relaciones
    public ItAssetSpec? Spec { get; set; }
    public ICollection<ItAssetHistory> History { get; set; } = [];

    /// <summary>
    /// Máquina de estados del ciclo de vida. Se valida en el dominio (no sólo en la UI).
    /// Disposed es terminal. Ver propuesta §3.
    /// </summary>
    public static bool CanTransition(ItAssetStatus from, ItAssetStatus to)
    {
        if (from == to) return true;
        return from switch
        {
            ItAssetStatus.Available        => to is ItAssetStatus.Assigned or ItAssetStatus.Loaned or ItAssetStatus.UnderMaintenance or ItAssetStatus.UnderReview or ItAssetStatus.Inactive or ItAssetStatus.Disposed,
            ItAssetStatus.Assigned         => to is ItAssetStatus.Returned or ItAssetStatus.UnderReview or ItAssetStatus.UnderRepair or ItAssetStatus.Damaged or ItAssetStatus.Lost or ItAssetStatus.Stolen,
            ItAssetStatus.Loaned           => to is ItAssetStatus.Returned or ItAssetStatus.Available or ItAssetStatus.Damaged or ItAssetStatus.Lost,
            ItAssetStatus.UnderReview      => to is ItAssetStatus.Available or ItAssetStatus.UnderRepair or ItAssetStatus.Disposed,
            ItAssetStatus.UnderMaintenance => to is ItAssetStatus.Available or ItAssetStatus.UnderRepair,
            ItAssetStatus.UnderRepair      => to is ItAssetStatus.Available or ItAssetStatus.Damaged or ItAssetStatus.Disposed,
            ItAssetStatus.Returned         => to is ItAssetStatus.Available or ItAssetStatus.UnderReview,
            ItAssetStatus.Damaged          => to is ItAssetStatus.UnderRepair or ItAssetStatus.Disposed,
            ItAssetStatus.Lost             => to is ItAssetStatus.Disposed or ItAssetStatus.Available,
            ItAssetStatus.Stolen           => to is ItAssetStatus.Disposed,
            ItAssetStatus.Inactive         => to is ItAssetStatus.Available,
            ItAssetStatus.Disposed         => false, // terminal
            _ => false
        };
    }
}
