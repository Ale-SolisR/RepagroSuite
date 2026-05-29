using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Rooms.DTOs;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Rooms.Services;

public class RoomService : IRoomService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly IRealtimeNotifier _realtime;
    private readonly IAppCache _cache;

    public RoomService(IUnitOfWork uow, IAuditService auditService, IRealtimeNotifier realtime, IAppCache cache)
    {
        _uow = uow;
        _auditService = auditService;
        _realtime = realtime;
        _cache = cache;
    }

    public async Task<RoomDto> CreateAsync(CreateRoomDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _uow.Rooms.CodeExistsAsync(code, cancellationToken: cancellationToken))
            throw new InvalidOperationException("Ya existe una sala con ese código.");

        var room = new Room
        {
            Name = dto.Name.Trim(),
            Code = code,
            Capacity = dto.Capacity,
            Location = dto.Location?.Trim(),
            Floor = dto.Floor?.Trim(),
            Description = dto.Description?.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            Color = dto.Color?.Trim(),
            Status = RoomStatus.Available,
            CreatedBy = createdBy
        };

        foreach (var featureId in dto.FeatureIds.Distinct())
            room.RoomFeatures.Add(new RoomFeature { RoomId = room.Id, FeatureId = featureId });

        foreach (var avail in dto.Availabilities)
        {
            room.Availabilities.Add(new RoomAvailability
            {
                RoomId = room.Id,
                DayOfWeek = avail.DayOfWeek,
                IsAvailable = avail.IsAvailable,
                OpenTime = TimeOnly.Parse(avail.OpenTime),
                CloseTime = TimeOnly.Parse(avail.CloseTime),
                MinReservationMinutes = avail.MinReservationMinutes,
                MaxReservationMinutes = avail.MaxReservationMinutes,
                SlotIntervalMinutes = avail.SlotIntervalMinutes
            });
        }

        await _uow.Rooms.AddAsync(room, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(createdBy, "ROOM_CREATED", entityName: "Room", entityId: room.Id.ToString(), module: "Rooms");
        await _realtime.RoomChangedAsync(room.Id, "created", cancellationToken);
        return await GetByIdAsync(room.Id, cancellationToken);
    }

    public async Task<RoomDto> UpdateAsync(Guid roomId, UpdateRoomDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetWithDetailsAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        // Optimistic concurrency: si el cliente envió un RowVersion, lo seteamos como "original"
        // para que SaveChanges lance DbUpdateConcurrencyException si otro admin cambió la sala mientras tanto.
        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            try
            {
                var clientVersion = Convert.FromBase64String(dto.RowVersion);
                _uow.Rooms.SetOriginalRowVersion(room, clientVersion);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Token de versión inválido. Recargue la sala e intente nuevamente.");
            }
        }

        room.Name = dto.Name.Trim();
        room.Capacity = dto.Capacity;
        room.Location = dto.Location?.Trim();
        room.Floor = dto.Floor?.Trim();
        room.Description = dto.Description?.Trim();
        room.ImageUrl = dto.ImageUrl?.Trim();
        room.Color = dto.Color?.Trim();
        room.UpdatedBy = updatedBy;

        // Update features
        room.RoomFeatures.Clear();
        foreach (var featureId in dto.FeatureIds.Distinct())
            room.RoomFeatures.Add(new RoomFeature { RoomId = roomId, FeatureId = featureId });

        // Update availabilities: solo reemplaza si el DTO realmente las trae.
        // El form de edición no las gestiona (van por UpsertAvailability), así que un
        // PUT sin Availabilities NO debe borrar las existentes.
        if (dto.Availabilities is not null)
        {
            room.Availabilities.Clear();
            foreach (var avail in dto.Availabilities)
            {
                room.Availabilities.Add(new RoomAvailability
                {
                    RoomId = roomId,
                    DayOfWeek = avail.DayOfWeek,
                    IsAvailable = avail.IsAvailable,
                    OpenTime = TimeOnly.Parse(avail.OpenTime),
                    CloseTime = TimeOnly.Parse(avail.CloseTime),
                    MinReservationMinutes = avail.MinReservationMinutes,
                    MaxReservationMinutes = avail.MaxReservationMinutes,
                    SlotIntervalMinutes = avail.SlotIntervalMinutes
                });
            }
        }

        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(updatedBy, "ROOM_UPDATED", entityName: "Room", entityId: roomId.ToString(), module: "Rooms");
        await _realtime.RoomChangedAsync(roomId, "updated", cancellationToken);
        return await GetByIdAsync(roomId, cancellationToken);
    }

    public async Task<RoomDto> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetWithDetailsAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");
        return MapToDto(room);
    }

    public async Task<PagedResult<RoomDto>> GetPagedAsync(int page, int pageSize, string? search = null, RoomStatus? status = null, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.Rooms.GetPagedAsync(page, pageSize, search, status, cancellationToken);
        return new PagedResult<RoomDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<RoomDto> ChangeStatusAsync(Guid roomId, RoomStatus newStatus, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        room.Status = newStatus;
        room.UpdatedBy = updatedBy;
        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(updatedBy, $"ROOM_STATUS_CHANGED_{newStatus.ToString().ToUpper()}", entityName: "Room", entityId: roomId.ToString(), module: "Rooms");
        await _realtime.RoomChangedAsync(roomId, "status_changed", cancellationToken);
        return MapToDto(room);
    }

    public async Task DeleteAsync(Guid roomId, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        _uow.Rooms.SoftDelete(room, deletedBy);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(deletedBy, "ROOM_DELETED", entityName: "Room", entityId: roomId.ToString(), module: "Rooms");
        await _realtime.RoomChangedAsync(roomId, "deleted", cancellationToken);
    }

    public async Task<IEnumerable<RoomDto>> GetAvailableAsync(DateTime start, DateTime end, int? minCapacity = null, CancellationToken cancellationToken = default)
    {
        var rooms = await _uow.Rooms.GetAvailableRoomsAsync(start, end, minCapacity, cancellationToken);
        return rooms.Select(MapToDto);
    }

    public async Task<IEnumerable<RoomAvailabilityDto>> GetAvailabilitiesAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        _ = await _uow.Rooms.GetByIdAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        var availabilities = await _uow.Rooms.GetAvailabilitiesAsync(roomId, cancellationToken);
        return availabilities.Select(a => new RoomAvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = (int)a.DayOfWeek,
            DayName = a.DayOfWeek.ToString(),
            IsAvailable = a.IsAvailable,
            OpenTime = a.OpenTime.ToString("HH:mm"),
            CloseTime = a.CloseTime.ToString("HH:mm"),
            MinReservationMinutes = a.MinReservationMinutes,
            MaxReservationMinutes = a.MaxReservationMinutes,
            SlotIntervalMinutes = a.SlotIntervalMinutes,
        });
    }

    public async Task UpsertAvailabilityAsync(Guid roomId, List<UpsertRoomAvailabilityDto> dtos, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        _ = await _uow.Rooms.GetByIdAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        var newAvailabilities = dtos.Select(dto => new RoomAvailability
        {
            RoomId = roomId,
            DayOfWeek = dto.DayOfWeek,
            IsAvailable = dto.IsAvailable,
            OpenTime = TimeOnly.Parse(dto.OpenTime),
            CloseTime = TimeOnly.Parse(dto.CloseTime),
            MinReservationMinutes = dto.MinReservationMinutes,
            MaxReservationMinutes = dto.MaxReservationMinutes,
            SlotIntervalMinutes = dto.SlotIntervalMinutes
        }).ToList();

        await _uow.Rooms.ReplaceAvailabilitiesAsync(roomId, newAvailabilities, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(updatedBy, "ROOM_AVAILABILITY_UPDATED", entityName: "Room", entityId: roomId.ToString(), module: "Rooms");
    }

    public async Task<RoomBlockDto> CreateBlockAsync(CreateRoomBlockDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(dto.RoomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        var block = new RoomBlock
        {
            RoomId = dto.RoomId,
            BlockType = dto.BlockType,
            Reason = dto.Reason?.Trim(),
            IsRecurring = dto.IsRecurring,
            RecurringDayOfWeek = dto.RecurringDayOfWeek,
            StartTime = dto.StartTime != null ? TimeOnly.Parse(dto.StartTime) : null,
            EndTime = dto.EndTime != null ? TimeOnly.Parse(dto.EndTime) : null,
            SpecificDate = dto.SpecificDate,
            SpecificStartDateTime = dto.SpecificStartDateTime,
            SpecificEndDateTime = dto.SpecificEndDateTime,
            IsActive = true,
            CreatedBy = createdBy
        };

        room.Blocks.Add(block);
        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(createdBy, "ROOM_BLOCK_CREATED", entityName: "RoomBlock", entityId: block.Id.ToString(), module: "Rooms");
        return MapBlockToDto(block);
    }

    public async Task DeleteBlockAsync(Guid blockId, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var block = await _uow.Rooms.GetBlockByIdAsync(blockId, cancellationToken)
            ?? throw new KeyNotFoundException("Bloqueo no encontrado.");

        block.IsActive = false;
        block.IsDeleted = true;
        block.DeletedAt = BusinessClock.Now;
        block.DeletedBy = deletedBy;
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(deletedBy, "ROOM_BLOCK_DELETED", entityName: "RoomBlock", entityId: blockId.ToString(), module: "Rooms");
    }

    public async Task<IEnumerable<AvailableSlotDto>> GetAvailableSlotsAsync(Guid roomId, DateTime date, CancellationToken cancellationToken = default)
    {
        var room = await _uow.Rooms.GetWithDetailsAsync(roomId, cancellationToken)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        var dayAvail = room.Availabilities.FirstOrDefault(a => a.DayOfWeek == date.DayOfWeek);
        if (dayAvail == null || !dayAvail.IsAvailable) return [];

        var slots = new List<AvailableSlotDto>();
        var current = date.Date.Add(dayAvail.OpenTime.ToTimeSpan());
        var end = date.Date.Add(dayAvail.CloseTime.ToTimeSpan());
        var interval = TimeSpan.FromMinutes(dayAvail.SlotIntervalMinutes);
        var minDuration = TimeSpan.FromMinutes(dayAvail.MinReservationMinutes);

        var existingReservations = await _uow.Reservations.GetByRoomAsync(roomId, date.Date, date.Date.AddDays(1), cancellationToken);

        while (current + minDuration <= end)
        {
            var slotEnd = current + minDuration;
            var isAvailable = !existingReservations.Any(r =>
                (r.Status == ReservationStatus.Approved || r.Status == ReservationStatus.Pending) &&
                r.StartDateTime < slotEnd && r.EndDateTime > current);

            // Check blocks
            if (isAvailable)
            {
                var currentTime = TimeOnly.FromTimeSpan(current.TimeOfDay);
                isAvailable = !room.Blocks.Any(b =>
                    b.IsActive && b.IsRecurring && b.RecurringDayOfWeek == date.DayOfWeek &&
                    b.StartTime.HasValue && b.EndTime.HasValue &&
                    currentTime < b.EndTime && TimeOnly.FromTimeSpan(slotEnd.TimeOfDay) > b.StartTime);
            }

            slots.Add(new AvailableSlotDto { Start = current, End = slotEnd, IsAvailable = isAvailable });
            current += interval;
        }

        return slots;
    }

    public async Task<IEnumerable<RoomScheduleDto>> GetWeeklySchedulesAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _uow.Rooms.GetAvailableRoomsWithSchedulesAsync(cancellationToken);
        return rooms.Select(r => new RoomScheduleDto
        {
            RoomId = r.Id,
            RoomName = r.Name,
            Color = r.Color,
            Days = r.Availabilities
                .Where(a => a.IsAvailable)
                .OrderBy(a => a.DayOfWeek)
                .Select(a => new DayWindowDto
                {
                    DayOfWeek = (int)a.DayOfWeek,
                    OpenTime = a.OpenTime.ToString("HH:mm"),
                    CloseTime = a.CloseTime.ToString("HH:mm"),
                })
                .ToList()
        }).ToList();
    }

    public async Task<IEnumerable<FeatureDto>> GetActiveFeaturesAsync(CancellationToken cancellationToken = default)
    {
        // Features cambian raras veces: cache 1 hora. Si en el futuro hay un endpoint
        // para administrarlas, llamar a _cache.Remove(CacheKeys.ActiveFeatures) tras update.
        return await _cache.GetOrCreateAsync(CacheKeys.ActiveFeatures, TimeSpan.FromHours(1), async ct =>
        {
            var features = await _uow.Rooms.GetActiveFeaturesAsync(ct);
            return features.Select(f => new FeatureDto
            {
                Id = f.Id,
                Name = f.Name,
                IconName = f.IconName,
            }).ToList();
        }, cancellationToken);
    }

    private static RoomDto MapToDto(Room r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Code = r.Code,
        Capacity = r.Capacity,
        Location = r.Location,
        Floor = r.Floor,
        Description = r.Description,
        Status = r.Status,
        StatusName = r.Status switch
        {
            RoomStatus.Available => "Disponible",
            RoomStatus.Occupied => "Ocupada",
            RoomStatus.Maintenance => "En mantenimiento",
            RoomStatus.Inactive => "Inactiva",
            _ => r.Status.ToString()
        },
        ImageUrl = r.ImageUrl,
        Color = r.Color,
        RowVersion = r.RowVersion is { Length: > 0 } ? Convert.ToBase64String(r.RowVersion) : null,
        Features = r.RoomFeatures
            .Where(rf => rf.Feature?.IsActive == true)
            .Select(rf => new FeatureDto { Id = rf.Feature.Id, Name = rf.Feature.Name, IconName = rf.Feature.IconName }),
        Availabilities = r.Availabilities.Select(a => new RoomAvailabilityDto
        {
            Id = a.Id,
            DayOfWeek = (int)a.DayOfWeek,
            DayName = a.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => a.DayOfWeek.ToString()
            },
            IsAvailable = a.IsAvailable,
            OpenTime = a.OpenTime.ToString("HH:mm"),
            CloseTime = a.CloseTime.ToString("HH:mm"),
            MinReservationMinutes = a.MinReservationMinutes,
            MaxReservationMinutes = a.MaxReservationMinutes,
            SlotIntervalMinutes = a.SlotIntervalMinutes
        }),
        CreatedAt = r.CreatedAt
    };

    private static RoomBlockDto MapBlockToDto(RoomBlock b) => new()
    {
        Id = b.Id,
        RoomId = b.RoomId,
        BlockType = b.BlockType,
        BlockTypeName = b.BlockType switch
        {
            BlockType.Lunch => "Almuerzo",
            BlockType.Maintenance => "Mantenimiento",
            BlockType.Cleaning => "Limpieza",
            BlockType.InternalUse => "Uso interno",
            BlockType.Holiday => "Feriado",
            BlockType.Custom => "Personalizado",
            _ => b.BlockType.ToString()
        },
        Reason = b.Reason,
        IsRecurring = b.IsRecurring,
        RecurringDayOfWeek = b.RecurringDayOfWeek,
        StartTime = b.StartTime?.ToString("HH:mm"),
        EndTime = b.EndTime?.ToString("HH:mm"),
        SpecificDate = b.SpecificDate,
        SpecificStartDateTime = b.SpecificStartDateTime,
        SpecificEndDateTime = b.SpecificEndDateTime
    };
}
