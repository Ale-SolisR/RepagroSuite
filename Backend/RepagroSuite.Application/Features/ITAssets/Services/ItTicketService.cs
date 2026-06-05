using System.Security.Cryptography;
using System.Text;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public class ItTicketService : IItTicketService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ISequenceGenerator _sequence;
    private readonly IPdfGenerator _pdf;

    public ItTicketService(IUnitOfWork uow, IAuditService audit, ICurrentUserService currentUser,
        ISequenceGenerator sequence, IPdfGenerator pdf)
    {
        _uow = uow;
        _audit = audit;
        _currentUser = currentUser;
        _sequence = sequence;
        _pdf = pdf;
    }

    public async Task<PagedResult<ItTicketListDto>> GetPagedAsync(int page, int pageSize, ItTicketType? type,
        ItTicketStatus? status, string? search, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.ItTickets.GetPagedAsync(page, pageSize, type, status, search, cancellationToken);
        return new PagedResult<ItTicketListDto>
        {
            Items = items.Select(MapToListDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ItTicketDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _uow.ItTickets.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Boleta no encontrada.");
        var dto = MapToDto(t);
        dto.Chain = await BuildChainAsync(id, cancellationToken);
        return dto;
    }

    /// <summary>
    /// Cadena de trazabilidad de una boleta vía las asignaciones: si es una entrega, lista las
    /// boletas que la cerraron (Relation="Cierre"); si es un cierre (devolución/desasignación/incidente),
    /// lista la entrega que respalda (Relation="Origen"). Sin duplicados.
    /// </summary>
    private async Task<List<ItTicketChainLinkDto>> BuildChainAsync(Guid ticketId, CancellationToken ct)
    {
        var assignments = await _uow.ItTickets.GetChainAssignmentsAsync(ticketId, ct);
        var links = new List<ItTicketChainLinkDto>();

        foreach (var a in assignments)
        {
            // Esta boleta es la ENTREGA → su cierre es ReturnTicket.
            if (a.AssignedTicketId == ticketId && a.ReturnTicket is { } close)
                links.Add(ToChainLink(close, "Cierre", a.Asset?.InternalCode));
            // Esta boleta es el CIERRE → su origen es AssignedTicket.
            if (a.ReturnTicketId == ticketId && a.AssignedTicket is { } origin)
                links.Add(ToChainLink(origin, "Origen", a.Asset?.InternalCode));
        }

        return links
            .GroupBy(l => new { l.TicketId, l.AssetCode })
            .Select(g => g.First())
            .OrderBy(l => l.IssuedAt)
            .ToList();
    }

    private static ItTicketChainLinkDto ToChainLink(ItTicket t, string relation, string? assetCode) => new()
    {
        TicketId = t.Id,
        TicketNumber = t.TicketNumber,
        TicketType = t.TicketType,
        TicketTypeName = TicketTypeName(t.TicketType),
        Status = t.Status,
        StatusName = TicketStatusName(t.Status),
        IssuedAt = t.IssuedAt,
        Relation = relation,
        AssetCode = assetCode,
    };

    public async Task<(byte[] Bytes, string FileName)> GetPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Con detalles para incluir el colaborador en el nombre del archivo.
        var t = await _uow.ItTickets.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Boleta no encontrada.");
        if (string.IsNullOrEmpty(t.PdfBase64))
            throw new InvalidOperationException("La boleta no tiene PDF generado.");
        return (Convert.FromBase64String(t.PdfBase64), PdfFileName(t));
    }

    /// <summary>Nombre de archivo identificativo y seguro: «{N° boleta}_{tipo}_{colaborador}.pdf».
    /// Ej.: TI-PRB-2026-000001_Perdida-robo-de-equipo_SOLIS-ROJAS-LUIS-ALEJANDRO.pdf</summary>
    private static string PdfFileName(ItTicket t)
    {
        var parts = new List<string> { Slug(t.TicketNumber) };
        var type = Slug(TicketTypeName(t.TicketType));
        if (!string.IsNullOrEmpty(type)) parts.Add(type);
        var emp = Slug(t.Employee?.FullName);
        if (!string.IsNullOrEmpty(emp)) parts.Add(emp);
        var name = string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
        return (string.IsNullOrEmpty(name) ? $"boleta-{t.TicketNumber}" : name) + ".pdf";
    }

    /// <summary>Quita tildes y caracteres no válidos para usar el texto como nombre de archivo.</summary>
    private static string Slug(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var normalized = s.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '/' or '\\' or '.' or ',' or ':') sb.Append('-');
        }
        var result = sb.ToString();
        while (result.Contains("--")) result = result.Replace("--", "-");
        return result.Trim('-');
    }

    public async Task<ItTicketDto> CreateAssignmentAsync(CreateAssignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (dto.AssetIds.Count == 0) throw new InvalidOperationException("Seleccione al menos un activo.");
        EnsureSignatures(dto.Signatures, requireEmployee: true);

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var employee = await _uow.ItEmployees.GetByIdAsync(dto.EmployeeId, ct)
                ?? throw new InvalidOperationException("Colaborador no encontrado.");
            if (!employee.IsActive)
                throw new InvalidOperationException("El colaborador no está activo.");

            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(ItTicketType.Entrega), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = ItTicketType.Entrega,
                Status = ItTicketStatus.Emitida,
                EmployeeId = employee.Id,
                ItResponsibleUserId = actorUserId,
                Notes = dto.Notes?.Trim(),
                CreatedBy = actorUserId
            };

            foreach (var assetId in dto.AssetIds.Distinct())
            {
                var asset = await _uow.ItAssets.GetByIdAsync(assetId, ct)
                    ?? throw new InvalidOperationException("Activo no encontrado.");
                // Disponible o Devuelto (heredado) se pueden asignar; cualquier otro estado no.
                if (asset.Status is not (ItAssetStatus.Available or ItAssetStatus.Returned))
                    throw new InvalidOperationException($"El activo {asset.InternalCode} no está disponible (estado actual: {ItAssetService.StatusName(asset.Status)}).");

                // Cierra asignaciones colgantes del activo (datos heredados: estado cambiado sin cerrar
                // la asignación) para no chocar con el índice único de "una asignación activa por activo".
                var asgRepo = _uow.Repository<ItAssignment>();
                var dangling = (await asgRepo.FindAsync(x => x.AssetId == asset.Id && x.Status == AssignmentStatus.Activa, ct)).ToList();
                if (dangling.Count > 0)
                {
                    foreach (var d in dangling)
                    {
                        d.Status = AssignmentStatus.Cerrada;
                        d.ReturnedAt = BusinessClock.Now;
                        d.ClosedReason = "Normalizacion";
                        d.UpdatedBy = actorUserId;
                        asgRepo.Update(d);
                    }
                    await _uow.SaveChangesAsync(ct);
                }

                ticket.Details.Add(new ItTicketDetail
                {
                    AssetId = asset.Id, LineType = "ASSET",
                    Description = asset.Model, Condition = dto.ConditionOut.ToString(), CreatedBy = actorUserId
                });

                await _uow.Repository<ItAssignment>().AddAsync(new ItAssignment
                {
                    AssetId = asset.Id, EmployeeId = employee.Id, AssignedTicketId = ticket.Id,
                    ConditionOut = dto.ConditionOut, Accessories = dto.Accessories?.Trim(),
                    Status = AssignmentStatus.Activa, CreatedBy = actorUserId
                }, ct);

                var from = asset.Status;
                asset.Status = ItAssetStatus.Assigned;
                asset.CurrentHolderEmployeeId = employee.Id;
                asset.UpdatedBy = actorUserId;
                _uow.ItAssets.Update(asset);
                await AddHistoryAsync(asset.Id, from, ItAssetStatus.Assigned, $"Asignado a {employee.FullName} ({number}).", actorUserId, ct, ticket.Id);
            }

            AttachEvidence(ticket, dto.Photos, dto.Signatures, actorUserId);
            await _uow.ItTickets.AddAsync(ticket, ct);
            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, "TI_TICKET_ASSIGNMENT", entityName: "ItTicket", entityId: ticketId.ToString(),
            module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(ticketId, cancellationToken);
    }

    public async Task<ItTicketDto> CreateReturnAsync(CreateReturnDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var validResults = new[] { ItAssetStatus.Available, ItAssetStatus.UnderReview, ItAssetStatus.UnderRepair, ItAssetStatus.Damaged, ItAssetStatus.Disposed };
        if (!validResults.Contains(dto.ResultingStatus))
            throw new InvalidOperationException("Estado resultante de devolución inválido.");
        EnsureSignatures(dto.Signatures, requireEmployee: true);

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var assignment = await _uow.ItTickets.GetActiveAssignmentAsync(dto.AssetId, ct)
                ?? throw new InvalidOperationException("El activo no tiene una asignación activa.");

            var asset = await _uow.ItAssets.GetByIdAsync(dto.AssetId, ct)
                ?? throw new InvalidOperationException("Activo no encontrado.");

            if (!ItAsset.CanTransition(asset.Status, dto.ResultingStatus))
                throw new InvalidOperationException($"Transición no permitida: {ItAssetService.StatusName(asset.Status)} → {ItAssetService.StatusName(dto.ResultingStatus)}.");

            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(ItTicketType.Devolucion), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = ItTicketType.Devolucion,
                Status = ItTicketStatus.Emitida,
                EmployeeId = assignment.EmployeeId,
                ItResponsibleUserId = actorUserId,
                Notes = dto.ReturnNotes?.Trim(),
                CreatedBy = actorUserId
            };
            ticket.Details.Add(new ItTicketDetail
            {
                AssetId = asset.Id, LineType = "ASSET", Description = asset.Model,
                Condition = dto.ConditionIn.ToString(), CreatedBy = actorUserId
            });
            AttachEvidence(ticket, dto.Photos, dto.Signatures, actorUserId);
            await _uow.ItTickets.AddAsync(ticket, ct);
            await _uow.SaveChangesAsync(ct);

            // Cierra la asignación
            assignment.Status = AssignmentStatus.Cerrada;
            assignment.ReturnedAt = BusinessClock.Now;
            assignment.ConditionIn = dto.ConditionIn;
            assignment.ReturnTicketId = ticket.Id;
            assignment.ReturnNotes = dto.ReturnNotes?.Trim();
            assignment.ClosedReason = nameof(ItTicketType.Devolucion);
            assignment.UpdatedBy = actorUserId;
            _uow.Repository<ItAssignment>().Update(assignment);

            var from = asset.Status;
            asset.Status = dto.ResultingStatus;
            asset.CurrentHolderEmployeeId = null;
            asset.PhysicalCondition = dto.ConditionIn;
            asset.UpdatedBy = actorUserId;
            _uow.ItAssets.Update(asset);
            await AddHistoryAsync(asset.Id, from, dto.ResultingStatus, $"Devuelto ({number}).", actorUserId, ct, ticket.Id);

            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, "TI_TICKET_RETURN", entityName: "ItTicket", entityId: ticketId.ToString(),
            module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(ticketId, cancellationToken);
    }

    public async Task<ItTicketDto> CreateDeassignmentAsync(CreateDeassignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        EnsureSignatures(dto.Signatures, requireEmployee: true);

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var assignment = await _uow.ItTickets.GetActiveAssignmentAsync(dto.AssetId, ct)
                ?? throw new InvalidOperationException("El activo no tiene una asignación activa.");
            var asset = await _uow.ItAssets.GetByIdAsync(dto.AssetId, ct)
                ?? throw new InvalidOperationException("Activo no encontrado.");

            if (!ItAsset.CanTransition(asset.Status, ItAssetStatus.Available))
                throw new InvalidOperationException($"No se puede desasignar desde el estado {ItAssetService.StatusName(asset.Status)}.");

            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(ItTicketType.Desasignacion), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = ItTicketType.Desasignacion,
                Status = ItTicketStatus.Emitida,
                EmployeeId = assignment.EmployeeId,
                ItResponsibleUserId = actorUserId,
                Notes = dto.Notes?.Trim(),
                CreatedBy = actorUserId
            };
            ticket.Details.Add(new ItTicketDetail { AssetId = asset.Id, LineType = "ASSET", Description = asset.Model, CreatedBy = actorUserId });
            AttachEvidence(ticket, dto.Photos, dto.Signatures, actorUserId);
            await _uow.ItTickets.AddAsync(ticket, ct);
            await _uow.SaveChangesAsync(ct);

            assignment.Status = AssignmentStatus.Cerrada;
            assignment.ReturnedAt = BusinessClock.Now;
            assignment.ReturnTicketId = ticket.Id;
            assignment.ReturnNotes = dto.Notes?.Trim();
            assignment.ClosedReason = nameof(ItTicketType.Desasignacion);
            assignment.UpdatedBy = actorUserId;
            _uow.Repository<ItAssignment>().Update(assignment);

            var from = asset.Status;
            asset.Status = ItAssetStatus.Available;
            asset.CurrentHolderEmployeeId = null;
            asset.UpdatedBy = actorUserId;
            _uow.ItAssets.Update(asset);
            await AddHistoryAsync(asset.Id, from, ItAssetStatus.Available, $"Desasignado ({number}).", actorUserId, ct, ticket.Id);

            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, "TI_TICKET_DEASSIGNMENT", entityName: "ItTicket", entityId: ticketId.ToString(),
            module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(ticketId, cancellationToken);
    }

    public async Task<ItTicketDto> CreateIncidentAsync(CreateIncidentDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (dto.TargetStatus is not (ItAssetStatus.Damaged or ItAssetStatus.Lost or ItAssetStatus.Stolen))
            throw new InvalidOperationException("Estado de incidente inválido (use Dañado, Perdido o Robado).");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("El incidente requiere un motivo.");
        // En incidentes la firma del colaborador es opcional (puede no estar presente); la de TI es obligatoria.
        EnsureSignatures(dto.Signatures, requireEmployee: false);

        var ticketType = dto.TargetStatus == ItAssetStatus.Damaged ? ItTicketType.Deterioro : ItTicketType.PerdidaRobo;

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var asset = await _uow.ItAssets.GetByIdAsync(dto.AssetId, ct)
                ?? throw new InvalidOperationException("Activo no encontrado.");
            if (!ItAsset.CanTransition(asset.Status, dto.TargetStatus))
                throw new InvalidOperationException($"Transición no permitida: {ItAssetService.StatusName(asset.Status)} → {ItAssetService.StatusName(dto.TargetStatus)}.");

            var assignment = await _uow.ItTickets.GetActiveAssignmentAsync(dto.AssetId, ct);

            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(ticketType), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = ticketType,
                Status = ItTicketStatus.Emitida,
                EmployeeId = assignment?.EmployeeId,
                ItResponsibleUserId = actorUserId,
                Notes = dto.Reason.Trim(),
                CreatedBy = actorUserId
            };
            ticket.Details.Add(new ItTicketDetail
            {
                AssetId = asset.Id, LineType = "ASSET", Description = asset.Model,
                Condition = dto.Condition?.ToString(), CreatedBy = actorUserId
            });
            AttachEvidence(ticket, dto.Photos, dto.Signatures, actorUserId);
            await _uow.ItTickets.AddAsync(ticket, ct);
            await _uow.SaveChangesAsync(ct);

            // Si estaba asignado, cierra la asignación y limpia el responsable (estado coherente).
            if (assignment is not null)
            {
                assignment.Status = AssignmentStatus.Cerrada;
                assignment.ReturnedAt = BusinessClock.Now;
                assignment.ReturnTicketId = ticket.Id;
                assignment.ReturnNotes = dto.Reason.Trim();
                assignment.ClosedReason = ticketType.ToString();
                assignment.UpdatedBy = actorUserId;
                _uow.Repository<ItAssignment>().Update(assignment);
            }

            var from = asset.Status;
            asset.Status = dto.TargetStatus;
            asset.CurrentHolderEmployeeId = null;
            if (dto.Condition.HasValue) asset.PhysicalCondition = dto.Condition.Value;
            asset.UpdatedBy = actorUserId;
            _uow.ItAssets.Update(asset);
            await AddHistoryAsync(asset.Id, from, dto.TargetStatus,
                $"{ItTicketService.TicketTypeName(ticketType)} ({number}). {dto.Reason.Trim()}", actorUserId, ct, ticket.Id);

            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, ticketType == ItTicketType.Deterioro ? "TI_TICKET_DAMAGE" : "TI_TICKET_LOSSTHEFT",
            entityName: "ItTicket", entityId: ticketId.ToString(), module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(ticketId, cancellationToken);
    }

    public async Task<ItTicketDto> CreateGenericTicketAsync(CreateGenericTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (dto.TicketType is ItTicketType.Entrega or ItTicketType.Devolucion)
            throw new InvalidOperationException("Use el flujo de asignación/devolución para esos tipos.");
        if (dto.AssetIds.Count == 0) throw new InvalidOperationException("Seleccione al menos un activo.");

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(dto.TicketType), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = dto.TicketType,
                Status = ItTicketStatus.Emitida,
                EmployeeId = dto.EmployeeId,
                ItResponsibleUserId = actorUserId,
                Notes = dto.Notes?.Trim(),
                CreatedBy = actorUserId
            };

            foreach (var assetId in dto.AssetIds.Distinct())
            {
                var asset = await _uow.ItAssets.GetByIdAsync(assetId, ct)
                    ?? throw new InvalidOperationException("Activo no encontrado.");

                ticket.Details.Add(new ItTicketDetail { AssetId = asset.Id, LineType = "ASSET", Description = asset.Model, CreatedBy = actorUserId });

                if (dto.NewAssetStatus.HasValue && dto.NewAssetStatus.Value != asset.Status)
                {
                    var target = dto.NewAssetStatus.Value;
                    if (!ItAsset.CanTransition(asset.Status, target))
                        throw new InvalidOperationException($"Transición no permitida para {asset.InternalCode}: {ItAssetService.StatusName(asset.Status)} → {ItAssetService.StatusName(target)}.");
                    if ((target is ItAssetStatus.Stolen or ItAssetStatus.Lost or ItAssetStatus.Disposed) && string.IsNullOrWhiteSpace(dto.StatusReason))
                        throw new InvalidOperationException("Este cambio de estado requiere un motivo.");

                    var from = asset.Status;
                    asset.Status = target;
                    asset.UpdatedBy = actorUserId;
                    _uow.ItAssets.Update(asset);
                    await AddHistoryAsync(asset.Id, from, target, $"{dto.TicketType} — {number}. {dto.StatusReason}".Trim(), actorUserId, ct);
                }
            }

            AttachEvidence(ticket, dto.Photos, dto.Signatures, actorUserId);
            await _uow.ItTickets.AddAsync(ticket, ct);
            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, $"TI_TICKET_{dto.TicketType.ToString().ToUpperInvariant()}", entityName: "ItTicket",
            entityId: ticketId.ToString(), module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(ticketId, cancellationToken);
    }

    public async Task<ItTicketDto> VoidAsync(Guid id, VoidTicketDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("La anulación requiere un motivo.");

        await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var ticket = await _uow.ItTickets.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException("Boleta no encontrada.");
            if (ticket.Status == ItTicketStatus.Anulada)
                throw new InvalidOperationException("La boleta ya esta anulada.");

            var reason = dto.Reason.Trim();
            ticket.Status = ItTicketStatus.Anulada;
            ticket.VoidedBy = actorUserId;
            ticket.VoidedAt = BusinessClock.Now;
            ticket.VoidReason = reason;
            ticket.UpdatedBy = actorUserId;
            _uow.ItTickets.Update(ticket);

            if (ticket.TicketType == ItTicketType.Entrega)
            {
                var assignmentRepo = _uow.Repository<ItAssignment>();
                var assignments = (await assignmentRepo.FindAsync(
                    a => a.AssignedTicketId == id && a.Status == AssignmentStatus.Activa, ct)).ToList();

                foreach (var assignment in assignments)
                {
                    var asset = await _uow.ItAssets.GetByIdAsync(assignment.AssetId, ct)
                        ?? throw new InvalidOperationException("Activo no encontrado.");

                    assignment.Status = AssignmentStatus.Cerrada;
                    assignment.ReturnedAt = BusinessClock.Now;
                    assignment.ClosedReason = "Anulacion";
                    assignment.ReturnNotes = reason;
                    assignment.UpdatedBy = actorUserId;
                    assignmentRepo.Update(assignment);

                    var from = asset.Status;
                    asset.Status = ItAssetStatus.Available;
                    asset.CurrentHolderEmployeeId = null;
                    asset.UpdatedBy = actorUserId;
                    _uow.ItAssets.Update(asset);

                    await AddHistoryAsync(asset.Id, from, ItAssetStatus.Available,
                        $"Asignacion anulada por boleta {ticket.TicketNumber}. {reason}", actorUserId, ct, ticket.Id);
                }
            }

            await _uow.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);

        await _audit.LogAsync(actorUserId, "TI_TICKET_VOIDED", entityName: "ItTicket", entityId: id.ToString(),
            newValues: new { dto.Reason }, module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private async Task AddHistoryAsync(Guid assetId, ItAssetStatus from, ItAssetStatus to, string desc, Guid by, CancellationToken ct, Guid? ticketId = null)
        => await _uow.Repository<ItAssetHistory>().AddAsync(new ItAssetHistory
        {
            AssetId = assetId, EventType = "STATUS_CHANGED", FromStatus = from, ToStatus = to,
            Description = desc, PerformedBy = by, CreatedBy = by, TicketId = ticketId
        }, ct);

    /// <summary>
    /// Valida que la boleta traiga las firmas requeridas (propuesta §9). La firma del responsable TI
    /// es SIEMPRE obligatoria. La del colaborador es obligatoria salvo en incidentes (deterioro/pérdida-robo),
    /// donde puede no estar presente.
    /// </summary>
    private static void EnsureSignatures(List<SignatureInputDto> signatures, bool requireEmployee)
    {
        bool hasIt = signatures.Any(s => s.SignerType == "ResponsableTI" && !string.IsNullOrWhiteSpace(s.ImageBase64));
        bool hasEmployee = signatures.Any(s => s.SignerType == "Colaborador" && !string.IsNullOrWhiteSpace(s.ImageBase64));
        if (!hasIt)
            throw new InvalidOperationException("Falta la firma del responsable de TI.");
        if (requireEmployee && !hasEmployee)
            throw new InvalidOperationException("Falta la firma del colaborador.");
    }

    private void AttachEvidence(ItTicket ticket, List<string> photos, List<SignatureInputDto> signatures, Guid by)
    {
        foreach (var p in photos.Where(p => !string.IsNullOrWhiteSpace(p)).Take(3))
            ticket.Photos.Add(new ItTicketPhoto
            {
                ImageBase64 = p, MimeType = MimeOf(p), SizeBytes = ApproxBytes(p),
                Sha256 = Sha256OfDataUrl(p), UploadedBy = by, CreatedBy = by
            });

        foreach (var s in signatures.Where(s => !string.IsNullOrWhiteSpace(s.ImageBase64)))
            ticket.Signatures.Add(new ItTicketSignature
            {
                SignerType = s.SignerType, SignerName = s.SignerName, ImageBase64 = s.ImageBase64,
                Sha256 = Sha256OfDataUrl(s.ImageBase64), IpAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent, AuthenticatedUserId = by, CreatedBy = by
            });
    }

    private async Task GeneratePdfAsync(Guid ticketId, CancellationToken ct)
    {
        var t = await _uow.ItTickets.GetWithDetailsAsync(ticketId, ct);
        if (t is null) return;

        var model = new TicketPdfModel
        {
            TicketNumber = t.TicketNumber,
            TypeName = TicketTypeName(t.TicketType),
            IssuedAt = t.IssuedAt.ToString("dd/MM/yyyy HH:mm"),
            EmployeeName = t.Employee?.FullName,
            EmployeeIdentification = t.Employee?.IdentificationNumber,
            ResponsibleName = t.ItResponsible?.FullName,
            ResponsibleIdentification = t.ItResponsible?.IdentificationNumber,
            Notes = t.Notes,
            Lines = t.Details.Select(d => new TicketPdfLine
            {
                InternalCode = d.Asset?.InternalCode ?? "—",
                TypeName = d.LineType == "ACCESSORY" ? "Accesorio" : null,
                Description = d.Description,
                SerialNumber = d.Asset?.SerialNumber,
                Condition = d.Condition
            }).ToList(),
            Signatures = t.Signatures.Select(s => new TicketPdfSignature
            {
                Label = s.SignerType == "ResponsableTI" ? "Responsable TI" : "Colaborador",
                SignerName = s.SignerName,
                // La cédula del firmante se toma de su persona según el tipo de firma.
                SignerIdentification = s.SignerType == "ResponsableTI"
                    ? t.ItResponsible?.IdentificationNumber
                    : t.Employee?.IdentificationNumber,
                ImageBase64 = s.ImageBase64,
                SignedAt = s.SignedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList(),
            PhotosBase64 = t.Photos.Select(p => p.ImageBase64).ToList()
        };

        var bytes = _pdf.GenerateTicketPdf(model);
        t.PdfBase64 = Convert.ToBase64String(bytes);
        t.PdfSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        _uow.ItTickets.Update(t);
        await _uow.SaveChangesAsync(ct);
    }

    private static string? MimeOf(string dataUrl)
    {
        if (dataUrl.StartsWith("data:") && dataUrl.Contains(';'))
            return dataUrl[5..dataUrl.IndexOf(';')];
        return null;
    }

    private static int? ApproxBytes(string dataUrl)
    {
        var raw = dataUrl.Contains(',') ? dataUrl[(dataUrl.IndexOf(',') + 1)..] : dataUrl;
        return (int)(raw.Length * 3L / 4);
    }

    private static string? Sha256OfDataUrl(string dataUrl)
    {
        var raw = dataUrl.Contains(',') ? dataUrl[(dataUrl.IndexOf(',') + 1)..] : dataUrl;
        try
        {
            var bytes = Convert.FromBase64String(raw);
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        catch (FormatException)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataUrl))).ToLowerInvariant();
        }
    }

    internal static string TicketTypeName(ItTicketType t) => t switch
    {
        ItTicketType.Entrega => "Entrega de equipo",
        ItTicketType.Devolucion => "Devolución de equipo",
        ItTicketType.Prestamo => "Préstamo",
        ItTicketType.Mantenimiento => "Mantenimiento",
        ItTicketType.Reparacion => "Reparación",
        ItTicketType.Traslado => "Traslado",
        ItTicketType.CambioResponsable => "Cambio de responsable",
        ItTicketType.AsignacionAccesorios => "Asignación de accesorios",
        ItTicketType.Baja => "Baja de activo",
        ItTicketType.Desasignacion => "Desasignación de equipo",
        ItTicketType.Deterioro => "Deterioro de equipo",
        ItTicketType.PerdidaRobo => "Pérdida / robo de equipo",
        _ => t.ToString()
    };

    private static string TicketStatusName(ItTicketStatus s) => s switch
    {
        ItTicketStatus.Borrador => "Borrador",
        ItTicketStatus.PendienteFirma => "Pendiente de firma",
        ItTicketStatus.Firmada => "Firmada",
        ItTicketStatus.Emitida => "Emitida",
        ItTicketStatus.Anulada => "Anulada",
        _ => s.ToString()
    };

    private static ItTicketListDto MapToListDto(ItTicket t) => new()
    {
        Id = t.Id,
        TicketNumber = t.TicketNumber,
        TicketType = t.TicketType,
        TicketTypeName = TicketTypeName(t.TicketType),
        Status = t.Status,
        StatusName = TicketStatusName(t.Status),
        IssuedAt = t.IssuedAt,
        EmployeeName = t.Employee?.FullName,
        ItResponsibleName = t.ItResponsible?.FullName,
        AssetCount = t.Details?.Count ?? 0
    };

    // Traduce la condición física almacenada (nombre del enum, p. ej. "Good") a español para mostrar.
    private static string? ConditionEs(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        return Enum.TryParse<PhysicalCondition>(raw, out var c)
            ? c switch
            {
                PhysicalCondition.New => "Nuevo",
                PhysicalCondition.Good => "Bueno",
                PhysicalCondition.Fair => "Regular",
                PhysicalCondition.Poor => "Malo",
                PhysicalCondition.Unusable => "Inservible",
                _ => raw,
            }
            : raw;
    }

    private static ItTicketDto MapToDto(ItTicket t) => new()
    {
        Id = t.Id,
        TicketNumber = t.TicketNumber,
        TicketType = t.TicketType,
        TicketTypeName = TicketTypeName(t.TicketType),
        Status = t.Status,
        StatusName = TicketStatusName(t.Status),
        IssuedAt = t.IssuedAt,
        EmployeeName = t.Employee?.FullName,
        EmployeeIdentification = t.Employee?.IdentificationNumber,
        ItResponsibleName = t.ItResponsible?.FullName,
        ItResponsibleIdentification = t.ItResponsible?.IdentificationNumber,
        AssetCount = t.Details.Count,
        Notes = t.Notes,
        PdfSha256 = t.PdfSha256,
        HasPdf = !string.IsNullOrEmpty(t.PdfBase64),
        VoidReason = t.VoidReason,
        VoidedAt = t.VoidedAt,
        Lines = t.Details.Select(d => new ItTicketLineDto
        {
            AssetId = d.AssetId,
            LineType = d.LineType,
            InternalCode = d.Asset?.InternalCode,
            TypeName = d.Asset?.AssetType?.Name,
            Description = d.Description,
            SerialNumber = d.Asset?.SerialNumber,
            Condition = ConditionEs(d.Condition)
        }).ToList(),
        Photos = t.Photos.Select(p => new ItTicketPhotoDto { Id = p.Id, ImageBase64 = p.ImageBase64 }).ToList(),
        Signatures = t.Signatures.Select(s => new ItTicketSignatureDto
        {
            SignerType = s.SignerType, SignerName = s.SignerName, ImageBase64 = s.ImageBase64, SignedAt = s.SignedAt
        }).ToList()
    };
}
