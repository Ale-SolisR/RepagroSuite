using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.RastreoUsers.DTOs;

namespace RepagroSuite.Application.Features.RastreoUsers.Services;

/// <summary>
/// Administración de los usuarios del sistema de Rastreo desde Repagro Suite.
/// Toda implementación opera contra <c>RASTREO.Usuarios</c> y registra auditoría con el admin actor.
/// </summary>
public interface IRastreoUserAdminService
{
    Task<PagedResult<RastreoUserDto>> GetPagedAsync(int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default);
    Task<RastreoUserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RastreoUserPasswordResultDto> CreateAsync(CreateRastreoUserDto dto, Guid adminId, CancellationToken cancellationToken = default);
    Task<string> ResetPasswordAsync(int id, ResetRastreoUserPasswordDto dto, Guid adminId, CancellationToken cancellationToken = default);
    Task<RastreoUserDto> ChangeRoleAsync(int id, UpdateRastreoUserRoleDto dto, Guid adminId, CancellationToken cancellationToken = default);
    Task<RastreoUserDto> SetStatusAsync(int id, UpdateRastreoUserStatusDto dto, Guid adminId, CancellationToken cancellationToken = default);
}
