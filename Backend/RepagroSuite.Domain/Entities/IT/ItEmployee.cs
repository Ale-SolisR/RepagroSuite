using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Colaborador del módulo TI. Es la persona responsable de un activo y la contraparte de las
/// boletas. Se crea con cédula + nombre (autocompletado vía consulta de identificación) + puesto.
/// Es independiente de <see cref="User"/> (que requiere registro/credenciales).
/// </summary>
public class ItEmployee : BaseEntity
{
    public IdentificationType IdentificationType { get; set; } = IdentificationType.PhysicalId;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string NormalizedIdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}
