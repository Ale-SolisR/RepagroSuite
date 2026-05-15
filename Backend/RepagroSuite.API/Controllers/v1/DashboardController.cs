using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Dashboard.DTOs;
using RepagroSuite.Application.Features.Dashboard.Services;

namespace RepagroSuite.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats(CancellationToken ct)
    {
        var result = await _dashboardService.GetStatsAsync(ct);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(result));
    }
}
