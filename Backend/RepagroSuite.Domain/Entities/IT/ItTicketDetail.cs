using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>Línea de boleta: un activo incluido o un accesorio/observación libre.</summary>
public class ItTicketDetail : BaseEntity
{
    public Guid TicketId { get; set; }
    public ItTicket? Ticket { get; set; }

    public Guid? AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    public string LineType { get; set; } = "ASSET";   // ASSET | ACCESSORY
    public string? Description { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Condition { get; set; }
}
