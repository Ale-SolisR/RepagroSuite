using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Application.Features.ITAssets.Services;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ti/assets")]
[Authorize]
public class ItAssetsController : ControllerBase
{
    private readonly IItAssetService _assets;
    private readonly ICurrentUserService _currentUser;

    public ItAssetsController(IItAssetService assets, ICurrentUserService currentUser)
    {
        _assets = assets;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<ItAssetListDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] ItAssetStatus? status = null,
        [FromQuery] Guid? assetTypeId = null, [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? holderId = null,
        CancellationToken ct = default)
    {
        var result = await _assets.GetPagedAsync(page, pageSize, search, status, assetTypeId, departmentId, holderId, ct);
        return Ok(ApiResponse<PagedResult<ItAssetListDto>>.Ok(result));
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "Ti.Dashboard.View")]
    public async Task<ActionResult<ApiResponse<ItDashboardDto>>> GetDashboard(CancellationToken ct)
    {
        var result = await _assets.GetDashboardAsync(ct);
        return Ok(ApiResponse<ItDashboardDto>.Ok(result));
    }

    [HttpGet("export/excel")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<IActionResult> ExportExcel(CancellationToken ct)
    {
        var bytes = await _assets.ExportInventoryExcelAsync(ct);
        var name = $"Inventario_TI_REPAGRO_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet("export/pdf")]
    [Authorize(Policy = "Ti.Dashboard.View")]
    public async Task<IActionResult> ExportDashboardPdf(CancellationToken ct)
    {
        var bytes = await _assets.GenerateDashboardPdfAsync(ct);
        var name = $"Dashboard_TI_REPAGRO_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(bytes, "application/pdf", name);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItAssetDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _assets.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ItAssetDto>.Ok(result));
    }

    [HttpGet("{id:guid}/photos/{photoId:guid}/download")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<IActionResult> DownloadPhoto(Guid id, Guid photoId, CancellationToken ct)
    {
        var file = await _assets.GetPhotoFileAsync(id, photoId, ct);
        if (file is null) return NotFound();
        return File(file.Content, file.MimeType, file.FileName);
    }

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItAssetHistoryDto>>>> GetHistory(Guid id, CancellationToken ct)
    {
        var result = await _assets.GetHistoryAsync(id, ct);
        return Ok(ApiResponse<IEnumerable<ItAssetHistoryDto>>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Ti.Inventory.Create")]
    public async Task<ActionResult<ApiResponse<ItAssetDto>>> Create([FromBody] CreateItAssetDto dto, CancellationToken ct)
    {
        var result = await _assets.CreateAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItAssetDto>.Ok(result, "Activo registrado correctamente."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Ti.Inventory.Update")]
    public async Task<ActionResult<ApiResponse<ItAssetDto>>> Update(Guid id, [FromBody] UpdateItAssetDto dto, CancellationToken ct)
    {
        var result = await _assets.UpdateAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItAssetDto>.Ok(result, "Activo actualizado correctamente."));
    }

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = "Ti.Asset.Reactivate")]
    public async Task<ActionResult<ApiResponse<ItAssetDto>>> Reactivate(Guid id, [FromBody] ReactivateAssetDto dto, CancellationToken ct)
    {
        var result = await _assets.ReactivateAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItAssetDto>.Ok(result, "Activo reactivado y disponible."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Ti.Inventory.Delete")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await _assets.DeleteAsync(id, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Activo eliminado."));
    }
}
