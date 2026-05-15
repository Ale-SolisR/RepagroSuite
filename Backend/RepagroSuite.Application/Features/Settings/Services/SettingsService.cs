using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.Settings.DTOs;

namespace RepagroSuite.Application.Features.Settings.Services;

// Implementation lives in Infrastructure (needs DbContext).
// This abstract base avoids TestEmail duplication.
public abstract class SettingsServiceBase : ISettingsService
{
    private readonly IEmailService _emailService;

    protected SettingsServiceBase(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public abstract Task<IEnumerable<SystemSettingDto>> GetAllAsync(string? module = null, CancellationToken cancellationToken = default);
    public abstract Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    public abstract Task UpdateAsync(string key, UpdateSettingDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    public abstract Task UpdateBulkAsync(UpdateSettingsBulkDto dto, Guid updatedBy, CancellationToken cancellationToken = default);

    public async Task<bool> TestEmailAsync(TestEmailDto dto, CancellationToken cancellationToken = default)
    {
        // TestAndSendAsync bypasses EMAIL.ENABLED — envía directo sin chequear la bandera de habilitado
        if (!string.IsNullOrWhiteSpace(dto.ToEmail))
            return await _emailService.TestAndSendAsync(dto.ToEmail, cancellationToken);
        return await _emailService.TestConnectionAsync(cancellationToken);
    }
}
