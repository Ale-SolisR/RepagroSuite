using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Application.Features.ITAssets.Services;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ti/catalogs")]
[Authorize]
public class ItCatalogsController : ControllerBase
{
    private readonly IItCatalogService _catalogs;
    private readonly ICurrentUserService _currentUser;

    public ItCatalogsController(IItCatalogService catalogs, ICurrentUserService currentUser)
    {
        _catalogs = catalogs;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItCatalogsDto>>> GetAll(CancellationToken ct)
    {
        var result = await _catalogs.GetAllAsync(ct);
        return Ok(ApiResponse<ItCatalogsDto>.Ok(result));
    }

    [HttpPost("brands")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItCatalogItemDto>>> CreateBrand([FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.CreateBrandAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItCatalogItemDto>.Ok(result, "Marca creada."));
    }

    [HttpPost("locations")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItCatalogItemDto>>> CreateLocation([FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.CreateLocationAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItCatalogItemDto>.Ok(result, "Ubicación creada."));
    }

    [HttpPost("departments")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItCatalogItemDto>>> CreateDepartment([FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.CreateDepartmentAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItCatalogItemDto>.Ok(result, "Departamento creado."));
    }
}
