using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.ITAssets.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.ITAssets.Services;

public class ItImportService : IItImportService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public ItImportService(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    private static readonly Dictionary<string, string> BrandMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LEN"] = "Lenovo", ["DELL"] = "Dell", ["HP"] = "HP", ["MSI"] = "MSI", ["HONOR"] = "Honor",
    };

    private static string TypeCodeFor(string? device)
    {
        var d = (device ?? "").Trim().ToLowerInvariant();
        if (d.Contains("laptop")) return "LAPTOP";
        if (d.Contains("escritorio") || d.Contains("desktop")) return "DESKTOP";
        if (d.Contains("tablet")) return "TABLET";
        return "OTHER";
    }

    public async Task<ItImportResultDto> ImportAssetsAsync(IEnumerable<ItAssetImportRow> rows, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var result = new ItImportResultDto();

        // Cargar catálogos y códigos existentes una vez.
        var types = (await _uow.ItAssets.GetTypesAsync(cancellationToken)).ToDictionary(t => t.Code, t => t.Id, StringComparer.OrdinalIgnoreCase);
        var brands = (await _uow.ItAssets.GetBrandsAsync(cancellationToken)).ToDictionary(b => b.Name, b => b, StringComparer.OrdinalIgnoreCase);
        var locations = (await _uow.ItAssets.GetLocationsAsync(cancellationToken)).ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);
        var depts = (await _uow.ItAssets.GetDepartmentsAsync(cancellationToken)).ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

        var existingCodes = new HashSet<string>(
            await _uow.ItAssets.GetAllInternalCodesAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var batchCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        async Task<ItBrand?> ResolveBrandAsync(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var name = BrandMap.TryGetValue(raw.Trim(), out var mapped) ? mapped : Capitalize(raw.Trim());
            if (brands.TryGetValue(name, out var b)) return b;
            b = new ItBrand { Name = name, CreatedBy = actorUserId };
            await _uow.Repository<ItBrand>().AddAsync(b, cancellationToken);
            brands[name] = b;
            result.BrandsCreated++;
            return b;
        }

        async Task<ItLocation?> ResolveLocationAsync(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var name = raw.Trim();
            if (locations.TryGetValue(name, out var l)) return l;
            l = new ItLocation { Name = name, CreatedBy = actorUserId };
            await _uow.Repository<ItLocation>().AddAsync(l, cancellationToken);
            locations[name] = l;
            result.LocationsCreated++;
            return l;
        }

        async Task<Department?> ResolveDeptAsync(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var name = raw.Trim();
            if (depts.TryGetValue(name, out var d)) return d;
            d = new Department { Name = name, CreatedBy = actorUserId };
            await _uow.Repository<Department>().AddAsync(d, cancellationToken);
            depts[name] = d;
            result.DepartmentsCreated++;
            return d;
        }

        foreach (var row in rows)
        {
            var code = row.Codigo?.Trim();
            if (string.IsNullOrWhiteSpace(code)) { result.Warnings.Add("Fila sin código, omitida."); continue; }

            // Idempotencia + duplicados dentro del lote → sufijo.
            if (existingCodes.Contains(code)) { result.SkippedExisting++; continue; }
            var finalCode = code;
            if (batchCodes.Contains(finalCode))
            {
                var n = 2;
                while (batchCodes.Contains($"{code}-{n}") || existingCodes.Contains($"{code}-{n}")) n++;
                finalCode = $"{code}-{n}";
                result.Warnings.Add($"Código duplicado '{code}' renombrado a '{finalCode}'.");
            }

            var typeCode = TypeCodeFor(row.Dispositivo);
            if (!types.TryGetValue(typeCode, out var typeId))
                types.TryGetValue("OTHER", out typeId);

            var brand = await ResolveBrandAsync(row.Marca);
            var location = await ResolveLocationAsync(row.Ubicacion);
            var dept = await ResolveDeptAsync(row.Unidad);

            var robada = (row.Comentarios ?? "").ToUpperInvariant().Contains("ROBAD");

            var notes = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.Comentarios)) notes.Add(row.Comentarios!.Trim());
            if (!string.IsNullOrWhiteSpace(row.Responsable)) notes.Add($"Responsable (bitácora): {row.Responsable!.Trim()}");

            var asset = new ItAsset
            {
                InternalCode = finalCode,
                AssetTypeId = typeId,
                BrandId = brand?.Id,
                Model = row.Modelo?.Trim(),
                Status = robada ? ItAssetStatus.Stolen : ItAssetStatus.Available,
                PhysicalCondition = PhysicalCondition.Good,
                LocationId = location?.Id,
                LocationDetail = row.DetalleUbic?.Trim(),
                DepartmentId = dept?.Id,
                Notes = notes.Count > 0 ? string.Join(" · ", notes) : null,
                CreatedBy = actorUserId
            };

            var anyDesk = CleanAnyDesk(row.AnyDesk);
            if (anyDesk is not null || !string.IsNullOrWhiteSpace(row.Usuario365) || !string.IsNullOrWhiteSpace(row.Kaspersky))
            {
                asset.Spec = new ItAssetSpec
                {
                    AnyDeskId = anyDesk,
                    Microsoft365User = row.Usuario365?.Trim(),
                    AntivirusStatus = NormalizeFlag(row.Kaspersky),
                    CreatedBy = actorUserId
                };
            }

            asset.History.Add(new ItAssetHistory
            {
                AssetId = asset.Id, EventType = "CREATED", ToStatus = asset.Status,
                Description = "Importado desde la bitácora Excel.", PerformedBy = actorUserId, CreatedBy = actorUserId
            });

            await _uow.ItAssets.AddAsync(asset, cancellationToken);
            batchCodes.Add(finalCode);
            result.Created++;
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actorUserId, "TI_IMPORT_ASSETS", entityName: "ItAsset",
            newValues: new { result.Created, result.SkippedExisting }, module: "TI", cancellationToken: cancellationToken);
        return result;
    }

    private static string? CleanAnyDesk(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var t = raw.Trim();
        if (t.Equals("NO", StringComparison.OrdinalIgnoreCase) || t.Equals("ok", StringComparison.OrdinalIgnoreCase)) return null;
        var digits = new string(t.Where(char.IsDigit).ToArray());
        return digits.Length >= 6 ? digits : null;
    }

    private static string? NormalizeFlag(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var t = raw.Trim().ToUpperInvariant();
        return t is "OK" ? "OK" : t is "NA" or "N/A" ? "N/A" : raw.Trim();
    }

    private static string Capitalize(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
