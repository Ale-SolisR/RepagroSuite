using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Interfaces.Repositories;

namespace RepagroSuite.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoomRepository Rooms { get; }
    IReservationRepository Reservations { get; }
    IItAssetRepository ItAssets { get; }
    IItTicketRepository ItTickets { get; }

    /// <summary>
    /// Repositorio genérico para entidades que no tienen repositorio especializado
    /// (p.ej. el módulo TI). Cachea una instancia por tipo dentro del scope.
    /// </summary>
    IGenericRepository<T> Repository<T>() where T : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // Ejecuta 'operation' dentro de una transacción gestionada por la execution strategy de EF Core.
    // Necesario porque EnableRetryOnFailure no permite transacciones iniciadas manualmente: toda la
    // unidad (lock + validación + insert) debe ser reintentable como un bloque.
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    // Lock exclusivo por sala usando sp_getapplock (SQL Server).
    // Se libera automáticamente al hacer commit/rollback de la transacción actual.
    // Debe llamarse DENTRO de una transacción ya abierta.
    Task AcquireRoomLockAsync(Guid roomId, int timeoutMs = 5000, CancellationToken cancellationToken = default);
}
