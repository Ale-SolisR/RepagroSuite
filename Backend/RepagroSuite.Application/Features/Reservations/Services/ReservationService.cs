using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Reservations.DTOs;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Reservations.Services;

public class ReservationService : IReservationService
{
    // Las horas se manejan como hora-de-pared de Costa Rica (UTC-6) en todo el sistema vía
    // BusinessClock. Comparar contra UTC rechazaría horas válidas de hoy en un servidor UTC.

    // Ventana de auto-aprobación: una solicitud cuyo inicio está a 30 min o menos se aprueba sola.
    private const int AutoApproveWindowMinutes = 30;

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

    public async Task<ReservationDto> CreateAsync(Guid userId, CreateReservationDto dto, bool callerCanApprove = false, CancellationToken cancellationToken = default)
    {
        // Admin/master → aprobada directa. Usuario normal → aprobada directa solo si el inicio
        // cae dentro de la ventana de 30 min; de lo contrario queda Pendiente.
        var autoApprove = callerCanApprove || dto.StartDateTime <= BusinessClock.Now.AddMinutes(AutoApproveWindowMinutes);

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
                Status = autoApprove ? ReservationStatus.Approved : ReservationStatus.Pending,
                ApprovedByUserId = callerCanApprove ? userId : null,
                ApprovedAt = autoApprove ? BusinessClock.Now : null,
            };

            await _uow.Reservations.AddAsync(reservation, ct);
            await _uow.SaveChangesAsync(ct);
            return reservation.Id;
        }, cancellationToken);

        var action = autoApprove ? "RESERVATION_CREATED_APPROVED" : "RESERVATION_CREATED";
        await _auditService.LogAsync(userId, action, entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservationId, dto.RoomId, "created", cancellationToken);
        return await GetByIdAsync(reservationId, cancellationToken);
    }

    public async Task<RecurringReservationResultDto> CreateRecurringAsync(Guid userId, CreateRecurringReservationDto dto, bool callerCanApprove = false, CancellationToken cancellationToken = default)
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

        // Mismo identificador para toda la serie: permite agruparla en la auditoría.
        var recurrenceGroupId = Guid.NewGuid();
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

                var autoApprove = callerCanApprove || start <= BusinessClock.Now.AddMinutes(AutoApproveWindowMinutes);
                var reservation = new Reservation
                {
                    RoomId = dto.RoomId,
                    UserId = userId,
                    StartDateTime = start,
                    EndDateTime = end,
                    PeopleCount = dto.PeopleCount,
                    Purpose = dto.Purpose.Trim(),
                    Notes = dto.Notes?.Trim(),
                    Status = autoApprove ? ReservationStatus.Approved : ReservationStatus.Pending,
                    ApprovedByUserId = callerCanApprove ? userId : null,
                    ApprovedAt = autoApprove ? BusinessClock.Now : null,
                    RecurrenceGroupId = recurrenceGroupId,
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
                ApprovedAt = BusinessClock.Now
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

    public async Task<PagedResult<ReservationDto>> GetPagedAsync(int page, int pageSize, Guid? userId = null, Guid? roomId = null, ReservationStatus? status = null, DateTime? from = null, DateTime? to = null, bool sortDescending = true, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.Reservations.GetPagedAsync(page, pageSize, userId, roomId, status, from, to, sortDescending, cancellationToken);
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
            reservation.ApprovedAt = BusinessClock.Now;
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
                ["purpose"] = result.Purpose,
                ["adminName"] = await GetUserFullNameAsync(approvedBy, cancellationToken)
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
        reservation.RejectedAt = BusinessClock.Now;
        _uow.Reservations.Update(reservation);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(rejectedBy, "RESERVATION_REJECTED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "rejected", cancellationToken);

        try
        {
            await _emailService.SendTemplateAsync(reservation.User.Email, "reservation_rejected", new Dictionary<string, string>
            {
                ["roomName"] = reservation.Room.Name,
                ["adminComment"] = dto.AdminComment,
                ["adminName"] = await GetUserFullNameAsync(rejectedBy, cancellationToken)
            }, cancellationToken);
        }
        catch { /* non-critical */ }

        return MapToDto(reservation);
    }

    public async Task<ReservationDto> CancelAsync(Guid reservationId, CancelReservationDto dto, Guid cancelledBy, bool canManageAny = false, CancellationToken cancellationToken = default)
    {
        var reservation = await _uow.Reservations.GetWithDetailsAsync(reservationId, cancellationToken)
            ?? throw new KeyNotFoundException("Reserva no encontrada.");

        // Un usuario normal solo puede cancelar sus propias reservas; admin/master, las de cualquiera.
        if (!canManageAny && reservation.UserId != cancelledBy)
            throw new InvalidOperationException("No tiene permiso para cancelar esta reserva.");

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Finished)
            throw new InvalidOperationException("La reserva ya fue cancelada o finalizada.");

        // Las reservas que ya iniciaron no se cancelan: se conservan en el calendario como historial.
        if (reservation.StartDateTime <= BusinessClock.Now)
            throw new InvalidOperationException("No se puede cancelar una reserva que ya inició. Se conserva como historial.");

        // Una reserva ya aprobada era una cita confirmada: al cancelarla se notifica por correo al usuario.
        var wasApproved = reservation.Status == ReservationStatus.Approved;

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancellationReason = dto.Reason.Trim();
        reservation.CancelledByUserId = cancelledBy;
        reservation.CancelledAt = BusinessClock.Now;
        _uow.Reservations.Update(reservation);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(cancelledBy, "RESERVATION_CANCELLED", entityName: "Reservation", entityId: reservationId.ToString(), module: "Reservations");
        await _realtime.ReservationChangedAsync(reservation.Id, reservation.RoomId, "cancelled", cancellationToken);

        if (wasApproved)
        {
            try
            {
                await _emailService.SendTemplateAsync(reservation.User.Email, "reservation_cancelled", new Dictionary<string, string>
                {
                    ["fullName"] = reservation.User.FullName,
                    ["roomName"] = reservation.Room.Name,
                    ["date"] = reservation.StartDateTime.ToString("dd/MM/yyyy"),
                    ["startTime"] = reservation.StartDateTime.ToString("HH:mm"),
                    ["endTime"] = reservation.EndDateTime.ToString("HH:mm"),
                    ["reason"] = dto.Reason.Trim(),
                    ["adminName"] = await GetUserFullNameAsync(cancelledBy, cancellationToken)
                }, cancellationToken);
            }
            catch { /* non-critical */ }
        }

        return MapToDto(reservation);
    }

    public async Task<int> AutoApproveDueAsync(CancellationToken cancellationToken = default)
    {
        // Pendientes cuyo inicio está a 30 min o menos (incluye las ya vencidas).
        var threshold = BusinessClock.Now.AddMinutes(AutoApproveWindowMinutes);
        var due = (await _uow.Reservations.GetPendingDueAsync(threshold, cancellationToken)).ToList();
        if (due.Count == 0) return 0;

        var processed = 0;
        foreach (var pending in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Cada pendiente se resuelve en su propia transacción con lock por sala.
                var outcome = await _uow.ExecuteInTransactionAsync<ReservationStatus?>(async ct =>
                {
                    await _uow.AcquireRoomLockAsync(pending.RoomId, timeoutMs: 5000, ct);

                    var res = await _uow.Reservations.GetWithDetailsAsync(pending.Id, ct);
                    if (res == null || res.Status != ReservationStatus.Pending)
                        return null;

                    // Choca con una YA aprobada → rechazo automático; si no, aprobación automática.
                    var conflict = await _uow.Reservations.HasApprovedConflictAsync(res.RoomId, res.StartDateTime, res.EndDateTime, res.Id, ct);
                    if (conflict)
                    {
                        res.Status = ReservationStatus.Rejected;
                        res.AdminComment = "Rechazada automáticamente: la sala ya tenía una reserva aprobada en ese horario.";
                        res.RejectedAt = BusinessClock.Now;
                    }
                    else
                    {
                        res.Status = ReservationStatus.Approved;
                        res.ApprovedAt = BusinessClock.Now;
                    }
                    _uow.Reservations.Update(res);
                    await _uow.SaveChangesAsync(ct);
                    return res.Status;
                }, cancellationToken);

                if (outcome.HasValue)
                {
                    processed++;
                    var approved = outcome.Value == ReservationStatus.Approved;
                    await _auditService.LogAsync(null,
                        approved ? "RESERVATION_AUTO_APPROVED" : "RESERVATION_AUTO_REJECTED",
                        entityName: "Reservation", entityId: pending.Id.ToString(), module: "Reservations");
                    await _realtime.ReservationChangedAsync(pending.Id, pending.RoomId, approved ? "approved" : "rejected", cancellationToken);
                }
            }
            catch
            {
                // Si una falla (lock/concurrencia), se reintenta en el siguiente ciclo del servicio.
            }
        }
        return processed;
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
        if (start < BusinessClock.Now) throw new InvalidOperationException("No se pueden crear reservas en el pasado. Seleccione una hora futura.");

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

    public async Task<PagedResult<ReservationGroupDto>> GetAuditGroupsAsync(int page, int pageSize, Guid? userId, Guid? roomId, ReservationStatus? status, bool sortDescending, CancellationToken cancellationToken = default)
    {
        var (keys, total) = await _uow.Reservations.GetAuditGroupKeysAsync(userId, roomId, status, sortDescending, page, pageSize, cancellationToken);
        var rows = (await _uow.Reservations.GetByGroupKeysAsync(keys, cancellationToken)).ToList();

        // Lookup en lote de los admins que tomaron acción (aprobar/rechazar/cancelar).
        var actionNames = await GetActionUserNamesAsync(rows, cancellationToken);

        var byKey = rows
            .GroupBy(r => r.RecurrenceGroupId ?? r.Id)
            .ToDictionary(g => g.Key, g => BuildGroupDto(g.ToList(), actionNames));

        // Conserva el orden de página devuelto por la consulta de claves.
        var items = keys.Where(byKey.ContainsKey).Select(k => byKey[k]).ToList();

        return new PagedResult<ReservationGroupDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ReservationDto>> GetGroupOccurrencesAsync(Guid recurrenceGroupId, CancellationToken cancellationToken = default)
    {
        var occurrences = (await _uow.Reservations.GetByRecurrenceGroupAsync(recurrenceGroupId, cancellationToken)).ToList();
        var actionNames = await GetActionUserNamesAsync(occurrences, cancellationToken);
        return occurrences.Select(r =>
        {
            var dto = MapToDto(r);
            ApplyActionNames(dto, r, actionNames);
            return dto;
        });
    }

    public async Task<BulkActionResultDto> ApproveGroupAsync(Guid recurrenceGroupId, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        var pending = (await _uow.Reservations.GetByRecurrenceGroupAsync(recurrenceGroupId, cancellationToken))
            .Where(r => r.Status == ReservationStatus.Pending)
            .ToList();

        int affected = 0, skipped = 0;
        foreach (var occ in pending)
        {
            try
            {
                var approved = await _uow.ExecuteInTransactionAsync(async ct =>
                {
                    await _uow.AcquireRoomLockAsync(occ.RoomId, timeoutMs: 5000, ct);
                    var res = await _uow.Reservations.GetWithDetailsAsync(occ.Id, ct);
                    if (res == null || res.Status != ReservationStatus.Pending) return false;
                    // Solo se omite si choca con una YA aprobada (no entre pendientes de la misma serie).
                    if (await _uow.Reservations.HasApprovedConflictAsync(res.RoomId, res.StartDateTime, res.EndDateTime, res.Id, ct))
                        return false;
                    res.Status = ReservationStatus.Approved;
                    res.ApprovedByUserId = approvedBy;
                    res.ApprovedAt = BusinessClock.Now;
                    _uow.Reservations.Update(res);
                    await _uow.SaveChangesAsync(ct);
                    return true;
                }, cancellationToken);

                if (approved) { affected++; await _realtime.ReservationChangedAsync(occ.Id, occ.RoomId, "approved", cancellationToken); }
                else skipped++;
            }
            catch { skipped++; }
        }

        if (affected > 0)
            await _auditService.LogAsync(approvedBy, "RESERVATION_GROUP_APPROVED", entityName: "Reservation", entityId: recurrenceGroupId.ToString(), module: "Reservations");

        return new BulkActionResultDto { Affected = affected, Skipped = skipped };
    }

    public async Task<BulkActionResultDto> RejectGroupAsync(Guid recurrenceGroupId, RejectReservationDto dto, Guid rejectedBy, CancellationToken cancellationToken = default)
    {
        var pending = (await _uow.Reservations.GetByRecurrenceGroupAsync(recurrenceGroupId, cancellationToken))
            .Where(r => r.Status == ReservationStatus.Pending)
            .ToList();

        int affected = 0;
        foreach (var occ in pending)
        {
            try
            {
                var res = await _uow.Reservations.GetWithDetailsAsync(occ.Id, cancellationToken);
                if (res == null || res.Status != ReservationStatus.Pending) continue;
                res.Status = ReservationStatus.Rejected;
                res.AdminComment = dto.AdminComment.Trim();
                res.RejectedByUserId = rejectedBy;
                res.RejectedAt = BusinessClock.Now;
                _uow.Reservations.Update(res);
                await _uow.SaveChangesAsync(cancellationToken);
                affected++;
                await _realtime.ReservationChangedAsync(occ.Id, occ.RoomId, "rejected", cancellationToken);
            }
            catch { /* continúa con la siguiente */ }
        }

        if (affected > 0)
            await _auditService.LogAsync(rejectedBy, "RESERVATION_GROUP_REJECTED", entityName: "Reservation", entityId: recurrenceGroupId.ToString(), module: "Reservations");

        return new BulkActionResultDto { Affected = affected };
    }

    private static ReservationGroupDto BuildGroupDto(List<Reservation> list, Dictionary<Guid, string> actionNames)
    {
        var ordered = list.OrderBy(r => r.StartDateTime).ToList();
        var first = ordered[0];
        var isRecurring = first.RecurrenceGroupId != null;
        ReservationDto? singleDto = null;
        if (!isRecurring)
        {
            singleDto = MapToDto(first);
            ApplyActionNames(singleDto, first, actionNames);
        }
        return new ReservationGroupDto
        {
            GroupKey = (first.RecurrenceGroupId ?? first.Id).ToString(),
            IsRecurring = isRecurring,
            RecurrenceGroupId = first.RecurrenceGroupId,
            RoomId = first.RoomId,
            RoomName = first.Room?.Name ?? string.Empty,
            UserId = first.UserId,
            UserFullName = first.User?.FullName ?? string.Empty,
            Purpose = first.Purpose,
            FirstStart = ordered.Min(r => r.StartDateTime),
            LastStart = ordered.Max(r => r.StartDateTime),
            OccurrenceCount = ordered.Count,
            PendingCount = ordered.Count(r => r.Status == ReservationStatus.Pending),
            ApprovedCount = ordered.Count(r => r.Status == ReservationStatus.Approved),
            RejectedCount = ordered.Count(r => r.Status == ReservationStatus.Rejected),
            CancelledCount = ordered.Count(r => r.Status == ReservationStatus.Cancelled),
            Single = singleDto
        };
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
        RejectedByUserId = r.RejectedByUserId,
        RejectedAt = r.RejectedAt,
        CancelledByUserId = r.CancelledByUserId,
        CancellationReason = r.CancellationReason,
        CancelledAt = r.CancelledAt,
        IsDirectAdminReservation = r.IsDirectAdminReservation,
        RecurrenceGroupId = r.RecurrenceGroupId,
        CreatedAt = r.CreatedAt
    };

    // Lookup puntual del nombre completo de un usuario (admin que ejecutó una acción).
    // Devuelve string.Empty si no se encuentra (fallback seguro para emails).
    private async Task<string> GetUserFullNameAsync(Guid userId, CancellationToken ct)
    {
        var users = await _uow.Users.FindAsync(u => u.Id == userId, ct);
        return users.FirstOrDefault()?.FullName ?? string.Empty;
    }

    // Lookup en lote de nombres de admin que aprobaron/rechazaron/cancelaron, evitando N+1.
    private async Task<Dictionary<Guid, string>> GetActionUserNamesAsync(IEnumerable<Reservation> reservations, CancellationToken ct)
    {
        var ids = reservations
            .SelectMany(r => new[] { r.ApprovedByUserId, r.RejectedByUserId, r.CancelledByUserId })
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var users = await _uow.Users.FindAsync(u => ids.Contains(u.Id), ct);
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }

    private static void ApplyActionNames(ReservationDto dto, Reservation r, Dictionary<Guid, string> names)
    {
        if (r.ApprovedByUserId.HasValue && names.TryGetValue(r.ApprovedByUserId.Value, out var an)) dto.ApprovedByName = an;
        if (r.RejectedByUserId.HasValue && names.TryGetValue(r.RejectedByUserId.Value, out var rn)) dto.RejectedByName = rn;
        if (r.CancelledByUserId.HasValue && names.TryGetValue(r.CancelledByUserId.Value, out var cn)) dto.CancelledByName = cn;
    }
}
