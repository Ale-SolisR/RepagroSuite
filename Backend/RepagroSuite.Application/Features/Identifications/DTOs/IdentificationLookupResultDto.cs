using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Identifications.DTOs;

public class IdentificationLookupResultDto
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string NormalizedIdentificationNumber { get; set; } = string.Empty;
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationTypeName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? FirstName1 { get; set; }
    public string? FirstName2 { get; set; }
    public string? LastName { get; set; }
    public string? LastName1 { get; set; }
    public string? LastName2 { get; set; }
    public string? LegalName { get; set; }
    public string? Source { get; set; }
    public bool Found { get; set; }
}
