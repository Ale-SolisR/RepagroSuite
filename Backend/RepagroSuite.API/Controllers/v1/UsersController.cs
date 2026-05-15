using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Users.DTOs;
using RepagroSuite.Application.Features.Users.Services;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register(
        [FromBody] RegisterUserDto dto, CancellationToken ct)
    {
        var result = await _userService.RegisterAsync(dto, ct);
        return Created(string.Empty, ApiResponse<UserDto>.Ok(result, "Solicitud de registro enviada. Un administrador revisará su solicitud."));
    }

    [HttpGet]
    [Authorize(Policy = "Users.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] UserStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _userService.GetPagedAsync(page, pageSize, search, status, ct);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Users.View")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(_currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        if (id != _currentUser.UserId && !_currentUser.HasPermission("Users.View"))
            return Forbid();
        var result = await _userService.UpdateAsync(id, dto, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Usuario actualizado correctamente."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Users.Approve")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Approve(
        Guid id, [FromBody] ApproveUserDto dto, CancellationToken ct)
    {
        var result = await _userService.ApproveAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Usuario aprobado correctamente."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Users.Reject")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Reject(
        Guid id, [FromBody] RejectUserDto dto, CancellationToken ct)
    {
        var result = await _userService.RejectAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Usuario rechazado."));
    }

    [HttpPost("{id:guid}/block")]
    [Authorize(Policy = "Users.Block")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Block(Guid id, CancellationToken ct)
    {
        var result = await _userService.BlockAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Usuario bloqueado."));
    }

    [HttpPost("{id:guid}/unblock")]
    [Authorize(Policy = "Users.Block")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Unblock(Guid id, CancellationToken ct)
    {
        var result = await _userService.UnblockAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<UserDto>.Ok(result, "Usuario desbloqueado."));
    }

    [HttpPost("{id:guid}/generate-temporary-password")]
    [Authorize(Policy = "Users.GenerateTemporaryPassword")]
    public async Task<ActionResult<ApiResponse<GenerateTemporaryPasswordResponseDto>>> GenerateTemporaryPassword(
        Guid id, CancellationToken ct)
    {
        var result = await _userService.GenerateTemporaryPasswordAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<GenerateTemporaryPasswordResponseDto>.Ok(result));
    }

    [HttpPost("{id:guid}/force-password-change")]
    [Authorize(Policy = "Users.ForcePasswordChange")]
    public async Task<ActionResult<ApiResponse<object>>> ForcePasswordChange(Guid id, CancellationToken ct)
    {
        await _userService.ForcePasswordChangeAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Se ha forzado el cambio de contraseña en el próximo inicio de sesión."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMINISTRATOR")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _userService.DeleteAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Usuario eliminado."));
    }
}
