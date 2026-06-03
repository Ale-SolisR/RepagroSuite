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
        var suppliers = await _uow.ItAssets.GetSuppliersAsync(cancellationToken);

        return new ItCatalogsDto
        {
            Types = types.Select(t => new ItAssetTypeDto
            {
                Id = t.Id, Name = t.Name, Code = t.Code, RequiresSerial = t.RequiresSerial,
                IsAssignable = t.IsAssignable, HasComputeSpecs = t.HasComputeSpecs, IconName = t.IconName
            }),
            Brands = brands.Select(b => new ItCatalogItemDto { Id = b.Id, Name = b.Name }),
            Locations = locations.Select(l => new ItCatalogItemDto { Id = l.Id, Name = l.Name, Code = l.Code }),
            Departments = departments.Select(d => new ItCatalogItemDto { Id = d.Id, Name = d.Name, Code = d.Code }),
            Suppliers = suppliers.Select(s => new ItCatalogItemDto { Id = s.Id, Name = s.Name })
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
        if (await repo.AnyAsync(d => d.Name == name, cancellationToken))
            throw new InvalidOperationException("Ya existe un departamento con ese nombre.");

        var dept = new Department { Name = name, Code = dto.Code?.Trim(), CreatedBy = createdBy };
        await repo.AddAsync(dept, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_DEPARTMENT_CREATED", entityName: "Department", entityId: dept.Id.ToString(),
            newValues: new { dept.Name, dept.Code }, module: "TI", cancellationToken: cancellationToken);
        return new ItCatalogItemDto { Id = dept.Id, Name = dept.Name, Code = dept.Code };
    }

    public async Task<IEnumerable<ItDepartmentDto>> GetDepartmentsAdminAsync(string? search, CancellationToken cancellationToken = default)
    {
        var depts = await _uow.ItAssets.GetAllDepartmentsAsync(search, cancellationToken);
        var counts = await _uow.ItAssets.GetDepartmentAssetCountsAsync(cancellationToken);
        return depts.Select(d => new ItDepartmentDto
        {
            Id = d.Id, Name = d.Name, Code = d.Code, IsActive = d.IsActive,
            AssetCount = counts.TryGetValue(d.Id, out var c) ? c : 0
        });
    }

    public async Task<ItDepartmentDto> UpdateDepartmentAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<Department>();
        var dept = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Departamento no encontrado.");
        if (await repo.AnyAsync(d => d.Name == name && d.Id != id, cancellationToken))
            throw new InvalidOperationException("Ya existe un departamento con ese nombre.");

        dept.Name = name;
        dept.Code = dto.Code?.Trim();
        dept.UpdatedBy = updatedBy;
        repo.Update(dept);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, "TI_DEPARTMENT_UPDATED", entityName: "Department", entityId: id.ToString(),
            newValues: new { dept.Name, dept.Code }, module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.DepartmentId == id, cancellationToken);
        return new ItDepartmentDto { Id = dept.Id, Name = dept.Name, Code = dept.Code, IsActive = dept.IsActive, AssetCount = count };
    }

    public async Task<ItDepartmentDto> SetDepartmentStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var repo = _uow.Repository<Department>();
        var dept = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Departamento no encontrado.");

        dept.IsActive = isActive;
        dept.UpdatedBy = updatedBy;
        repo.Update(dept);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, isActive ? "TI_DEPARTMENT_ACTIVATED" : "TI_DEPARTMENT_DEACTIVATED",
            entityName: "Department", entityId: id.ToString(), newValues: new { dept.Name, dept.IsActive },
            module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.DepartmentId == id, cancellationToken);
        return new ItDepartmentDto { Id = dept.Id, Name = dept.Name, Code = dept.Code, IsActive = dept.IsActive, AssetCount = count };
    }

    public async Task<ItCatalogItemDto> CreateSupplierAsync(CreateCatalogItemDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<Supplier>();
        if (await repo.AnyAsync(s => s.Name == name, cancellationToken))
            throw new InvalidOperationException("Ya existe un proveedor con ese nombre.");

        var supplier = new Supplier { Name = name, CreatedBy = createdBy };
        await repo.AddAsync(supplier, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(createdBy, "TI_SUPPLIER_CREATED", entityName: "Supplier", entityId: supplier.Id.ToString(),
            newValues: new { supplier.Name }, module: "TI", cancellationToken: cancellationToken);
        return new ItCatalogItemDto { Id = supplier.Id, Name = supplier.Name };
    }

    public async Task<IEnumerable<ItSupplierDto>> GetSuppliersAdminAsync(string? search, CancellationToken cancellationToken = default)
    {
        var suppliers = await _uow.ItAssets.GetAllSuppliersAsync(search, cancellationToken);
        var counts = await _uow.ItAssets.GetSupplierAssetCountsAsync(cancellationToken);
        return suppliers.Select(s => new ItSupplierDto
        {
            Id = s.Id, Name = s.Name, IsActive = s.IsActive,
            AssetCount = counts.TryGetValue(s.Id, out var c) ? c : 0
        });
    }

    public async Task<ItSupplierDto> UpdateSupplierAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<Supplier>();
        var supplier = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Proveedor no encontrado.");
        if (await repo.AnyAsync(s => s.Name == name && s.Id != id, cancellationToken))
            throw new InvalidOperationException("Ya existe un proveedor con ese nombre.");

        supplier.Name = name;
        supplier.UpdatedBy = updatedBy;
        repo.Update(supplier);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, "TI_SUPPLIER_UPDATED", entityName: "Supplier", entityId: id.ToString(),
            newValues: new { supplier.Name }, module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.SupplierId == id, cancellationToken);
        return new ItSupplierDto { Id = supplier.Id, Name = supplier.Name, IsActive = supplier.IsActive, AssetCount = count };
    }

    public async Task<ItSupplierDto> SetSupplierStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var repo = _uow.Repository<Supplier>();
        var supplier = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Proveedor no encontrado.");

        supplier.IsActive = isActive;
        supplier.UpdatedBy = updatedBy;
        repo.Update(supplier);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, isActive ? "TI_SUPPLIER_ACTIVATED" : "TI_SUPPLIER_DEACTIVATED",
            entityName: "Supplier", entityId: id.ToString(), newValues: new { supplier.Name, supplier.IsActive },
            module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.SupplierId == id, cancellationToken);
        return new ItSupplierDto { Id = supplier.Id, Name = supplier.Name, IsActive = supplier.IsActive, AssetCount = count };
    }

    public async Task<IEnumerable<ItBrandDto>> GetBrandsAdminAsync(string? search, CancellationToken cancellationToken = default)
    {
        var brands = await _uow.ItAssets.GetAllBrandsAsync(search, cancellationToken);
        var counts = await _uow.ItAssets.GetBrandAssetCountsAsync(cancellationToken);
        return brands.Select(b => new ItBrandDto
        {
            Id = b.Id, Name = b.Name, IsActive = b.IsActive,
            AssetCount = counts.TryGetValue(b.Id, out var c) ? c : 0
        });
    }

    public async Task<ItBrandDto> UpdateBrandAsync(Guid id, CreateCatalogItemDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var name = Require(dto.Name);
        var repo = _uow.Repository<ItBrand>();
        var brand = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Marca no encontrada.");
        if (await repo.AnyAsync(b => b.Name == name && b.Id != id, cancellationToken))
            throw new InvalidOperationException("Ya existe una marca con ese nombre.");

        brand.Name = name;
        brand.UpdatedBy = updatedBy;
        repo.Update(brand);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, "TI_BRAND_UPDATED", entityName: "ItBrand", entityId: id.ToString(),
            newValues: new { brand.Name }, module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.BrandId == id, cancellationToken);
        return new ItBrandDto { Id = brand.Id, Name = brand.Name, IsActive = brand.IsActive, AssetCount = count };
    }

    public async Task<ItBrandDto> SetBrandStatusAsync(Guid id, bool isActive, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var repo = _uow.Repository<ItBrand>();
        var brand = await repo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Marca no encontrada.");

        brand.IsActive = isActive;
        brand.UpdatedBy = updatedBy;
        repo.Update(brand);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(updatedBy, isActive ? "TI_BRAND_ACTIVATED" : "TI_BRAND_DEACTIVATED",
            entityName: "ItBrand", entityId: id.ToString(), newValues: new { brand.Name, brand.IsActive },
            module: "TI", cancellationToken: cancellationToken);

        var count = await _uow.Repository<ItAsset>().CountAsync(a => a.BrandId == id, cancellationToken);
        return new ItBrandDto { Id = brand.Id, Name = brand.Name, IsActive = brand.IsActive, AssetCount = count };
    }

    private static string Require(string? name)
    {
        var n = name?.Trim();
        if (string.IsNullOrWhiteSpace(n)) throw new InvalidOperationException("El nombre es obligatorio.");
        return n;
    }
}
