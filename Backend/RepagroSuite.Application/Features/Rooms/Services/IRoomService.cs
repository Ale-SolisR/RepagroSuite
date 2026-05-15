using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Rooms.DTOs;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Rooms.Services;

public interface IRoomService
{
    Task<RoomDto> CreateAsync(CreateRoomDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task<RoomDto> UpdateAsync(Guid roomId, UpdateRoomDto dto, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<RoomDto> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<PagedResult<RoomDto>> GetPagedAsync(int page, int pageSize, string? search = null, RoomStatus? status = null, CancellationToken cancellationToken = default);
    Task<RoomDto> ChangeStatusAsync(Guid roomId, RoomStatus newStatus, Guid updatedBy, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid roomId, Guid deletedBy, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoomDto>> GetAvailableAsync(DateTime start, DateTime end, int? minCapacity = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilitiesAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task UpsertAvailabilityAsync(Guid roomId, List<UpsertRoomAvailabilityDto> dtos, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<RoomBlockDto> CreateBlockAsync(CreateRoomBlockDto dto, Guid createdBy, CancellationToken cancellationToken = default);
    Task DeleteBlockAsync(Guid blockId, Guid deletedBy, CancellationToken cancellationToken = default);
    Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid roomId, DateTime date, CancellationToken cancellationToken = default);
}
