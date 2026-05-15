using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Users.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Users.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, string? search = null, UserStatus? status = null, CancellationToken cancellationToken = default);
    Task<UserDto> ApproveAsync(Guid userId, ApproveUserDto dto, Guid approvedBy, CancellationToken cancellationToken = default);
    Task<UserDto> RejectAsync(Guid userId, RejectUserDto dto, Guid rejectedBy, CancellationToken cancellationToken = default);
    Task<UserDto> BlockAsync(Guid userId, Guid blockedBy, CancellationToken cancellationToken = default);
    Task<UserDto> UnblockAsync(Guid userId, Guid unblockedBy, CancellationToken cancellationToken = default);
    Task<UserDto> InactivateAsync(Guid userId, Guid inactivatedBy, CancellationToken cancellationToken = default);
    Task<UserDto> PromoteToAdminAsync(Guid userId, Guid promotedBy, CancellationToken cancellationToken = default);
    Task<UserDto> DemoteFromAdminAsync(Guid userId, Guid demotedBy, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid userId, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<GenerateTemporaryPasswordResponseDto> GenerateTemporaryPasswordAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default);
    Task ForcePasswordChangeAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default);
}
