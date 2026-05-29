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

/// <summary>Catálogos agregados para poblar selects del formulario de activo en una sola llamada.</summary>
public class ItCatalogsDto
{
    public IEnumerable<ItAssetTypeDto> Types { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Brands { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Locations { get; set; } = [];
    public IEnumerable<ItCatalogItemDto> Departments { get; set; } = [];
}
