using RepagroSuite.Domain.Interfaces.Repositories;

namespace RepagroSuite.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRoomRepository Rooms { get; }
    IReservationRepository Reservations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
