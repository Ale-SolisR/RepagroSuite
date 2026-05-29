namespace RepagroSuite.Application.Features.ITAssets.DTOs;

/// <summary>Fila cruda de la bitácora Excel (solo activos válidos). La contraseña NUNCA se importa.</summary>
public class ItAssetImportRow
{
    public string? Codigo { get; set; }
    public string? Dispositivo { get; set; }
    public string? Unidad { get; set; }
    public string? Ubicacion { get; set; }
    public string? DetalleUbic { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Responsable { get; set; }
    public string? AnyDesk { get; set; }
    public string? Kaspersky { get; set; }
    public string? M365 { get; set; }
    public string? Usuario365 { get; set; }
    public string? Comentarios { get; set; }
}

public class ItImportResultDto
{
    public int Created { get; set; }
    public int SkippedExisting { get; set; }
    public int BrandsCreated { get; set; }
    public int LocationsCreated { get; set; }
    public int DepartmentsCreated { get; set; }
    public List<string> Warnings { get; set; } = [];
}
