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
        var normalized = new string((dto.IdentificationNumber ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("La cédula es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("El nombre es obligatorio.");
        if (await _uow.ItEmployees.GetByNormalizedIdAsync(normalized, cancellationToken) is not null)
            throw new InvalidOperationException("Ya existe un colaborador con esa cédula.");

        var employee = new ItEmployee
        {
            IdentificationType = normalized.Length > 10 ? IdentificationType.LegalEntityId : IdentificationType.PhysicalId,
            IdentificationNumber = dto.IdentificationNumber.Trim(),
            NormalizedIdentificationNumber = normalized,
            FullName = dto.FullName.Trim(),
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

        e.FullName = dto.FullName.Trim();
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
