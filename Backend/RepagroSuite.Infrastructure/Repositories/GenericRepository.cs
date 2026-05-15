using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => predicate == null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void Remove(T entity) => _dbSet.Remove(entity);

    public void SoftDelete(T entity, Guid? deletedBy = null)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
        _dbSet.Update(entity);
    }

    public IQueryable<T> Query() => _dbSet.AsQueryable();
}
