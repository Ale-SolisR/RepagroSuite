using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Firma electrónica de evidencia asociada a una boleta. NO es firma digital certificada
/// legalmente (Ley 8454 CR). Guarda trazabilidad técnica: fecha/hora, IP, user agent, usuario y hash.
/// </summary>
public class ItTicketSignature : BaseEntity
{
    public Guid TicketId { get; set; }
    public ItTicket? Ticket { get; set; }

    public string SignerType { get; set; } = "Colaborador";  // Colaborador | ResponsableTI
    public string? SignerName { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;   // PNG data URL
    public string? Sha256 { get; set; }

    public DateTime SignedAt { get; set; } = BusinessClock.Now;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? AuthenticatedUserId { get; set; }
}
