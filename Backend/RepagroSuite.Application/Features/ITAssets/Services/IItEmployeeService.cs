using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public interface IItEmployeeService
{
    Task<PagedResult<ItEmployeeDto>> GetPagedAsync(int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default);
    Task<IEnumerable<ItEmployeeDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ItEmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItEmployeeDto> CreateAsync(CreateItEmployeeDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ItEmployeeDto> UpdateAsync(Guid id, UpdateItEmployeeDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<ItEmployeeHistoryDto> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
