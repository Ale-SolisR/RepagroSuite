using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Especificaciones técnicas del activo (1:1). Separadas del activo base para no inflarlo.
/// NO contiene contraseñas: AnyDeskId es el identificador, nunca el secreto (propuesta §1.4 / §11).
/// </summary>
public class ItAssetSpec : BaseEntity
{
    public Guid AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    public string? OperatingSystem { get; set; }
    public string? Processor { get; set; }
    public int? RamGb { get; set; }
    public int? DiskGb { get; set; }
    public string? MacEthernet { get; set; }
    public string? MacWifi { get; set; }
    public string? IpAddress { get; set; }
    public string? DomainName { get; set; }
    public string? AnyDeskId { get; set; }
    public string? Microsoft365User { get; set; }
    public string? AntivirusStatus { get; set; }
    public string? TechNotes { get; set; }
}
