using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public class ItCatalogService : IItCatalogService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public ItCatalogService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<ItCatalogsDto> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var types = await _uow.ItAssets.GetTypesAsync(cancellationToken);
        var brands = await _uow.ItAssets.GetBrandsAsync(cancellationToken);
        var locations = await _uow.ItAssets.GetLocationsAsync(cancellationToken);
        var departments = await _uow.ItAssets.GetDepartmentsAsync(cancellationToken);

        return new ItCatalogsDto
        {
            Types = types.Select(t => new ItAssetTypeDto
            {
                Id = t.Id, Name = t.Name, Code = t.Code, RequiresSerial = t.RequiresSerial,
                IsAssignable = t.IsAssignable, HasComputeSpecs = t.HasComputeSpecs, IconName = t.IconName
            }),
            Brands = brands.Select(b => new ItCatalogItemDto { Id = b.Id, Name = b.Name }),
            Locations = locations.Select(l => new ItCatalogItemDto { Id = l.Id, Name = l.Name, Code = l.Code }),
            Departments = departments.Select(d => new ItCatalogItemDto { Id = d.Id, Name = d.Name, Code = d.Code })
        };
    }

    public async Task<ItCatalogItemDto> CreateBrandAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<ItBrand>();
        if (await repo.AnyAsync(b => b.Name == name, cancellationToken))
            throw new InvalidOperationException("Ya existe una marca con ese nombre.");

        var brand = new ItBrand { Name = name, CreatedBy = createdBy };
        await repo.AddAsync(brand, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_BRAND_CREATED", entityName: "ItBrand", entityId: brand.Id.ToString(), module: "TI", cancellationToken: cancellationToken);
        return new ItCatalogItemDto { Id = brand.Id, Name = brand.Name };
    }

    public async Task<ItCatalogItemDto> CreateLocationAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<ItLocation>();
        var location = new ItLocation { Name = name, Code = dto.Code?.Trim(), CreatedBy = createdBy };
        await repo.AddAsync(location, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_LOCATION_CREATED", entityName: "ItLocation", entityId: location.Id.ToString(), module: "TI", cancellationToken: cancellationToken);
        return new ItCatalogItemDto { Id = location.Id, Name = location.Name, Code = location.Code };
    }

    public async Task<ItCatalogItemDto> CreateDepartmentAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<Department>();
        var dept = new Department { Name = name, Code = dto.Code?.Trim(), CreatedBy = createdBy };
        await repo.AddAsync(dept, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_DEPARTMENT_CREATED", entityName: "Department", entityId: dept.Id.ToString(), module: "TI", cancellationToken: cancellationToken);
        return new ItCatalogItemDto { Id = dept.Id, Name = dept.Name, Code = dept.Code };
    }

    private static string Require(string? name)
    {
        var n = name?.Trim();
        if (string.IsNullOrWhiteSpace(n)) throw new InvalidOperationException("El nombre es obligatorio.");
        return n;
    }
}
