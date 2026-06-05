using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Application.Features.ITAssets.Services;

namespace RepagroSuite.API.Controllers.v1;

/// <summary>
/// Bóveda de credenciales por activo de TI. Lectura/revelado con Ti.Inventory.View;
/// alta/edición/baja con Ti.Inventory.Update. Los secretos se guardan cifrados y el revelado se audita.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ti/assets/{assetId:guid}/credentials")]
[Authorize]
public class ItAssetCredentialsController : ControllerBase
{
    private readonly IItAssetCredentialService _service;
    private readonly ICurrentUserService _currentUser;

    public ItAssetCredentialsController(IItAssetCredentialService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItAssetCredentialDto>>>> GetAll(Guid assetId, CancellationToken ct)
    {
        var result = await _service.GetForAssetAsync(assetId, ct);
        return Ok(ApiResponse<IEnumerable<ItAssetCredentialDto>>.Ok(result));
    }

    [HttpGet("{credentialId:guid}/secret")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItAssetCredentialSecretDto>>> Reveal(Guid assetId, Guid credentialId, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await _service.RevealSecretAsync(assetId, credentialId, _currentUser.UserId!.Value, ip, ua, ct);
        return Ok(ApiResponse<ItAssetCredentialSecretDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Ti.Inventory.Update")]
    public async Task<ActionResult<ApiResponse<ItAssetCredentialDto>>> Create(Guid assetId, [FromBody] CreateItAssetCredentialDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(assetId, dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItAssetCredentialDto>.Ok(result, "Credencial guardada."));
    }

    [HttpPut("{credentialId:guid}")]
    [Authorize(Policy = "Ti.Inventory.Update")]
    public async Task<ActionResult<ApiResponse<ItAssetCredentialDto>>> Update(Guid assetId, Guid credentialId, [FromBody] UpdateItAssetCredentialDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(assetId, credentialId, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItAssetCredentialDto>.Ok(result, "Credencial actualizada."));
    }

    [HttpDelete("{credentialId:guid}")]
    [Authorize(Policy = "Ti.Inventory.Update")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid assetId, Guid credentialId, CancellationToken ct)
    {
        await _service.DeleteAsync(assetId, credentialId, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Credencial eliminada."));
    }
}
