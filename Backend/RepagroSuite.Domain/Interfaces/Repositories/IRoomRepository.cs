using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Interfaces.Repositories;

public interface IRoomRepository : IGenericRepository<Room>
{
    Task<Room?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Room?> GetWithDetailsAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, Guid? excludeRoomId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime start, DateTime end, int? minCapacity = null, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Room> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null, RoomStatus? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoomAvailability>> GetAvailabilitiesAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task ReplaceAvailabilitiesAsync(Guid roomId, IEnumerable<RoomAvailability> newAvailabilities, CancellationToken cancellationToken = default);
    Task<IEnumerable<Feature>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default);
    Task<RoomBlock?> GetBlockByIdAsync(Guid blockId, CancellationToken cancellationToken = default);
}
