using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Línea de tiempo del activo: altas, cambios de estado, ediciones relevantes.
/// Complementa AuditLog (que guarda el diff campo a campo) con un relato legible por el usuario.
/// </summary>
public class ItAssetHistory : BaseEntity
{
    public Guid AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    public string EventType { get; set; } = string.Empty;   // CREATED | STATUS_CHANGED | UPDATED | ...
    public ItAssetStatus? FromStatus { get; set; }
    public ItAssetStatus? ToStatus { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; } = BusinessClock.Now;
    public Guid? PerformedBy { get; set; }

    /// <summary>Boleta que originó el evento (entrega, devolución, desasignación, deterioro, pérdida/robo). Null para altas/reactivaciones.</summary>
    public Guid? TicketId { get; set; }
}
