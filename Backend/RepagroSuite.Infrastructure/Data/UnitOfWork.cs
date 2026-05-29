using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RepagroSuite.Domain.Common;
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
    private IItAssetRepository? _itAssets;
    private IItTicketRepository? _itTickets;
    private IItEmployeeRepository? _itEmployees;
    private readonly Dictionary<Type, object> _genericRepos = [];

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRoomRepository Rooms => _rooms ??= new RoomRepository(_context);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_context);
    public IItAssetRepository ItAssets => _itAssets ??= new ItAssetRepository(_context);
    public IItTicketRepository ItTickets => _itTickets ??= new ItTicketRepository(_context);
    public IItEmployeeRepository ItEmployees => _itEmployees ??= new ItEmployeeRepository(_context);

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        if (_genericRepos.TryGetValue(typeof(T), out var existing))
            return (IGenericRepository<T>)existing;

        var repo = new GenericRepository<T>(_context);
        _genericRepos[typeof(T)] = repo;
        return repo;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // La execution strategy reintenta TODO el bloque ante fallos transitorios (deadlocks,
        // timeouts de red contra la BD remota). Cada reintento abre una transacción nueva.
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task AcquireRoomLockAsync(Guid roomId, int timeoutMs = 5000, CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("AcquireRoomLockAsync requiere una transacción activa. Llame primero a BeginTransactionAsync.");

        var resourceParam = new SqlParameter("@Resource", $"room-reservation:{roomId}");
        var timeoutParam = new SqlParameter("@LockTimeout", timeoutMs);
        var resultParam = new SqlParameter("@result", System.Data.SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        // sp_getapplock devuelve >= 0 si adquirió el lock, < 0 si falló.
        // Se libera automáticamente al hacer commit/rollback de la transacción.
        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @Resource, @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = @LockTimeout",
            resourceParam, timeoutParam, resultParam);

        var code = (int)(resultParam.Value ?? -1);
        if (code < 0)
            throw new InvalidOperationException(
                "El sistema está procesando otra solicitud para esta sala. Intente nuevamente en unos segundos.");
    }

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
