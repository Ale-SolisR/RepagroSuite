using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IItEmployeeRepository : IGenericRepository<ItEmployee>
{
    Task<(IReadOnlyList<ItEmployee> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItEmployee>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ItEmployee?> GetByNormalizedIdAsync(string normalizedId, CancellationToken cancellationToken = default);

    /// <summary>Asignaciones del colaborador (activas e históricas) con activo, tipo y boletas de entrega/cierre.</summary>
    Task<IReadOnlyList<ItAssignment>> GetAssignmentsWithDetailsAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>Boletas en las que el colaborador es contraparte, con sus detalles (para contar activos).</summary>
    Task<IReadOnlyList<ItTicket>> GetTicketsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
