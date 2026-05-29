namespace RepagroSuite.Application.Features.ITAssets.DTOs;

public class ItEmployeeDto
{
    public Guid Id { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItEmployeeDto
{
    /// <summary>Cédula. Se normaliza y se valida que no exista otro colaborador con la misma.</summary>
    public string IdentificationNumber { get; set; } = string.Empty;
    /// <summary>Nombre (autocompletado en el front vía /identifications/lookup; el usuario puede ajustarlo).</summary>
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateItEmployeeDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
