using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.Auth.DTOs;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public AuthService(
        IUnitOfWork uow,
        IPasswordService passwordService,
        ITokenService tokenService,
        IEmailService emailService,
        IAuditService auditService)
    {
        _uow = uow;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailService = emailService;
        _auditService = auditService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToUpperInvariant();
        var user = await _uow.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            await _auditService.LogAsync(null, "LOGIN_FAILED", module: "Auth", success: false, errorMessage: "Credenciales inválidas", ipAddress: ipAddress);
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt > BusinessClock.Now)
            throw new UnauthorizedAccessException("Su usuario está temporalmente bloqueado. Intente más tarde.");

        if (!_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEndAt = BusinessClock.Now.AddMinutes(15);
                user.FailedLoginAttempts = 0;
            }
            user.UpdatedAt = BusinessClock.Now;
            await _uow.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(user.Id, "LOGIN_FAILED", module: "Auth", success: false, errorMessage: "Contraseña incorrecta", ipAddress: ipAddress);
            throw new UnauthorizedAccessException("Credenciales inválidas.");
        }

        if (user.Status == UserStatus.Pending)
            throw new UnauthorizedAccessException("Su usuario aún está pendiente de aprobación.");

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedAccessException("Su usuario está bloqueado o inactivo. Contacte al administrador.");

        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = BusinessClock.Now;
        user.UpdatedAt = BusinessClock.Now;

        var roles = user.UserRoles.Select(ur => ur.Role.NormalizedName).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress, userAgent);

        // Sesión única: revoca los refresh tokens de cualquier sesión previa del usuario. Combinado
        // con el sello de sesión (LastLoginAt en el claim "lat"), el inicio de sesión más reciente
        // invalida de inmediato cualquier otra sesión activa.
        await _uow.Users.RevokeActiveRefreshTokensAsync(user.Id, "Reemplazada por una nueva sesión", cancellationToken);

        await _uow.Users.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(user.Id, "LOGIN_SUCCESS", module: "Auth", ipAddress: ipAddress, userAgent: userAgent);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            // El access token JWT expira en UTC (la validación del token compara contra UTC).
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            MustChangePassword = user.MustChangePassword,
            User = new UserSessionDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles,
                Permissions = permissions,
                IsMaster = user.Id == new Guid("33333333-3333-3333-3333-333333333333")
            }
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Token de refresco inválido.");

        var existingToken = user.RefreshTokens.First(rt => rt.Token == refreshToken);

        if (!existingToken.IsActive)
            throw new UnauthorizedAccessException("El token de refresco ha expirado o fue revocado.");

        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress, userAgent);
        existingToken.IsRevoked = true;
        existingToken.RevokedAt = BusinessClock.Now;
        existingToken.RevokedReason = "Rotado";
        existingToken.ReplacedByToken = newRefreshToken.Token;

        var roles = user.UserRoles.Select(ur => ur.Role.NormalizedName).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);

        await _uow.Users.AddRefreshTokenAsync(newRefreshToken, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            // El access token JWT expira en UTC (la validación del token compara contra UTC).
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
            MustChangePassword = user.MustChangePassword,
            User = new UserSessionDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles,
                Permissions = permissions,
                IsMaster = user.Id == new Guid("33333333-3333-3333-3333-333333333333")
            }
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByRefreshTokenAsync(refreshToken, cancellationToken);
        if (user == null) return;

        var token = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken);
        if (token != null && token.IsActive)
        {
            token.IsRevoked = true;
            token.RevokedAt = BusinessClock.Now;
            token.RevokedReason = "Logout";
            await _uow.SaveChangesAsync(cancellationToken);
        }

        await _auditService.LogAsync(user.Id, "LOGOUT", module: "Auth");
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash!))
            throw new InvalidOperationException("La contraseña actual es incorrecta.");

        if (dto.NewPassword == dto.CurrentPassword)
            throw new InvalidOperationException("La nueva contraseña no puede ser igual a la actual.");

        if (!_passwordService.IsPasswordPolicyCompliant(dto.NewPassword, out var violations))
            throw new InvalidOperationException(string.Join(", ", violations));

        user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
        user.LastPasswordChangedAt = BusinessClock.Now;
        user.MustChangePassword = false;
        user.UpdatedAt = BusinessClock.Now;
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(userId, "PASSWORD_CHANGED", module: "Auth");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "password_changed", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["date"] = BusinessClock.Now.ToString("dd/MM/yyyy HH:mm")
            }, cancellationToken);
        }
        catch { /* non-critical */ }
    }

    public async Task ForcedChangePasswordAsync(Guid userId, ForcedChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!user.MustChangePassword)
            throw new InvalidOperationException("No se requiere cambio de contraseña obligatorio.");

        if (user.TemporaryPasswordExpiresAt.HasValue && user.TemporaryPasswordExpiresAt < BusinessClock.Now)
            throw new InvalidOperationException("La contraseña temporal ha expirado. Solicite al administrador una nueva.");

        if (!_passwordService.IsPasswordPolicyCompliant(dto.NewPassword, out var violations))
            throw new InvalidOperationException(string.Join(", ", violations));

        user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
        user.MustChangePassword = false;
        user.LastPasswordChangedAt = BusinessClock.Now;
        user.TemporaryPasswordExpiresAt = null;
        user.UpdatedAt = BusinessClock.Now;
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(userId, "FORCED_PASSWORD_CHANGED", module: "Auth");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "password_changed", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["date"] = BusinessClock.Now.ToString("dd/MM/yyyy HH:mm")
            }, cancellationToken);
        }
        catch { /* non-critical */ }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToUpperInvariant();
        var user = await _uow.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null || user.Status != UserStatus.Active) return;

        // Token criptográficamente seguro: 256 bits desde RNG, URL-safe Base64.
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = BusinessClock.Now.AddHours(24);
        user.UpdatedAt = BusinessClock.Now;
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(user.Id, "PASSWORD_RESET_REQUESTED", module: "Auth");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "password_reset", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["resetLink"] = $"https://repagro.local/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}",
                ["expiryHours"] = "24"
            }, cancellationToken);
        }
        catch { /* non-critical */ }
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToUpperInvariant();
        var user = await _uow.Users.GetByEmailAsync(email, cancellationToken);

        if (user == null || user.PasswordResetToken != dto.Token)
            throw new InvalidOperationException("Token de recuperación inválido o expirado.");

        if (user.PasswordResetTokenExpiresAt < BusinessClock.Now)
            throw new InvalidOperationException("El enlace de recuperación ha expirado. Solicite uno nuevo.");

        if (!_passwordService.IsPasswordPolicyCompliant(dto.NewPassword, out var violations))
            throw new InvalidOperationException(string.Join(", ", violations));

        user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.LastPasswordChangedAt = BusinessClock.Now;
        user.MustChangePassword = false;
        user.UpdatedAt = BusinessClock.Now;
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(user.Id, "PASSWORD_RESET_COMPLETED", module: "Auth");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "password_changed", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["date"] = BusinessClock.Now.ToString("dd/MM/yyyy HH:mm")
            }, cancellationToken);
        }
        catch { /* non-critical */ }
    }
}
