using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IItEmployeeRepository : IGenericRepository<ItEmployee>
{
    Task<(IReadOnlyList<ItEmployee> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItEmployee>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<ItEmployee?> GetByNormalizedIdAsync(string normalizedId, CancellationToken cancellationToken = default);
}
