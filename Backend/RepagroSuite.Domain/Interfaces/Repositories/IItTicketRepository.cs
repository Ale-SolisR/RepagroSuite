using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IItTicketRepository : IGenericRepository<ItTicket>
{
    Task<(IReadOnlyList<ItTicket> Items, int Total)> GetPagedAsync(
        int page, int pageSize, ItTicketType? type, ItTicketStatus? status, string? search,
        Guid? employeeId, CancellationToken cancellationToken = default);

    /// <summary>Boleta con detalles (+activo), fotos, firmas y contrapartes.</summary>
    Task<ItTicket?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Asignación activa de un activo (la única posible), o null.</summary>
    Task<ItAssignment?> GetActiveAssignmentAsync(Guid assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asignaciones ligadas a una boleta (sea su entrega o su cierre), con activo y contrapartes,
    /// para construir la cadena de trazabilidad Entrega ↔ Devolución/Desasignación/Incidente.
    /// </summary>
    Task<IReadOnlyList<ItAssignment>> GetChainAssignmentsAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
