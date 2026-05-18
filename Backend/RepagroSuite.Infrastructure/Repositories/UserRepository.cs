using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces.Repositories;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<User?> GetByIdentificationNumberAsync(string normalizedNumber, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.NormalizedIdentificationNumber == normalizedNumber, cancellationToken);

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);

    public async Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(u => u.NormalizedEmail == normalizedEmail && (excludeUserId == null || u.Id != excludeUserId), cancellationToken);

    public async Task<bool> IdentificationExistsAsync(string normalizedNumber, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(u => u.NormalizedIdentificationNumber == normalizedNumber && (excludeUserId == null || u.Id != excludeUserId), cancellationToken);

    public async Task<IEnumerable<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default)
        => await _dbSet.Where(u => u.Status == status).OrderByDescending(u => u.CreatedAt).ToListAsync(cancellationToken);

    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(u => u.RefreshTokens.Where(rt => !rt.IsDeleted))
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => await _dbSet
            .Include(u => u.RefreshTokens.Where(rt => !rt.IsDeleted))
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == token && !rt.IsDeleted), cancellationToken);

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        => await _context.Set<RefreshToken>().AddAsync(token, cancellationToken);

    public async Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search = null, UserStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToUpper();
            query = query.Where(u =>
                u.NormalizedEmail.Contains(s) ||
                u.FullName.ToUpper().Contains(s) ||
                u.NormalizedIdentificationNumber.Contains(s));
        }

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
