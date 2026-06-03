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

    // ─── Mantenimiento (CRUD) de departamentos ───────────────────────────────────
    [HttpGet("departments")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItDepartmentDto>>>> GetDepartments([FromQuery] string? search, CancellationToken ct)
    {
        var result = await _catalogs.GetDepartmentsAdminAsync(search, ct);
        return Ok(ApiResponse<IEnumerable<ItDepartmentDto>>.Ok(result));
    }

    [HttpPut("departments/{id:guid}")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItDepartmentDto>>> UpdateDepartment(Guid id, [FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.UpdateDepartmentAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItDepartmentDto>.Ok(result, "Departamento actualizado."));
    }

    [HttpPatch("departments/{id:guid}/status")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItDepartmentDto>>> SetDepartmentStatus(Guid id, [FromBody] UpdateCatalogStatusDto dto, CancellationToken ct)
    {
        var result = await _catalogs.SetDepartmentStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItDepartmentDto>.Ok(result, dto.IsActive ? "Departamento activado." : "Departamento inactivado."));
    }

    // ─── Mantenimiento (CRUD) de proveedores ─────────────────────────────────────
    [HttpPost("suppliers")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItCatalogItemDto>>> CreateSupplier([FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.CreateSupplierAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItCatalogItemDto>.Ok(result, "Proveedor creado."));
    }

    [HttpGet("suppliers")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItSupplierDto>>>> GetSuppliers([FromQuery] string? search, CancellationToken ct)
    {
        var result = await _catalogs.GetSuppliersAdminAsync(search, ct);
        return Ok(ApiResponse<IEnumerable<ItSupplierDto>>.Ok(result));
    }

    [HttpPut("suppliers/{id:guid}")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItSupplierDto>>> UpdateSupplier(Guid id, [FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.UpdateSupplierAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItSupplierDto>.Ok(result, "Proveedor actualizado."));
    }

    [HttpPatch("suppliers/{id:guid}/status")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItSupplierDto>>> SetSupplierStatus(Guid id, [FromBody] UpdateCatalogStatusDto dto, CancellationToken ct)
    {
        var result = await _catalogs.SetSupplierStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItSupplierDto>.Ok(result, dto.IsActive ? "Proveedor activado." : "Proveedor inactivado."));
    }

    // ─── Mantenimiento (CRUD) de marcas ──────────────────────────────────────────
    [HttpGet("brands")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItBrandDto>>>> GetBrands([FromQuery] string? search, CancellationToken ct)
    {
        var result = await _catalogs.GetBrandsAdminAsync(search, ct);
        return Ok(ApiResponse<IEnumerable<ItBrandDto>>.Ok(result));
    }

    [HttpPut("brands/{id:guid}")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItBrandDto>>> UpdateBrand(Guid id, [FromBody] CreateCatalogItemDto dto, CancellationToken ct)
    {
        var result = await _catalogs.UpdateBrandAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItBrandDto>.Ok(result, "Marca actualizada."));
    }

    [HttpPatch("brands/{id:guid}/status")]
    [Authorize(Policy = "Ti.Catalog.Manage")]
    public async Task<ActionResult<ApiResponse<ItBrandDto>>> SetBrandStatus(Guid id, [FromBody] UpdateCatalogStatusDto dto, CancellationToken ct)
    {
        var result = await _catalogs.SetBrandStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItBrandDto>.Ok(result, dto.IsActive ? "Marca activada." : "Marca inactivada."));
    }
}
