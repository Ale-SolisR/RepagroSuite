using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Ubicación física del activo (sede, oficina). El detalle fino va en ItAsset.LocationDetail.</summary>
public class ItLocation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ItAsset> Assets { get; set; } = [];
}
