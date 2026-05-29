using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Departamento / unidad de negocio al que pertenece el activo.</summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ItAsset> Assets { get; set; } = [];
}
