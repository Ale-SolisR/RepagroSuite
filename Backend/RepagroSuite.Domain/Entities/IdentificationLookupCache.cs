using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class IdentificationLookupCache : BaseEntity
{
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string NormalizedIdentificationNumber { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? FirstName1 { get; set; }
    public string? FirstName2 { get; set; }
    public string? LastName { get; set; }
    public string? LastName1 { get; set; }
    public string? LastName2 { get; set; }
    public string? LegalName { get; set; }
    public string? Source { get; set; }
    public string? RawResponseJson { get; set; }
    public int ResultCount { get; set; }
    public string? DatabaseDate { get; set; }
    public DateTime LastLookupAt { get; set; } = DateTime.UtcNow;
}
