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
        return MapToDto(t);
    }

    public async Task<byte[]> GetPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _uow.ItTickets.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Boleta no encontrada.");
        if (string.IsNullOrEmpty(t.PdfBase64))
            throw new InvalidOperationException("La boleta no tiene PDF generado.");
        return Convert.FromBase64String(t.PdfBase64);
    }

    public async Task<ItTicketDto> CreateAssignmentAsync(CreateAssignmentDto dto, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (dto.AssetIds.Count == 0) throw new InvalidOperationException("Seleccione al menos un activo.");

        var ticketId = await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var employee = await _uow.Users.GetByIdAsync(dto.EmployeeUserId, ct)
                ?? throw new InvalidOperationException("Colaborador no encontrado.");
            if (employee.Status != UserStatus.Active)
                throw new InvalidOperationException("El colaborador no está activo.");

            var number = await _sequence.NextTicketNumberAsync(ItTicket.TypeCode(ItTicketType.Entrega), ct);
            var ticket = new ItTicket
            {
                TicketNumber = number,
                TicketType = ItTicketType.Entrega,
                Status = ItTicketStatus.Emitida,
                EmployeeUserId = employee.Id,
                ItResponsibleUserId = actorUserId,
                Notes = dto.Notes?.Trim(),
                CreatedBy = actorUserId
            };

            foreach (var assetId in dto.AssetIds.Distinct())
            {
                var asset = await _uow.ItAssets.GetByIdAsync(assetId, ct)
                    ?? throw new InvalidOperationException("Activo no encontrado.");
                if (asset.Status != ItAssetStatus.Available)
                    throw new InvalidOperationException($"El activo {asset.InternalCode} no está disponible (estado actual: {ItAssetService.StatusName(asset.Status)}).");

                ticket.Details.Add(new ItTicketDetail
                {
                    AssetId = asset.Id, LineType = "ASSET",
                    Description = asset.Model, Condition = dto.ConditionOut.ToString(), CreatedBy = actorUserId
                });

                await _uow.Repository<ItAssignment>().AddAsync(new ItAssignment
                {
                    AssetId = asset.Id, EmployeeUserId = employee.Id, AssignedTicketId = ticket.Id,
                    ConditionOut = dto.ConditionOut, Accessories = dto.Accessories?.Trim(),
                    Status = AssignmentStatus.Activa, CreatedBy = actorUserId
                }, ct);

                var from = asset.Status;
                asset.Status = ItAssetStatus.Assigned;
                asset.CurrentHolderUserId = employee.Id;
                asset.UpdatedBy = actorUserId;
                _uow.ItAssets.Update(asset);
                await AddHistoryAsync(asset.Id, from, ItAssetStatus.Assigned, $"Asignado a {employee.FullName} ({number}).", actorUserId, ct);
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
                EmployeeUserId = assignment.EmployeeUserId,
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
            assignment.UpdatedBy = actorUserId;
            _uow.Repository<ItAssignment>().Update(assignment);

            var from = asset.Status;
            asset.Status = dto.ResultingStatus;
            asset.CurrentHolderUserId = null;
            asset.PhysicalCondition = dto.ConditionIn;
            asset.UpdatedBy = actorUserId;
            _uow.ItAssets.Update(asset);
            await AddHistoryAsync(asset.Id, from, dto.ResultingStatus, $"Devuelto ({number}).", actorUserId, ct);

            await _uow.SaveChangesAsync(ct);
            return ticket.Id;
        }, cancellationToken);

        await GeneratePdfAsync(ticketId, cancellationToken);
        await _audit.LogAsync(actorUserId, "TI_TICKET_RETURN", entityName: "ItTicket", entityId: ticketId.ToString(),
            module: "TI", cancellationToken: cancellationToken);
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
                EmployeeUserId = dto.EmployeeUserId,
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

        var ticket = await _uow.ItTickets.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Boleta no encontrada.");
        if (ticket.Status == ItTicketStatus.Anulada)
            throw new InvalidOperationException("La boleta ya está anulada.");

        ticket.Status = ItTicketStatus.Anulada;
        ticket.VoidedBy = actorUserId;
        ticket.VoidedAt = BusinessClock.Now;
        ticket.VoidReason = dto.Reason.Trim();
        ticket.UpdatedBy = actorUserId;
        _uow.ItTickets.Update(ticket);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(actorUserId, "TI_TICKET_VOIDED", entityName: "ItTicket", entityId: id.ToString(),
            newValues: new { dto.Reason }, module: "TI", cancellationToken: cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private async Task AddHistoryAsync(Guid assetId, ItAssetStatus from, ItAssetStatus to, string desc, Guid by, CancellationToken ct)
        => await _uow.Repository<ItAssetHistory>().AddAsync(new ItAssetHistory
        {
            AssetId = assetId, EventType = "STATUS_CHANGED", FromStatus = from, ToStatus = to,
            Description = desc, PerformedBy = by, CreatedBy = by
        }, ct);

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
            ResponsibleName = t.ItResponsible?.FullName,
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
        ItResponsibleName = t.ItResponsible?.FullName,
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
            Condition = d.Condition
        }).ToList(),
        Photos = t.Photos.Select(p => new ItTicketPhotoDto { Id = p.Id, ImageBase64 = p.ImageBase64 }).ToList(),
        Signatures = t.Signatures.Select(s => new ItTicketSignatureDto
        {
            SignerType = s.SignerType, SignerName = s.SignerName, ImageBase64 = s.ImageBase64, SignedAt = s.SignedAt
        }).ToList()
    };
}
