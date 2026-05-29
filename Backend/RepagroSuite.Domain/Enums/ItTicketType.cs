namespace RepagroSuite.Domain.Enums;

/// <summary>Tipos de boleta TI. El prefijo del consecutivo se deriva en ItTicket.TypeCode.</summary>
public enum ItTicketType
{
    Entrega = 0,
    Devolucion = 1,
    Prestamo = 2,
    Mantenimiento = 3,
    Reparacion = 4,
    Traslado = 5,
    CambioResponsable = 6,
    AsignacionAccesorios = 7,
    Baja = 8
}
