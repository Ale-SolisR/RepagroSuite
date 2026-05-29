using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Foto de evidencia de una boleta (hasta 3). Por ahora se guarda como data URL base64 en BD
/// (igual que Room.ImageUrl); el diseño deja la metadata lista para migrar a blob externo (propuesta §7).
/// </summary>
public class ItTicketPhoto : BaseEntity
{
    public Guid TicketId { get; set; }
    public ItTicket? Ticket { get; set; }
    public Guid? AssetId { get; set; }

    public string ImageBase64 { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public int? SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public Guid? UploadedBy { get; set; }
}
