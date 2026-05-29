namespace RepagroSuite.Domain.Enums;

/// <summary>Ciclo de la boleta. Una boleta Emitida es inmutable: sólo se anula (propuesta §6).</summary>
public enum ItTicketStatus
{
    Borrador = 0,
    PendienteFirma = 1,
    Firmada = 2,
    Emitida = 3,
    Anulada = 4
}
