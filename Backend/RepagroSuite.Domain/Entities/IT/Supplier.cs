using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Catálogo normalizado de proveedores. Cada activo puede pertenecer a un proveedor.</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ItAsset> Assets { get; set; } = [];
}
