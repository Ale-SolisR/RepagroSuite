using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null, string? userAgent = null);
    Guid? GetUserIdFromToken(string token);
}
