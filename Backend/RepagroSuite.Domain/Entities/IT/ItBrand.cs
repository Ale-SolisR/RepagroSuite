using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Catálogo normalizado de marcas (HP, Dell, Lenovo…). Evita los duplicados del Excel.</summary>
public class ItBrand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ItAsset> Assets { get; set; } = [];
}
