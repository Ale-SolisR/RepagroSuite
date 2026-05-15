using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(dto, _currentUser.IpAddress, _currentUser.UserAgent, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Sesión iniciada correctamente."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(
        [FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken, _currentUser.IpAddress, _currentUser.UserAgent, ct);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshTokenRequestDto dto, CancellationToken ct)
    {
        await _authService.LogoutAsync(dto.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Sesión cerrada correctamente."));
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
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Si el correo está registrado, recibirá instrucciones para restablecer su contraseña."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña restablecida correctamente."));
    }
}
