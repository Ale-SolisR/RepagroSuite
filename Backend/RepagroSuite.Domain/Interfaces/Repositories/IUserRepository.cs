using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<User?> GetByIdentificationNumberAsync(string normalizedIdentificationNumber, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string normalizedEmail, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> IdentificationExistsAsync(string normalizedIdentificationNumber, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetWithRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<(IEnumerable<User> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, UserStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>Sello de sesión del usuario (LastLoginAt): cambia en cada login y sirve para
    /// invalidar tokens de sesiones anteriores (sesión única). Devuelve null si no existe.</summary>
    Task<DateTime?> GetSessionStampAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Revoca todos los refresh tokens activos del usuario (al iniciar una nueva sesión única).</summary>
    Task RevokeActiveRefreshTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}
