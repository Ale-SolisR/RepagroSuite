using RepagroSuite.Application.Features.Dashboard.DTOs;

namespace RepagroSuite.Application.Features.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
