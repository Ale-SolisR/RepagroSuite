using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Reservations.Services;

public class ReservationService : IReservationService
{
    // Las horas se manejan como hora-de-pared de Costa Rica (UTC-6, sin DST) en todo el sistema.
    // Comparar contra DateTime.UtcNow rechazaría horas válidas de hoy en un servidor UTC.
    private static readonly TimeZoneInfo BusinessTimeZone = ResolveBusinessTimeZone();

    private static TimeZoneInfo ResolveBusinessTimeZone()
    {
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* probar siguiente id */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("CR", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
    }

    private static DateTime BusinessNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessTimeZone);

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
        var reservationId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            await _uow.AcquireRoomLockAsync(dto.RoomId, timeoutMs: 5000, ct);
            await ValidateReservationAsync(dto.RoomId, dto.StartDateTime, dto.EndDateTime, dto.PeopleCount, null, ct);

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

            await _uow.Reservations.AddAsync(reservation, ct);
            await _uow.SaveChangesAsync(ct);
            return reservation.Id;
        }, cancellationToken);

        await _auditService.LogAsync(userId, "RESERVATION_CREATED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservationId, dto.RoomId, "created", cancellationToken);
        return await GetByIdAsync(reservationId, cancellationToken);
    }

    public async Task<RecurringReservationResultDto> CreateRecurringAsync(Guid userId, CreateRecurringReservationDto dto, CancellationToken cancellationToken = default)
    {
        if (!TimeOnly.TryParse(dto.StartTime, out var startTime) || !TimeOnly.TryParse(dto.EndTime, out var endTime))
            throw new InvalidOperationException("Horario inválido. Use el formato HH:mm.");
        if (startTime >= endTime)
            throw new InvalidOperationException("La hora de inicio debe ser anterior a la hora de fin.");
        if (dto.EndDate < dto.StartDate)
            throw new InvalidOperationException("La fecha final debe ser posterior o igual a la fecha inicial.");

        // Genera una ocurrencia por semana en el mismo día de la semana que StartDate.
        const int maxOccurrences = 52;
        var occurrences = new List<DateTime>();
        for (var date = dto.StartDate; date <= dto.EndDate; date = date.AddDays(7))
        {
            occurrences.Add(date.ToDateTime(startTime));
            if (occurrences.Count > maxOccurrences)
                throw new InvalidOperationException($"La recurrencia genera demasiadas ocurrencias (máximo {maxOccurrences}). Acorte el rango de fechas.");
        }
        if (occurrences.Count == 0)
            throw new InvalidOperationException("El rango de fechas no genera ninguna ocurrencia.");

        Guid? firstCreatedId = null;
        var result = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var res = new RecurringReservationResultDto { TotalOccurrences = occurrences.Count };
            await _uow.AcquireRoomLockAsync(dto.RoomId, timeoutMs: 10000, ct);

            var created = new List<Reservation>();
            foreach (var start in occurrences)
            {
                var end = start.Date.Add(endTime.ToTimeSpan());
                try
                {
                    await ValidateReservationAsync(dto.RoomId, start, end, dto.PeopleCount, null, ct);
                }
                catch (Exception ex)
                {
                    res.Skipped.Add(new SkippedOccurrenceDto { Date = start, Reason = ex.Message });
                    continue;
                }

                var reservation = new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = userId,
                    StartDateTime = start,
                    EndDateTime = end,
                    PeopleCount = dto.PeopleCount,
                    Purpose = dto.Purpose.Trim(),
                    Notes = dto.Notes?.Trim(),
                    Status = ReservationStatus.Pending
                };
                await _uow.Reservations.AddAsync(reservation, ct);
                created.Add(reservation);
            }

            if (created.Count > 0)
                await _uow.SaveChangesAsync(ct);

            res.CreatedCount = created.Count;
            firstCreatedId = created.Count > 0 ? created[0].Id : null;
            return res;
        }, cancellationToken);

        if (result.CreatedCount > 0 && firstCreatedId.HasValue)
        {
            await _auditService.LogAsync(userId, "RESERVATION_RECURRING_CREATED", entityName: "Reservation", entityId: firstCreatedId.Value.ToString(), module: "Reservations");
            await _realtime.ReservationChangedAsync(firstCreatedId.Value, dto.RoomId, "created", cancellationToken);
        }

        return result;
    }

    public async Task<ReservationDto> AdminDirectCreateAsync(Guid adminId, AdminDirectReservationDto dto, CancellationToken cancellationToken = default)
    {
        var reservationId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            await _uow.AcquireRoomLockAsync(dto.RoomId, timeoutMs: 5000, ct);
            await ValidateReservationAsync(dto.RoomId, dto.StartDateTime, dto.EndDateTime, dto.PeopleCount, null, ct);

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

            await _uow.Reservations.AddAsync(reservation, ct);
            await _uow.SaveChangesAsync(ct);
            return reservation.Id;
        }, cancellationToken);

        await _auditService.LogAsync(adminId, "RESERVATION_DIRECT_CREATED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservationId, dto.RoomId, "created", cancellationToken);
        return await GetByIdAsync(reservationId, cancellationToken);
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
        await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, ct)
                ?? throw new KeyNotFoundException("Reserva no encontrada.");

            if (reservation.Status != ReservationStatus.Pending)
                throw new InvalidOperationException("Solo se pueden aprobar reservas en estado Pendiente.");

            // Lock por sala para evitar que otro admin apruebe una reserva conflictiva al mismo tiempo.
            await _uow.AcquireRoomLockAsync(reservation.RoomId, timeoutMs: 5000, ct);

            // Re-validate conflict within transaction
            if (await _uow.Reservations.HasConflictAsync(reservation.RoomId, reservation.StartDateTime, reservation.EndDateTime, reservationId, ct))
                throw new InvalidOperationException("La sala ya fue reservada en ese horario. Existe un conflicto de disponibilidad.");

            reservation.Status = ReservationStatus.Approved;
            reservation.AdminComment = dto.AdminComment?.Trim();
            reservation.ApprovedByUserId = approvedBy;
            reservation.ApprovedAt = DateTime.UtcNow;
            _uow.Reservations.Update(reservation);
            await _uow.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        var result = await GetByIdAsync(reservationId, cancellationToken);

        await _auditService.LogAsync(approvedBy, "RESERVATION_APPROVED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(result.Id, result.RoomId, "approved", cancellationToken);

        try
        {
            await _emailService.SendTemplateAsync(result.UserEmail, "reservation_approved", new Dictionary<string, string>
            {
                ["roomName"] = result.RoomName,
                ["date"] = result.StartDateTime.ToString("dd/MM/yyyy"),
                ["startTime"] = result.StartDateTime.ToString("HH:mm"),
                ["endTime"] = result.EndDateTime.ToString("HH:mm"),
                ["purpose"] = result.Purpose
            }, cancellationToken);
        }
        catch { /* non-critical */ }

        return result;
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
        var projections = await _uow.Reservations.GetForCalendarAsync(from, to, roomId, cancellationToken);
        return projections.Select(p => new CalendarEventDto
        {
            Id = p.Id,
            Title = $"{p.RoomName} - {p.UserFullName}",
            Start = p.StartDateTime,
            End = p.EndDateTime,
            Color = p.RoomColor,
            RoomId = p.RoomId,
            RoomName = p.RoomName,
            UserId = p.UserId,
            UserName = p.UserFullName,
            Status = p.Status.ToString()
        });
    }

    private async Task ValidateReservationAsync(Guid roomId, DateTime start, DateTime end, int peopleCount, Guid? excludeId, CancellationToken ct)
    {
        if (start >= end) throw new InvalidOperationException("La hora de inicio debe ser anterior a la hora de fin.");
        if (start < BusinessNow) throw new InvalidOperationException("No se pueden crear reservas en el pasado. Seleccione una hora futura.");

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
