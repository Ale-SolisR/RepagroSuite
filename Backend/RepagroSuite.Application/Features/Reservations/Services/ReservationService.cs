using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Reservations.Services;

public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;
    private readonly IRealtimeNotifier _realtime;

    public ReservationService(IUnitOfWork uow, IAuditService auditService, IEmailService emailService, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _auditService = auditService;
        _emailService = emailService;
        _realtime = realtime;
    }

    public async Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto, CancellationToken cancellationToken = default)
    {
        // Transacción + lock pesimista por sala para evitar doble booking bajo concurrencia.
        // Sin esto, dos requests simultáneos para el mismo slot pasarían ambos la validación
        // de HasConflictAsync e insertarían reservas duplicadas.
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.AcquireRoomLockAsync(dto.RoomId, timeoutMs: 5000, cancellationToken);
            await ValidateReservationAsync(dto.RoomId, dto.StartDateTime, dto.EndDateTime, dto.PeopleCount, null, cancellationToken);

            var reservation = new Reservation
            {
                RoomId = dto.RoomId,
                UserId = userId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                PeopleCount = dto.PeopleCount,
                Purpose = dto.Purpose.Trim(),
                Notes = dto.Notes?.Trim(),
                Status = ReservationStatus.Pending
            };

            await _uow.Reservations.AddAsync(reservation, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            await _auditService.LogAsync(userId, "RESERVATION_CREATED", entityName: "Reservation", entityId: reservation.Id.ToString(), module: "Reservations");
            await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "created", cancellationToken);
            return await GetByIdAsync(reservation.Id, cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ReservationDto> AdminDirectCreateAsync(Guid adminId, AdminDirectReservationDto dto, CancellationToken cancellationToken = default)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            await _uow.AcquireRoomLockAsync(dto.RoomId, timeoutMs: 5000, cancellationToken);
            await ValidateReservationAsync(dto.RoomId, dto.StartDateTime, dto.EndDateTime, dto.PeopleCount, null, cancellationToken);

            var targetUserId = dto.UserId ?? adminId;
            var reservation = new Reservation
            {
                RoomId = dto.RoomId,
                UserId = targetUserId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                PeopleCount = dto.PeopleCount,
                Purpose = dto.Purpose.Trim(),
                Notes = dto.Notes?.Trim(),
                Status = ReservationStatus.Approved,
                IsDirectAdminReservation = true,
                ApprovedByUserId = adminId,
                ApprovedAt = DateTime.UtcNow
            };

            await _uow.Reservations.AddAsync(reservation, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            await _auditService.LogAsync(adminId, "RESERVATION_DIRECT_CREATED", entityName: "Reservation", entityId: reservation.Id.ToString(), module: "Reservations");
            await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "created", cancellationToken);
            return await GetByIdAsync(reservation.Id, cancellationToken);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ReservationDto> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default)
    {
        var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reserva no encontrada.");
        return MapToDto(reservation);
    }

    public async Task<PagedResult<ReservationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.Reservations.GetPagedAsync(page, pageSize, userId, roomId, status, from, to, cancellationToken);
        return new PagedResult<ReservationDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ReservationDto> ApproveAsync(Guid reservationId, ApproveReservationDto dto, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
                ?? throw new KeyNotFoundException("Reserva no encontrada.");

            if (reservation.Status != ReservationStatus.Pending)
                throw new InvalidOperationException("Solo se pueden aprobar reservas en estado Pendiente.");

            // Lock por sala para evitar que otro admin apruebe una reserva conflictiva al mismo tiempo.
            await _uow.AcquireRoomLockAsync(reservation.RoomId, timeoutMs: 5000, cancellationToken);

            // Re-validate conflict within transaction
            if (await _uow.Reservations.HasConflictAsync(reservation.RoomId, reservation.StartDateTime, reservation.EndDateTime, reservationId, cancellationToken))
                throw new InvalidOperationException("La sala ya fue reservada en ese horario. Existe un conflicto de disponibilidad.");

            reservation.Status = ReservationStatus.Approved;
            reservation.AdminComment = dto.AdminComment?.Trim();
            reservation.ApprovedByUserId = approvedBy;
            reservation.ApprovedAt = DateTime.UtcNow;
            _uow.Reservations.Update(reservation);
            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            await _auditService.LogAsync(approvedBy, "RESERVATION_APPROVED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
            await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "approved", cancellationToken);

            try
            {
                await _emailService.SendTemplateAsync(reservation.User.Email, "reservation_approved", new Dictionary<string, string>
                {
                    ["roomName"] = reservation.Room.Name,
                    ["date"] = reservation.StartDateTime.ToString("dd/MM/yyyy"),
                    ["startTime"] = reservation.StartDateTime.ToString("HH:mm"),
                    ["endTime"] = reservation.EndDateTime.ToString("HH:mm"),
                    ["purpose"] = reservation.Purpose
                }, cancellationToken);
            }
            catch { /* non-critical */ }

            return MapToDto(reservation);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ReservationDto> RejectAsync(Guid reservationId, RejectReservationDto dto, Guid rejectedBy, CancellationToken cancellationToken = default)
    {
        var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reserva no encontrada.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Solo se pueden rechazar reservas en estado Pendiente.");

        reservation.Status = ReservationStatus.Rejected;
        reservation.AdminComment = dto.AdminComment.Trim();
        reservation.RejectedByUserId = rejectedBy;
        reservation.RejectedAt = DateTime.UtcNow;
        _uow.Reservations.Update(reservation);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(rejectedBy, "RESERVATION_REJECTED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "rejected", cancellationToken);

        try
        {
            await _emailService.SendTemplateAsync(reservation.User.Email, "reservation_rejected", new Dictionary<string, string>
            {
                ["roomName"] = reservation.Room.Name,
                ["adminComment"] = dto.AdminComment
            }, cancellationToken);
        }
        catch { /* non-critical */ }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> CancelAsync(Guid reservationId, CancelReservationDto dto, Guid cancelledBy, CancellationToken cancellationToken = default)
    {
        var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reserva no encontrada.");

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Finished)
            throw new InvalidOperationException("La reserva ya fue cancelada o finalizada.");

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancellationReason = dto.Reason.Trim();
        reservation.CancelledByUserId = cancelledBy;
        reservation.CancelledAt = DateTime.UtcNow;
        _uow.Reservations.Update(reservation);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(cancelledBy, "RESERVATION_CANCELLED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "cancelled", cancellationToken);

        return MapToDto(reservation);
    }

    public async Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(DateTime from, DateTime to, Guid? roomId = null, CancellationToken cancellationToken = default)
    {
        var reservations = await _uow.Reservations.GetForCalendarAsync(from, to, roomId, cancellationToken);
        return reservations.Select(r => new CalendarEventDto
        {
            Id = r.Id,
            Title = $"{r.Room?.Name} - {r.User?.FullName}",
            Start = r.StartDateTime,
            End = r.EndDateTime,
            Color = r.Room?.Color,
            RoomName = r.Room?.Name ?? string.Empty,
            UserName = r.User?.FullName ?? string.Empty,
            Status = r.Status.ToString()
        });
    }

    private async Task ValidateReservationAsync(Guid roomId, DateTime start, DateTime end, int peopleCount, Guid? excludeId, CancellationToken ct)
    {
        if (start >= end) throw new InvalidOperationException("La hora de inicio debe ser anterior a la hora de fin.");
        if (start < DateTime.UtcNow) throw new InvalidOperationException("No se pueden crear reservas en el pasado.");

        var room = await _uow.Rooms.GetWithDetailsAsync(roomId, ct)
            ?? throw new KeyNotFoundException("Sala no encontrada.");

        if (room.Status == RoomStatus.Inactive)
            throw new InvalidOperationException("La sala está inactiva y no puede ser reservada.");
        if (room.Status == RoomStatus.Maintenance)
            throw new InvalidOperationException("La sala está en mantenimiento y no puede ser reservada.");
        if (peopleCount > room.Capacity)
            throw new InvalidOperationException($"La sala tiene capacidad para {room.Capacity} personas. Se solicitaron {peopleCount}.");

        // Availability schedule validation
        var dayAvailability = room.Availabilities.FirstOrDefault(a => a.DayOfWeek == start.DayOfWeek);
        if (dayAvailability == null || !dayAvailability.IsAvailable)
            throw new InvalidOperationException("La sala no está disponible el día seleccionado.");

        var localStart = TimeOnly.FromTimeSpan(start.TimeOfDay);
        var localEnd = TimeOnly.FromTimeSpan(end.TimeOfDay);

        if (localStart < dayAvailability.OpenTime)
            throw new InvalidOperationException($"La sala abre a las {dayAvailability.OpenTime:HH:mm}.");
        if (localEnd > dayAvailability.CloseTime)
            throw new InvalidOperationException($"La sala cierra a las {dayAvailability.CloseTime:HH:mm}.");

        var durationMinutes = (int)(end - start).TotalMinutes;
        if (durationMinutes < dayAvailability.MinReservationMinutes)
            throw new InvalidOperationException($"La duración mínima es de {dayAvailability.MinReservationMinutes} minutos.");
        if (durationMinutes > dayAvailability.MaxReservationMinutes)
            throw new InvalidOperationException($"La duración máxima es de {dayAvailability.MaxReservationMinutes} minutos.");

        // Block validation
        foreach (var block in room.Blocks.Where(b => b.IsActive))
        {
            if (block.IsRecurring && block.RecurringDayOfWeek == start.DayOfWeek && block.StartTime.HasValue && block.EndTime.HasValue)
            {
                if (localStart < block.EndTime && localEnd > block.StartTime)
                    throw new InvalidOperationException($"La reserva cruza un bloque no disponible: {block.Reason ?? block.BlockType.ToString()}.");
            }
            else if (!block.IsRecurring && block.SpecificStartDateTime.HasValue && block.SpecificEndDateTime.HasValue)
            {
                if (start < block.SpecificEndDateTime && end > block.SpecificStartDateTime)
                    throw new InvalidOperationException($"La reserva cruza un bloque no disponible: {block.Reason ?? block.BlockType.ToString()}.");
            }
        }

        // Conflict check
        if (await _uow.Reservations.HasConflictAsync(roomId, start, end, excludeId, ct))
            throw new InvalidOperationException("La sala ya está reservada en ese horario. Seleccione otro horario o sala.");
    }

    private static ReservationDto MapToDto(Reservation r) => new()
    {
        Id = r.Id,
        RoomId = r.RoomId,
        RoomName = r.Room?.Name ?? string.Empty,
        RoomCode = r.Room?.Code ?? string.Empty,
        RoomColor = r.Room?.Color,
        UserId = r.UserId,
        UserFullName = r.User?.FullName ?? string.Empty,
        UserEmail = r.User?.Email ?? string.Empty,
        StartDateTime = r.StartDateTime,
        EndDateTime = r.EndDateTime,
        PeopleCount = r.PeopleCount,
        Purpose = r.Purpose,
        Notes = r.Notes,
        Status = r.Status,
        StatusName = r.Status switch
        {
            ReservationStatus.Pending => "Pendiente",
            ReservationStatus.Approved => "Aprobada",
            ReservationStatus.Rejected => "Rechazada",
            ReservationStatus.Cancelled => "Cancelada",
            ReservationStatus.Finished => "Finalizada",
            _ => r.Status.ToString()
        },
        AdminComment = r.AdminComment,
        ApprovedByUserId = r.ApprovedByUserId,
        ApprovedAt = r.ApprovedAt,
        CancellationReason = r.CancellationReason,
        CancelledAt = r.CancelledAt,
        IsDirectAdminReservation = r.IsDirectAdminReservation,
        CreatedAt = r.CreatedAt
    };
}
