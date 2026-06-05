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
[Route("api/v{version:apiVersion}/ti/employees")]
[Authorize]
public class ItEmployeesController : ControllerBase
{
    private readonly IItEmployeeService _employees;
    private readonly ICurrentUserService _currentUser;

    public ItEmployeesController(IItEmployeeService employees, ICurrentUserService currentUser)
    {
        _employees = employees;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<PagedResult<ItEmployeeDto>>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? activeOnly = null, CancellationToken ct = default)
    {
        var result = await _employees.GetPagedAsync(page, pageSize, search, activeOnly, ct);
        return Ok(ApiResponse<PagedResult<ItEmployeeDto>>.Ok(result));
    }

    // Lista liviana para selects (responsable / colaborador).
    [HttpGet("active")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ItEmployeeDto>>>> GetActive(CancellationToken ct)
    {
        var result = await _employees.GetActiveAsync(ct);
        return Ok(ApiResponse<IEnumerable<ItEmployeeDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItEmployeeDto>>> GetById(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ItEmployeeDto>.Ok(await _employees.GetByIdAsync(id, ct)));

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = "Ti.Inventory.View")]
    public async Task<ActionResult<ApiResponse<ItEmployeeHistoryDto>>> GetHistory(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ItEmployeeHistoryDto>.Ok(await _employees.GetHistoryAsync(id, ct)));

    [HttpPost]
    [Authorize(Policy = "Ti.Employee.Manage")]
    public async Task<ActionResult<ApiResponse<ItEmployeeDto>>> Create([FromBody] CreateItEmployeeDto dto, CancellationToken ct)
    {
        var result = await _employees.CreateAsync(dto, _currentUser.UserId!.Value, ct);
        return Created(string.Empty, ApiResponse<ItEmployeeDto>.Ok(result, "Colaborador creado."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Ti.Employee.Manage")]
    public async Task<ActionResult<ApiResponse<ItEmployeeDto>>> Update(Guid id, [FromBody] UpdateItEmployeeDto dto, CancellationToken ct)
    {
        var result = await _employees.UpdateAsync(id, dto, _currentUser.UserId!.Value, ct);
        return Ok(ApiResponse<ItEmployeeDto>.Ok(result, "Colaborador actualizado."));
    }
}
