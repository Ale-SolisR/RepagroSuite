using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Settings.DTOs;
using RepagroSuite.Application.Features.Settings.Services;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "Settings.View")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(ISettingsService settingsService, ICurrentUserService currentUser)
    {
        _settingsService = settingsService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemSettingDto>>>> GetAll(
        [FromQuery] string? module = null, CancellationToken ct = default)
    {
        var result = await _settingsService.GetAllAsync(module, ct);
        return Ok(ApiResponse<IEnumerable<SystemSettingDto>>.Ok(result));
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<SystemSettingDto>>> GetByKey(string key, CancellationToken ct)
    {
        var result = await _settingsService.GetByKeyAsync(key, ct);
        if (result == null) return NotFound(ApiResponse<SystemSettingDto>.Fail("Configuración no encontrada.", "NOT_FOUND"));
        return Ok(ApiResponse<SystemSettingDto>.Ok(result));
    }

    [HttpPut("{key}")]
    [Authorize(Policy = "Settings.Update")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        string key, [FromBody] UpdateSettingDto dto, CancellationToken ct)
    {
        await _settingsService.UpdateAsync(key, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Configuración actualizada."));
    }

    [HttpPut("bulk")]
    [Authorize(Policy = "Settings.Update")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateBulk(
        [FromBody] UpdateSettingsBulkDto dto, CancellationToken ct)
    {
        await _settingsService.UpdateBulkAsync(dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Configuraciones actualizadas."));
    }

    [HttpPost("email/test")]
    [Authorize(Policy = "Settings.Email.Test")]
    public async Task<ActionResult<ApiResponse<object>>> TestEmail(CancellationToken ct)
    {
        var dto = new TestEmailDto { ToEmail = _currentUser.Email ?? string.Empty };
        var ok = await _settingsService.TestEmailAsync(dto, ct);
        return ok
            ? Ok(ApiResponse<object>.Ok(null!, "Conexión exitosa. Correo de prueba enviado."))
            : Ok(ApiResponse<object>.Fail("No se pudo conectar al servidor SMTP. Verifique la configuración.", "SMTP_ERROR"));
    }
}
