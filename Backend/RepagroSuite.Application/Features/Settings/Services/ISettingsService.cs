using RepagroSuite.Application.Features.Settings.DTOs;

namespace RepagroSuite.Application.Features.Settings.Services;

public interface ISettingsService
{
    Task<IEnumerable<SystemSettingDto>> GetAllAsync(string? module = null, CancellationToken cancellationToken = default);
    Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task UpdateAsync(string key, UpdateSettingDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task UpdateBulkAsync(UpdateSettingsBulkDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<bool> TestEmailAsync(TestEmailDto dto, CancellationToken cancellationToken = default);
}
