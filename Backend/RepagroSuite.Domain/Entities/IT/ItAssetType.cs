using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Catálogo de tipos de activo TI (Laptop, Desktop, Impresora, Switch, Licencia…).</summary>
public class ItAssetType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    /// <summary>Si exige número de serie obligatorio al registrar.</summary>
    public bool RequiresSerial { get; set; } = true;
    /// <summary>Si el tipo puede asignarse a un colaborador.</summary>
    public bool IsAssignable { get; set; } = true;
    /// <summary>Si maneja especificaciones de cómputo (CPU/RAM/Disco/IP…).</summary>
    public bool HasComputeSpecs { get; set; }
    public string? IconName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ItAsset> Assets { get; set; } = [];
}
