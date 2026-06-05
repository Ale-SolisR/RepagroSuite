using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public class ItEmployeeService : IItEmployeeService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public ItEmployeeService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<PagedResult<ItEmployeeDto>> GetPagedAsync(int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.ItEmployees.GetPagedAsync(page, pageSize, search, activeOnly, cancellationToken);
        return new PagedResult<ItEmployeeDto>
        {
            Items = items.Select(Map),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<ItEmployeeDto>> GetActiveAsync(CancellationToken cancellationToken = default)
        => (await _uow.ItEmployees.GetActiveAsync(cancellationToken)).Select(Map);

    public async Task<ItEmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _uow.ItEmployees.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Colaborador no encontrado.");
        return Map(e);
    }

    public async Task<ItEmployeeDto> CreateAsync(CreateItEmployeeDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var identificationNumber = dto.IdentificationNumber?.Trim() ?? "";
        var fullName = dto.FullName?.Trim() ?? "";
        var normalized = new string(identificationNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("La cédula es obligatoria.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("El nombre es obligatorio.");
        if (await _uow.ItEmployees.GetByNormalizedIdAsync(normalized, cancellationToken) is not null)
            throw new InvalidOperationException("Ya existe un colaborador con esa cédula.");

        var employee = new ItEmployee
        {
            IdentificationType = normalized.Length > 10 ? IdentificationType.LegalEntityId : IdentificationType.PhysicalId,
            IdentificationNumber = identificationNumber,
            NormalizedIdentificationNumber = normalized,
            FullName = fullName,
            Position = dto.Position?.Trim(),
            Department = dto.Department?.Trim(),
            Email = dto.Email?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedBy = createdBy
        };

        await _uow.ItEmployees.AddAsync(employee, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_EMPLOYEE_CREATED", entityName: "ItEmployee", entityId: employee.Id.ToString(),
            newValues: new { employee.IdentificationNumber, employee.FullName }, module: "TI", cancellationToken: cancellationToken);
        return Map(employee);
    }

    public async Task<ItEmployeeDto> UpdateAsync(Guid id, UpdateItEmployeeDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var e = await _uow.ItEmployees.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Colaborador no encontrado.");

        var identificationNumber = dto.IdentificationNumber?.Trim() ?? "";
        var fullName = dto.FullName?.Trim() ?? "";
        var normalized = new string(identificationNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("La cédula es obligatoria.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new InvalidOperationException("El nombre es obligatorio.");
        var duplicate = await _uow.ItEmployees.GetByNormalizedIdAsync(normalized, cancellationToken);
        if (duplicate is not null && duplicate.Id != id)
            throw new InvalidOperationException("Ya existe un colaborador con esa cédula.");

        e.IdentificationType = normalized.Length > 10 ? IdentificationType.LegalEntityId : IdentificationType.PhysicalId;
        e.IdentificationNumber = identificationNumber;
        e.NormalizedIdentificationNumber = normalized;
        e.FullName = fullName;
        e.Position = dto.Position?.Trim();
        e.Department = dto.Department?.Trim();
        e.Email = dto.Email?.Trim();
        e.PhoneNumber = dto.PhoneNumber?.Trim();
        e.IsActive = dto.IsActive;
        e.UpdatedBy = updatedBy;
        _uow.ItEmployees.Update(e);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, "TI_EMPLOYEE_UPDATED", entityName: "ItEmployee", entityId: id.ToString(), module: "TI", cancellationToken: cancellationToken);
        return Map(e);
    }

    public async Task<ItEmployeeHistoryDto> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _uow.ItEmployees.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Colaborador no encontrado.");

        var assignments = await _uow.ItEmployees.GetAssignmentsWithDetailsAsync(id, cancellationToken);
        var tickets = await _uow.ItEmployees.GetTicketsAsync(id, cancellationToken);

        var mapped = assignments
            .Where(a => a.AssignedTicket?.Status != ItTicketStatus.Anulada)
            .Select(a => new ItEmployeeAssignmentDto
        {
            Id = a.Id,
            AssetId = a.AssetId,
            AssetCode = a.Asset?.InternalCode ?? "—",
            AssetTypeName = a.Asset?.AssetType?.Name,
            AssetModel = a.Asset?.Model,
            AssetStatus = a.Asset?.Status ?? ItAssetStatus.Available,
            AssetStatusName = ItAssetService.StatusName(a.Asset?.Status ?? ItAssetStatus.Available),
            IsActive = a.Status == AssignmentStatus.Activa,
            AssignedAt = a.AssignedAt,
            ReturnedAt = a.ReturnedAt,
            ConditionOutName = ConditionName(a.ConditionOut),
            ConditionInName = a.ConditionIn.HasValue ? ConditionName(a.ConditionIn.Value) : null,
            ClosedReason = a.ClosedReason,
            AssignedTicketId = a.AssignedTicketId,
            AssignedTicketNumber = a.AssignedTicket?.TicketNumber,
            ClosingTicketId = a.ReturnTicketId,
            ClosingTicketNumber = a.ReturnTicket?.TicketNumber,
            Notes = a.ReturnNotes ?? a.Accessories,
            AssetPhotos = (a.Asset?.Photos ?? new List<ItAssetPhoto>())
                .Where(p => p.Content.Length > 0)
                .OrderBy(p => p.SortOrder)
                .Take(3)
                .Select(p => $"data:{(string.IsNullOrEmpty(p.MimeType) ? "image/jpeg" : p.MimeType)};base64,{Convert.ToBase64String(p.Content)}")
                .ToList()
        }).ToList();

        var current = mapped.Where(m => m.IsActive).ToList();
        var past = mapped.Where(m => !m.IsActive).ToList();

        return new ItEmployeeHistoryDto
        {
            Employee = Map(employee),
            CurrentAssetsCount = current.Count,
            PastAssetsCount = past.Count,
            CurrentAssignments = current,
            PastAssignments = past,
            Tickets = tickets.Select(t => new ItEmployeeTicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                TicketType = t.TicketType,
                TicketTypeName = ItTicketService.TicketTypeName(t.TicketType),
                Status = t.Status,
                StatusName = t.Status.ToString(),
                IssuedAt = t.IssuedAt,
                AssetCount = t.Details?.Count ?? 0,
                HasPdf = !string.IsNullOrEmpty(t.PdfBase64)
            }).ToList()
        };
    }

    private static string ConditionName(PhysicalCondition c) => c switch
    {
        PhysicalCondition.New => "Nuevo",
        PhysicalCondition.Good => "Bueno",
        PhysicalCondition.Fair => "Regular",
        PhysicalCondition.Poor => "Malo",
        PhysicalCondition.Unusable => "Inservible",
        _ => c.ToString()
    };

    private static ItEmployeeDto Map(ItEmployee e) => new()
    {
        Id = e.Id,
        IdentificationNumber = e.IdentificationNumber,
        FullName = e.FullName,
        Position = e.Position,
        Department = e.Department,
        Email = e.Email,
        PhoneNumber = e.PhoneNumber,
        IsActive = e.IsActive
    };
}
