namespace RepagroSuite.Application.Features.ITAssets.DTOs;

public class ItAssetTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool RequiresSerial { get; set; }
    public bool IsAssignable { get; set; }
    public bool HasComputeSpecs { get; set; }
    public string? IconName { get; set; }
}

/// <summary>Item genérico de catálogo (marca, ubicación, departamento).</summary>
public class ItCatalogItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class CreateCatalogItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

/// <summary>Departamento con su estado y uso, para el mantenimiento (CRUD) desde el formulario de activo.</summary>
public class ItDepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Cantidad de activos (no eliminados) asignados a este departamento. Informa antes de inactivar.</summary>
    public int AssetCount { get; set; }
}

/// <summary>Marca con su estado y uso, para el mantenimiento (CRUD) desde el formulario de activo.</summary>
public class ItBrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    /// <summary>Cantidad de activos (no eliminados) de esta marca. Informa antes de inactivar.</summary>
    public int AssetCount { get; set; }
}

/// <summary>Proveedor con su estado y uso, para el mantenimiento (CRUD) desde el formulario de activo.</summary>
public class ItSupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    /// <summary>Cantidad de activos (no eliminados) de este proveedor. Informa antes de inactivar.</summary>
    public int AssetCount { get; set; }
}

/// <summary>Cambio de estado activo/inactivo para items de catálogo (departamentos, marcas, proveedores…).</summary>
public class UpdateCatalogStatusDto
{
    public bool IsActive { get; set; }
}

/// <summary>Catálogos agregados para poblar selects del formulario de activo en una sola llamada.</summary>
public class ItCatalogsDto
{
    public IEnumerable<ItAssetTypeDto> Types { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Brands { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Locations { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Departments { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Suppliers { get; set; } = [];
}
