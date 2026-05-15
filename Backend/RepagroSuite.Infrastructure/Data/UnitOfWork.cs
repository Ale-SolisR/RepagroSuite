using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepagroSuite.Domain.Interfaces;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Repositories;

namespace RepagroSuite.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IRoomRepository? _rooms;
    private IReservationRepository? _reservations;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
