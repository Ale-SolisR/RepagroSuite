using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.RastreoUsers.DTOs;
using RepagroSuite.Application.Features.RastreoUsers.Services;

namespace RepagroSuite.API.Controllers.v1;

/// <summary>
/// Administración de los usuarios del SISTEMA DE RASTREO desde Repagro Suite.
/// Estos usuarios viven en el esquema RASTREO y son independientes de los usuarios de Repagro
/// (CORE.Usuarios): crear/editar aquí NO otorga ningún acceso a Repagro Suite. Requiere permisos
/// dedicados RastreoUsers.* que un administrador de Repagro no posee por defecto.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/rastreo/users")]
[Authorize]
public class RastreoUsersController : ControllerBase
{
    private readonly IRastreoUserAdminService _service;
    private readonly ICurrentUserService _currentUser;

    public RastreoUsersController(IRastreoUserAdminService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "RastreoUsers.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<RastreoUserDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? activeOnly = null, CancellationToken ct = default)
    {
        var result = await _service.GetPagedAsync(page, pageSize, search, activeOnly, ct);
        return Ok(ApiResponse<PagedResult<RastreoUserDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "RastreoUsers.View")]
    public async Task<ActionResult<ApiResponse<RastreoUserDto>>> GetById(int id, CancellationToken ct)
        => Ok(ApiResponse<RastreoUserDto>.Ok(await _service.GetByIdAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = "RastreoUsers.Create")]
    public async Task<ActionResult<ApiResponse<RastreoUserDto>>> Create([FromBody] CreateRastreoUserDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<RastreoUserDto>.Ok(result, "Usuario de rastreo creado. Comparte la contraseña por un canal seguro."));
    }

    [HttpPost("{id:int}/reset-password")]
    [Authorize(Policy = "RastreoUsers.ResetPassword")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetRastreoUserPasswordDto dto, CancellationToken ct)
    {
        await _service.ResetPasswordAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada correctamente."));
    }

    [HttpPatch("{id:int}/role")]
    [Authorize(Policy = "RastreoUsers.ManageRole")]
    public async Task<ActionResult<ApiResponse<RastreoUserDto>>> ChangeRole(int id, [FromBody] UpdateRastreoUserRoleDto dto, CancellationToken ct)
    {
        var result = await _service.ChangeRoleAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<RastreoUserDto>.Ok(result, "Rol actualizado correctamente."));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "RastreoUsers.ManageStatus")]
    public async Task<ActionResult<ApiResponse<RastreoUserDto>>> SetStatus(int id, [FromBody] UpdateRastreoUserStatusDto dto, CancellationToken ct)
    {
        var result = await _service.SetStatusAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<RastreoUserDto>.Ok(result, dto.Activo ? "Usuario activado." : "Usuario desactivado."));
    }
}
