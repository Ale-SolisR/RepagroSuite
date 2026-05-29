using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public class ItAssetService : IItAssetService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IItExcelExporter _excel;
    private readonly IPdfGenerator _pdf;

    public ItAssetService(IUnitOfWork uow, IAuditService audit, IItExcelExporter excel, IPdfGenerator pdf)
    {
        _uow = uow;
        _audit = audit;
        _excel = excel;
        _pdf = pdf;
    }

    public async Task<PagedResult<ItAssetListDto>> GetPagedAsync(int page, int pageSize, string? search,
        ItAssetStatus? status, Guid? assetTypeId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.ItAssets.GetPagedAsync(page, pageSize, search, status, assetTypeId, departmentId, cancellationToken);
        return new PagedResult<ItAssetListDto>
        {
            Items = items.Select(MapToListDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ItAssetDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _uow.ItAssets.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Activo no encontrado.");
        return MapToDto(asset);
    }

    public async Task<IEnumerable<ItAssetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var history = await _uow.ItAssets.GetHistoryAsync(id, cancellationToken);
        return history.Select(h => new ItAssetHistoryDto
        {
            Id = h.Id,
            EventType = h.EventType,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            Description = h.Description,
            OccurredAt = h.OccurredAt
        });
    }

    public async Task<ItAssetDto> CreateAsync(CreateItAssetDto dto, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var code = dto.InternalCode.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("El código interno es obligatorio.");
        if (await _uow.ItAssets.InternalCodeExistsAsync(code, null, cancellationToken))
            throw new InvalidOperationException("Ya existe un activo con ese código interno.");

        var type = await _uow.Repository<ItAssetType>().GetByIdAsync(dto.AssetTypeId, cancellationToken)
            ?? throw new InvalidOperationException("Tipo de activo inválido.");

        var serial = string.IsNullOrWhiteSpace(dto.SerialNumber) ? null : dto.SerialNumber.Trim();
        if (type.RequiresSerial && serial is null)
            throw new InvalidOperationException($"El tipo «{type.Name}» exige número de serie.");
        if (serial is not null && await _uow.ItAssets.SerialExistsAsync(serial, null, cancellationToken))
            throw new InvalidOperationException("Ya existe un activo con ese número de serie.");

        var asset = new ItAsset
        {
            InternalCode = code,
            AssetTypeId = dto.AssetTypeId,
            BrandId = dto.BrandId,
            Model = dto.Model?.Trim(),
            SerialNumber = serial,
            AssetTag = dto.AssetTag?.Trim(),
            Status = ItAssetStatus.Available,
            PhysicalCondition = dto.PhysicalCondition,
            LocationId = dto.LocationId,
            LocationDetail = dto.LocationDetail?.Trim(),
            DepartmentId = dto.DepartmentId,
            CurrentHolderEmployeeId = dto.CurrentHolderEmployeeId,
            PurchaseDate = dto.PurchaseDate,
            Supplier = dto.Supplier?.Trim(),
            Cost = dto.Cost,
            Currency = NormalizeCurrency(dto.Currency),
            HasWarranty = dto.HasWarranty,
            WarrantyEndDate = dto.WarrantyEndDate,
            Notes = dto.Notes?.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            CreatedBy = createdBy
        };

        if (dto.Spec is not null && HasAnySpec(dto.Spec))
            asset.Spec = MapSpec(dto.Spec, createdBy);

        asset.History.Add(new ItAssetHistory
        {
            AssetId = asset.Id,
            EventType = "CREATED",
            ToStatus = ItAssetStatus.Available,
            Description = "Activo registrado en el inventario.",
            PerformedBy = createdBy,
            CreatedBy = createdBy
        });

        await _uow.ItAssets.AddAsync(asset, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(createdBy, "TI_ASSET_CREATED", entityName: "ItAsset", entityId: asset.Id.ToString(),
            newValues: new { asset.InternalCode, asset.SerialNumber, asset.Status }, module: "TI",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(asset.Id, cancellationToken);
    }

    public async Task<ItAssetDto> UpdateAsync(Guid id, UpdateItAssetDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var asset = await _uow.ItAssets.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Activo no encontrado.");

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            try { _uow.ItAssets.SetOriginalRowVersion(asset, Convert.FromBase64String(dto.RowVersion)); }
            catch (FormatException) { throw new InvalidOperationException("Token de versión inválido. Recargue el activo e intente de nuevo."); }
        }

        var serial = string.IsNullOrWhiteSpace(dto.SerialNumber) ? null : dto.SerialNumber.Trim();
        if (serial is not null && await _uow.ItAssets.SerialExistsAsync(serial, id, cancellationToken))
            throw new InvalidOperationException("Ya existe otro activo con ese número de serie.");

        asset.BrandId = dto.BrandId;
        asset.AssetTypeId = dto.AssetTypeId;
        asset.Model = dto.Model?.Trim();
        asset.SerialNumber = serial;
        asset.AssetTag = dto.AssetTag?.Trim();
        asset.PhysicalCondition = dto.PhysicalCondition;
        asset.LocationId = dto.LocationId;
        asset.LocationDetail = dto.LocationDetail?.Trim();
        asset.DepartmentId = dto.DepartmentId;
        asset.CurrentHolderEmployeeId = dto.CurrentHolderEmployeeId;
        asset.PurchaseDate = dto.PurchaseDate;
        asset.Supplier = dto.Supplier?.Trim();
        asset.Cost = dto.Cost;
        asset.Currency = NormalizeCurrency(dto.Currency);
        asset.HasWarranty = dto.HasWarranty;
        asset.WarrantyEndDate = dto.WarrantyEndDate;
        asset.Notes = dto.Notes?.Trim();
        asset.ImageUrl = dto.ImageUrl?.Trim();
        asset.UpdatedBy = updatedBy;

        // Spec 1:1 — crea o actualiza
        if (dto.Spec is not null && HasAnySpec(dto.Spec))
        {
            asset.Spec ??= new ItAssetSpec { AssetId = asset.Id, CreatedBy = updatedBy };
            ApplySpec(asset.Spec, dto.Spec);
            asset.Spec.UpdatedBy = updatedBy;
        }

        _uow.ItAssets.Update(asset);
        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(updatedBy, "TI_ASSET_UPDATED", entityName: "ItAsset", entityId: id.ToString(), module: "TI",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ItAssetDto> ChangeStatusAsync(Guid id, ChangeItAssetStatusDto dto, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var asset = await _uow.ItAssets.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Activo no encontrado.");

        var from = asset.Status;
        if (!ItAsset.CanTransition(from, dto.Status))
            throw new InvalidOperationException($"Transición no permitida: {StatusName(from)} → {StatusName(dto.Status)}.");

        // Robo/pérdida exigen motivo (propuesta §3 / §18).
        if ((dto.Status is ItAssetStatus.Stolen or ItAssetStatus.Lost or ItAssetStatus.Disposed)
            && string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Este cambio de estado requiere un motivo.");

        asset.Status = dto.Status;
        asset.UpdatedBy = updatedBy;
        _uow.ItAssets.Update(asset);

        await _uow.Repository<ItAssetHistory>().AddAsync(new ItAssetHistory
        {
            AssetId = id,
            EventType = "STATUS_CHANGED",
            FromStatus = from,
            ToStatus = dto.Status,
            Description = dto.Reason?.Trim(),
            PerformedBy = updatedBy,
            CreatedBy = updatedBy
        }, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(updatedBy, $"TI_ASSET_STATUS_{dto.Status.ToString().ToUpperInvariant()}",
            entityName: "ItAsset", entityId: id.ToString(),
            oldValues: new { Status = from.ToString() }, newValues: new { Status = dto.Status.ToString(), dto.Reason },
            module: "TI", cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var asset = await _uow.ItAssets.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Activo no encontrado.");

        _uow.ItAssets.SoftDelete(asset, deletedBy);
        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(deletedBy, "TI_ASSET_DELETED", entityName: "ItAsset", entityId: id.ToString(), module: "TI",
            cancellationToken: cancellationToken);
    }

    public async Task<ItDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _uow.ItAssets.GetAllForDashboardAsync(cancellationToken);
        var now = BusinessClock.Now;
        var soon = now.AddDays(60);

        int total = assets.Count;
        int assigned = assets.Count(a => a.Status == ItAssetStatus.Assigned);
        int available = assets.Count(a => a.Status == ItAssetStatus.Available);
        int loaned = assets.Count(a => a.Status == ItAssetStatus.Loaned);
        int underRepair = assets.Count(a => a.Status == ItAssetStatus.UnderRepair);
        int underMaint = assets.Count(a => a.Status == ItAssetStatus.UnderMaintenance);
        int damaged = assets.Count(a => a.Status == ItAssetStatus.Damaged);
        int lost = assets.Count(a => a.Status == ItAssetStatus.Lost);
        int stolen = assets.Count(a => a.Status == ItAssetStatus.Stolen);
        int disposed = assets.Count(a => a.Status == ItAssetStatus.Disposed);

        int operable = assigned + available + loaned;           // base para tasa de asignación
        int active = total - disposed;                           // base "vivos" (no dados de baja)
        int incidents = underRepair + underMaint + damaged + lost + stolen;

        int withoutSerial = assets.Count(a => string.IsNullOrWhiteSpace(a.SerialNumber));
        int withoutTag = assets.Count(a => string.IsNullOrWhiteSpace(a.AssetTag));
        int withoutHolder = assets.Count(a => a.CurrentHolderEmployeeId == null);

        // Índice de calidad de datos: 3 campos clave por activo (serie, placa, responsable).
        int fieldsTotal = total * 3;
        int fieldsOk = (total - withoutSerial) + (total - withoutTag) + (total - withoutHolder);
        double dataQuality = fieldsTotal == 0 ? 0 : Math.Round(fieldsOk * 100.0 / fieldsTotal, 1);

        int warrantyActive = assets.Count(a => a.HasWarranty && a.WarrantyEndDate != null && a.WarrantyEndDate >= now);
        int warrantyExpired = assets.Count(a => a.HasWarranty && a.WarrantyEndDate != null && a.WarrantyEndDate < now);
        int withoutWarranty = total - warrantyActive - warrantyExpired;

        var withDate = assets.Where(a => a.PurchaseDate != null).ToList();
        double avgAgeMonths = withDate.Count == 0 ? 0
            : Math.Round(withDate.Average(a => (now - a.PurchaseDate!.Value).TotalDays / 30.44), 1);

        // Tendencia de adquisiciones: últimos 12 meses (cronológico).
        var trend = new List<ItTrendPointDto>();
        for (int i = 11; i >= 0; i--)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            int count = assets.Count(a => a.PurchaseDate != null
                && a.PurchaseDate.Value.Year == month.Year && a.PurchaseDate.Value.Month == month.Month);
            trend.Add(new ItTrendPointDto { Label = $"{MonthAbbr(month.Month)} {month:yy}", Count = count });
        }

        return new ItDashboardDto
        {
            TotalAssets = total,
            Assigned = assigned,
            Available = available,
            Loaned = loaned,
            UnderRepair = underRepair,
            UnderMaintenance = underMaint,
            Damaged = damaged,
            Lost = lost,
            Stolen = stolen,
            Disposed = disposed,

            // Costo sin moneda explícita se asume en colones (default Costa Rica).
            TotalCostCrc = assets.Where(a => a.Currency != "USD").Sum(a => a.Cost ?? 0),
            TotalCostUsd = assets.Where(a => a.Currency == "USD").Sum(a => a.Cost ?? 0),
            AssetsWithCost = assets.Count(a => a.Cost != null && a.Cost > 0),

            AssignmentRatePct = operable == 0 ? 0 : Math.Round(assigned * 100.0 / operable, 1),
            AvailabilityRatePct = active == 0 ? 0 : Math.Round(available * 100.0 / active, 1),
            DataQualityPct = dataQuality,
            IncidentRatePct = active == 0 ? 0 : Math.Round(incidents * 100.0 / active, 1),

            WithoutSerial = withoutSerial,
            WithoutTag = withoutTag,
            WithoutHolder = withoutHolder,
            WarrantyExpiringSoon = assets.Count(a => a.HasWarranty && a.WarrantyEndDate != null
                && a.WarrantyEndDate >= now && a.WarrantyEndDate <= soon),

            WarrantyActive = warrantyActive,
            WarrantyExpired = warrantyExpired,
            WithoutWarranty = withoutWarranty,

            AvgAgeMonths = avgAgeMonths,
            AssetsWithPurchaseDate = withDate.Count,

            ByType = assets.GroupBy(a => a.AssetType?.Name ?? "—")
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            ByStatus = assets.GroupBy(a => a.Status)
                .Select(g => new ItCountByLabelDto { Label = StatusName(g.Key), Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            ByDepartment = assets.GroupBy(a => a.Department?.Name ?? "Sin departamento")
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            ByBrand = assets.GroupBy(a => a.Brand?.Name ?? "Sin marca")
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(8).ToList(),
            ByLocation = assets.GroupBy(a => a.Location?.Name ?? "Sin ubicación")
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(8).ToList(),
            ByCondition = assets.GroupBy(a => ConditionName(a.PhysicalCondition))
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            ByAgeBucket = BuildAgeBuckets(withDate, now),
            TopHolders = assets.Where(a => a.Holder != null)
                .GroupBy(a => a.Holder!.FullName)
                .Select(g => new ItCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(8).ToList(),
            ValueByHolder = assets.Where(a => a.Holder != null && a.Cost != null && a.Cost > 0)
                .GroupBy(a => a.Holder!.FullName)
                .Select(g => new ItValueByLabelDto
                {
                    Label = g.Key,
                    Crc = g.Where(a => a.Currency != "USD").Sum(a => a.Cost ?? 0),
                    Usd = g.Where(a => a.Currency == "USD").Sum(a => a.Cost ?? 0),
                    AssetCount = g.Count(),
                    Breakdown = g.GroupBy(a => a.AssetType?.Name ?? "—")
                        .Select(tg => new ItValueBreakdownDto
                        {
                            Label = tg.Key,
                            Count = tg.Count(),
                            Crc = tg.Where(a => a.Currency != "USD").Sum(a => a.Cost ?? 0),
                            Usd = tg.Where(a => a.Currency == "USD").Sum(a => a.Cost ?? 0)
                        })
                        .OrderByDescending(x => x.Crc + x.Usd).ToList()
                })
                .OrderByDescending(x => x.Crc + x.Usd).Take(10).ToList(),
            AcquisitionTrend = trend,
            GeneratedAt = now.ToString("dd/MM/yyyy HH:mm")
        };
    }

    public async Task<byte[]> ExportInventoryExcelAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _uow.ItAssets.GetAllForExportAsync(cancellationToken);
        var dashboard = await GetDashboardAsync(cancellationToken);
        var rows = assets.Select(a => new ItAssetExportRow
        {
            InternalCode = a.InternalCode,
            AssetType = a.AssetType?.Name ?? "—",
            Brand = a.Brand?.Name,
            Model = a.Model,
            SerialNumber = a.SerialNumber,
            AssetTag = a.AssetTag,
            Status = StatusName(a.Status),
            Condition = ConditionName(a.PhysicalCondition),
            Location = a.Location?.Name,
            LocationDetail = a.LocationDetail,
            Department = a.Department?.Name,
            Holder = a.Holder?.FullName,
            PurchaseDate = a.PurchaseDate,
            Supplier = a.Supplier,
            Cost = a.Cost,
            Currency = a.Currency,
            Warranty = a.HasWarranty
                ? (a.WarrantyEndDate != null && a.WarrantyEndDate < BusinessClock.Now ? "Vencida" : "Vigente")
                : "Sin garantía",
            WarrantyEndDate = a.WarrantyEndDate,
            OperatingSystem = a.Spec?.OperatingSystem,
            Processor = a.Spec?.Processor,
            RamGb = a.Spec?.RamGb,
            DiskGb = a.Spec?.DiskGb,
            IpAddress = a.Spec?.IpAddress,
            AnyDeskId = a.Spec?.AnyDeskId,
            Microsoft365User = a.Spec?.Microsoft365User,
            Notes = a.Notes
        }).ToList();

        return _excel.ExportInventory(rows, dashboard);
    }

    public async Task<byte[]> GenerateDashboardPdfAsync(CancellationToken cancellationToken = default)
    {
        var dashboard = await GetDashboardAsync(cancellationToken);
        return _pdf.GenerateDashboardPdf(dashboard);
    }

    private static List<ItCountByLabelDto> BuildAgeBuckets(List<ItAsset> withDate, DateTime now)
    {
        var buckets = new[] { "< 1 año", "1–2 años", "2–3 años", "3–5 años", "5+ años" };
        var counts = new int[buckets.Length];
        foreach (var a in withDate)
        {
            double years = (now - a.PurchaseDate!.Value).TotalDays / 365.25;
            int idx = years < 1 ? 0 : years < 2 ? 1 : years < 3 ? 2 : years < 5 ? 3 : 4;
            counts[idx]++;
        }
        return buckets.Select((b, i) => new ItCountByLabelDto { Label = b, Count = counts[i] }).ToList();
    }

    private static string MonthAbbr(int m) => m switch
    {
        1 => "ene", 2 => "feb", 3 => "mar", 4 => "abr", 5 => "may", 6 => "jun",
        7 => "jul", 8 => "ago", 9 => "sep", 10 => "oct", 11 => "nov", 12 => "dic", _ => "—"
    };

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static string? NormalizeCurrency(string? c)
    {
        if (string.IsNullOrWhiteSpace(c)) return null;
        var u = c.Trim().ToUpperInvariant();
        return u is "CRC" or "USD" ? u : null;
    }

    private static bool HasAnySpec(ItAssetSpecDto s) =>
        s.OperatingSystem != null || s.Processor != null || s.RamGb != null || s.DiskGb != null ||
        s.MacEthernet != null || s.MacWifi != null || s.IpAddress != null || s.DomainName != null ||
        s.AnyDeskId != null || s.Microsoft365User != null || s.AntivirusStatus != null || s.TechNotes != null;

    private static ItAssetSpec MapSpec(ItAssetSpecDto s, Guid by)
    {
        var spec = new ItAssetSpec { CreatedBy = by };
        ApplySpec(spec, s);
        return spec;
    }

    private static void ApplySpec(ItAssetSpec spec, ItAssetSpecDto s)
    {
        spec.OperatingSystem = s.OperatingSystem?.Trim();
        spec.Processor = s.Processor?.Trim();
        spec.RamGb = s.RamGb;
        spec.DiskGb = s.DiskGb;
        spec.MacEthernet = s.MacEthernet?.Trim();
        spec.MacWifi = s.MacWifi?.Trim();
        spec.IpAddress = s.IpAddress?.Trim();
        spec.DomainName = s.DomainName?.Trim();
        spec.AnyDeskId = s.AnyDeskId?.Trim();
        spec.Microsoft365User = s.Microsoft365User?.Trim();
        spec.AntivirusStatus = s.AntivirusStatus?.Trim();
        spec.TechNotes = s.TechNotes?.Trim();
    }

    internal static string StatusName(ItAssetStatus s) => s switch
    {
        ItAssetStatus.Available => "Disponible",
        ItAssetStatus.Assigned => "Asignado",
        ItAssetStatus.Loaned => "Prestado",
        ItAssetStatus.UnderReview => "En revisión",
        ItAssetStatus.UnderMaintenance => "En mantenimiento",
        ItAssetStatus.UnderRepair => "En reparación",
        ItAssetStatus.Returned => "Devuelto",
        ItAssetStatus.Damaged => "Dañado",
        ItAssetStatus.Lost => "Perdido",
        ItAssetStatus.Stolen => "Robado",
        ItAssetStatus.Disposed => "Dado de baja",
        ItAssetStatus.Inactive => "Inactivo",
        _ => s.ToString()
    };

    private static string ConditionName(PhysicalCondition c) => c switch
    {
        PhysicalCondition.New => "Nuevo",
        PhysicalCondition.Good => "Bueno",
        PhysicalCondition.Fair => "Regular",
        PhysicalCondition.Poor => "Malo",
        PhysicalCondition.Unusable => "Inservible",
        _ => c.ToString()
    };

    private static ItAssetListDto MapToListDto(ItAsset a) => new()
    {
        Id = a.Id,
        InternalCode = a.InternalCode,
        AssetTypeName = a.AssetType?.Name ?? "—",
        BrandName = a.Brand?.Name,
        Model = a.Model,
        SerialNumber = a.SerialNumber,
        Status = a.Status,
        StatusName = StatusName(a.Status),
        LocationName = a.Location?.Name,
        DepartmentName = a.Department?.Name,
        CurrentHolderName = a.Holder?.FullName,
        ImageUrl = a.ImageUrl
    };

    private static ItAssetDto MapToDto(ItAsset a) => new()
    {
        Id = a.Id,
        InternalCode = a.InternalCode,
        AssetTypeId = a.AssetTypeId,
        AssetTypeName = a.AssetType?.Name ?? "—",
        BrandId = a.BrandId,
        BrandName = a.Brand?.Name,
        Model = a.Model,
        SerialNumber = a.SerialNumber,
        AssetTag = a.AssetTag,
        Status = a.Status,
        StatusName = StatusName(a.Status),
        PhysicalCondition = a.PhysicalCondition,
        PhysicalConditionName = ConditionName(a.PhysicalCondition),
        LocationId = a.LocationId,
        LocationName = a.Location?.Name,
        LocationDetail = a.LocationDetail,
        DepartmentId = a.DepartmentId,
        DepartmentName = a.Department?.Name,
        CurrentHolderEmployeeId = a.CurrentHolderEmployeeId,
        CurrentHolderName = a.Holder?.FullName,
        PurchaseDate = a.PurchaseDate,
        Supplier = a.Supplier,
        Cost = a.Cost,
        Currency = a.Currency,
        HasWarranty = a.HasWarranty,
        WarrantyEndDate = a.WarrantyEndDate,
        Notes = a.Notes,
        ImageUrl = a.ImageUrl,
        CreatedAt = a.CreatedAt,
        RowVersion = a.RowVersion is { Length: > 0 } ? Convert.ToBase64String(a.RowVersion) : null,
        Spec = a.Spec is null ? null : new ItAssetSpecDto
        {
            OperatingSystem = a.Spec.OperatingSystem,
            Processor = a.Spec.Processor,
            RamGb = a.Spec.RamGb,
            DiskGb = a.Spec.DiskGb,
            MacEthernet = a.Spec.MacEthernet,
            MacWifi = a.Spec.MacWifi,
            IpAddress = a.Spec.IpAddress,
            DomainName = a.Spec.DomainName,
            AnyDeskId = a.Spec.AnyDeskId,
            Microsoft365User = a.Spec.Microsoft365User,
            AntivirusStatus = a.Spec.AntivirusStatus,
            TechNotes = a.Spec.TechNotes
        }
    };
}
