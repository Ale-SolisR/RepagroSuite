using Microsoft.EntityFrameworkCore;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.Settings.DTOs;
using RepagroSuite.Application.Features.Settings.Services;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

public class SettingsService : SettingsServiceBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public SettingsService(ApplicationDbContext context, IEmailService emailService, IAuditService auditService)
        : base(emailService)
    {
        _context = context;
        _auditService = auditService;
    }

    public override async Task<IEnumerable<SystemSettingDto>> GetAllAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SystemSettings.Where(s => !s.IsDeleted);
        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(s => s.Module == module);

        var settings = await query.OrderBy(s => s.Module).ThenBy(s => s.Key).ToListAsync(cancellationToken);
        return settings.Select(MapToDto);
    }

    public override async Task<SystemSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);
        return setting == null ? null : MapToDto(setting);
    }

    public override async Task UpdateAsync(string key, UpdateSettingDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Configuración '{key}' no encontrada.");

        if (setting.IsReadOnly)
            throw new InvalidOperationException("Esta configuración es de solo lectura.");

        var oldValue = setting.Value;
        setting.Value = dto.Value?.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = updatedBy;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(updatedBy, "SETTING_UPDATED", entityName: "SystemSetting", entityId: key,
            oldValues: new { Value = oldValue }, newValues: new { Value = setting.Value }, module: "Settings");
    }

    public override async Task UpdateBulkAsync(UpdateSettingsBulkDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in dto.Settings)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);
            if (setting == null || setting.IsReadOnly) continue;

            setting.Value = value?.Trim();
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = updatedBy;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(updatedBy, "SETTINGS_BULK_UPDATED", module: "Settings");
    }

    private static SystemSettingDto MapToDto(SystemSetting s) => new()
    {
        Id = s.Id,
        Key = s.Key,
        Value = s.IsEncrypted ? null : s.Value,
        DefaultValue = s.DefaultValue,
        Description = s.Description,
        Module = s.Module,
        DataType = s.DataType,
        IsReadOnly = s.IsReadOnly,
        IsEncrypted = s.IsEncrypted
    };
}
