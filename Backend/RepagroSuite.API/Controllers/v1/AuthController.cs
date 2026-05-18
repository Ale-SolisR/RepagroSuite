using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Auth.DTOs;
using RepagroSuite.Application.Features.Auth.Services;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "rp_rt";
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, _currentUser.IpAddress, _currentUser.UserAgent, ct);
        SetRefreshCookie(result.RefreshToken);
        // Vaciamos el refresh del body: el cliente NO debe persistirlo (lo maneja la cookie httpOnly).
        result.RefreshToken = string.Empty;
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Sesión iniciada correctamente."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        [FromBody] RefreshTokenRequestDto? dto, CancellationToken ct)
    {
        // Preferimos la cookie. Aceptamos body sólo como fallback durante la transición.
        var token = Request.Cookies[RefreshCookieName] ?? dto?.RefreshToken ?? string.Empty;
        if (string.IsNullOrEmpty(token))
            throw new UnauthorizedAccessException("Token de refresco no presente.");

        var result = await _authService.RefreshTokenAsync(token, _currentUser.IpAddress, _currentUser.UserAgent, ct);
        SetRefreshCookie(result.RefreshToken);
        result.RefreshToken = string.Empty;
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshTokenRequestDto? dto, CancellationToken ct)
    {
        var token = Request.Cookies[RefreshCookieName] ?? dto?.RefreshToken ?? string.Empty;
        if (!string.IsNullOrEmpty(token))
            await _authService.LogoutAsync(token, ct);
        ClearRefreshCookie();
        return Ok(ApiResponse<object>.Ok(null!, "Sesión cerrada correctamente."));
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,                // HTTPS obligatorio
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",        // sólo se envía a endpoints de auth
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _authService.ChangePasswordAsync(_currentUser.UserId!.Value, dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada correctamente."));
    }

    [HttpPost("forced-change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ForcedChangePassword(
        [FromBody] ForcedChangePasswordDto dto, CancellationToken ct)
    {
        await _authService.ForcedChangePasswordAsync(_currentUser.UserId!.Value, dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada. Ya puede usar el sistema."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Si el correo está registrado, recibirá instrucciones para restablecer su contraseña."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña restablecida correctamente."));
    }
}
