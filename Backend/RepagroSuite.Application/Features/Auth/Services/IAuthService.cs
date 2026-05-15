using RepagroSuite.Application.Features.Auth.DTOs;

namespace RepagroSuite.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task ForcedChangePasswordAsync(Guid userId, ForcedChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
}
