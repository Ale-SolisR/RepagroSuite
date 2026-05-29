using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Consecutivo de boletas por tipo y año. Se incrementa con bloqueo transaccional (UPDLOCK)
/// dentro de la misma transacción de la boleta para evitar duplicados/saltos (propuesta §10).
/// </summary>
public class ItDocumentSequence : BaseEntity
{
    public string TicketTypeCode { get; set; } = string.Empty;  // ENT, DEV, REP, BAJ…
    public int Year { get; set; }
    public string Prefix { get; set; } = "TI";
    public long LastNumber { get; set; }
}
