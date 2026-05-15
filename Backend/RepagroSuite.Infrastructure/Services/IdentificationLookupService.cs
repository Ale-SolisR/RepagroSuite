using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.Identifications.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Infrastructure.Data;

namespace RepagroSuite.Infrastructure.Services;

public class IdentificationLookupService : IIdentificationLookupService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<IdentificationLookupService> _logger;

    public IdentificationLookupService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<IdentificationLookupService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<IdentificationLookupResultDto?> LookupAsync(string identificationNumber, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeNumber(identificationNumber);
        if (string.IsNullOrEmpty(normalized)) return null;

        var cacheEnabled = _config["IDENTIFICATION_CACHE_ENABLED"] != "false";
        var cacheDays = int.TryParse(_config["IDENTIFICATION_CACHE_DAYS"], out var d) ? d : 30;

        if (cacheEnabled)
        {
            var cached = await LookupFromCacheAsync(normalized, cancellationToken);
            if (cached != null) return cached;
        }

        return await LookupFromApiAsync(normalized, cacheEnabled, cacheDays, cancellationToken);
    }

    public async Task<IdentificationLookupResultDto?> LookupFromCacheAsync(string normalizedNumber, CancellationToken cancellationToken = default)
    {
        var cache = await _context.IdentificationLookupCaches
            .FirstOrDefaultAsync(c => c.NormalizedIdentificationNumber == normalizedNumber, cancellationToken);

        if (cache == null) return null;

        return MapFromCache(cache);
    }

    private async Task<IdentificationLookupResultDto?> LookupFromApiAsync(
        string normalized, bool cacheEnabled, int cacheDays, CancellationToken cancellationToken)
    {
        var baseUrl = _config["IDENTIFICATION_BASE_URL"] ?? "https://apis.gometa.org/cedulas/";
        var timeout = int.TryParse(_config["IDENTIFICATION_TIMEOUT_SECONDS"], out var t) ? t : 10;

        try
        {
            var client = _httpClientFactory.CreateClient("GoMeta");
            client.Timeout = TimeSpan.FromSeconds(timeout);

            var response = await client.GetAsync($"{baseUrl}{normalized}", cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = ParseGoMetaResponse(json, normalized);

            if (result != null && cacheEnabled)
                await SaveToCacheAsync(result, json, normalized, cacheDays, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando GoMeta para cédula {Number}", normalized);
            return null;
        }
    }

    private static IdentificationLookupResultDto? ParseGoMetaResponse(string json, string normalized)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("resultcount", out var countProp) || countProp.GetInt32() == 0) return null;
            if (!root.TryGetProperty("results", out var resultsProp) || resultsProp.ValueKind != JsonValueKind.Array) return null;
            if (resultsProp.GetArrayLength() == 0) return null;

            var first = resultsProp[0];
            var idType = DetectType(first, root);
            var isPhysical = idType == IdentificationType.PhysicalId;

            return new IdentificationLookupResultDto
            {
                IdentificationNumber = normalized,
                NormalizedIdentificationNumber = normalized,
                IdentificationType = idType,
                IdentificationTypeName = isPhysical ? "Cédula física" : "Cédula jurídica",
                FullName = GetString(first, root, ["fullname", "nombre"]),
                FirstName = isPhysical ? GetString(first, null, ["firstname"]) : null,
                FirstName1 = isPhysical ? GetString(first, null, ["firstname1"]) : null,
                FirstName2 = isPhysical ? GetString(first, null, ["firstname2"]) : null,
                LastName = isPhysical ? GetString(first, null, ["lastname"]) : null,
                LastName1 = isPhysical ? GetString(first, null, ["lastname1"]) : null,
                LastName2 = isPhysical ? GetString(first, null, ["lastname2"]) : null,
                LegalName = !isPhysical ? GetString(first, root, ["fullname", "nombre"]) : null,
                Source = "GoMeta",
                Found = true
            };
        }
        catch
        {
            return null;
        }
    }

    private static IdentificationType DetectType(JsonElement first, JsonElement root)
    {
        if (first.TryGetProperty("guess_type", out var gt) && gt.GetString() == "JURIDICA")
            return IdentificationType.LegalEntityId;
        if (first.TryGetProperty("type", out var tp) && tp.GetString() == "J")
            return IdentificationType.LegalEntityId;
        if (root.TryGetProperty("tipoIdentificacion", out var ti) && ti.GetString() == "02")
            return IdentificationType.LegalEntityId;
        return IdentificationType.PhysicalId;
    }

    private static string? GetString(JsonElement first, JsonElement? root, string[] keys)
    {
        foreach (var key in keys)
        {
            if (first.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString()?.Trim();
            if (root.HasValue && root.Value.TryGetProperty(key, out var rv) && rv.ValueKind == JsonValueKind.String)
                return rv.GetString()?.Trim();
        }
        return null;
    }

    private async Task SaveToCacheAsync(
        IdentificationLookupResultDto dto, string rawJson, string normalized, int cacheDays, CancellationToken ct)
    {
        var existing = await _context.IdentificationLookupCaches
            .FirstOrDefaultAsync(c => c.NormalizedIdentificationNumber == normalized, ct);

        if (existing != null)
        {
            existing.LastLookupAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.FullName = dto.FullName;
            existing.FirstName = dto.FirstName;
            existing.FirstName1 = dto.FirstName1;
            existing.FirstName2 = dto.FirstName2;
            existing.LastName = dto.LastName;
            existing.LastName1 = dto.LastName1;
            existing.LastName2 = dto.LastName2;
            existing.LegalName = dto.LegalName;
        }
        else
        {
            _context.IdentificationLookupCaches.Add(new IdentificationLookupCache
            {
                IdentificationType = dto.IdentificationType,
                IdentificationNumber = dto.IdentificationNumber,
                NormalizedIdentificationNumber = normalized,
                FullName = dto.FullName,
                FirstName = dto.FirstName,
                FirstName1 = dto.FirstName1,
                FirstName2 = dto.FirstName2,
                LastName = dto.LastName,
                LastName1 = dto.LastName1,
                LastName2 = dto.LastName2,
                LegalName = dto.LegalName,
                Source = "GoMeta",
                RawResponseJson = rawJson,
                ResultCount = 1,
                LastLookupAt = DateTime.UtcNow
            });
        }

        try { await _context.SaveChangesAsync(ct); } catch { /* non-critical */ }
    }

    private static IdentificationLookupResultDto MapFromCache(IdentificationLookupCache cache)
    {
        var isPhysical = cache.IdentificationType == IdentificationType.PhysicalId;
        return new IdentificationLookupResultDto
        {
            IdentificationNumber = cache.IdentificationNumber,
            NormalizedIdentificationNumber = cache.NormalizedIdentificationNumber,
            IdentificationType = cache.IdentificationType,
            IdentificationTypeName = isPhysical ? "Cédula física" : "Cédula jurídica",
            FullName = cache.FullName,
            FirstName = cache.FirstName,
            FirstName1 = cache.FirstName1,
            FirstName2 = cache.FirstName2,
            LastName = cache.LastName,
            LastName1 = cache.LastName1,
            LastName2 = cache.LastName2,
            LegalName = cache.LegalName,
            Source = "Cache",
            Found = true
        };
    }

    private static string NormalizeNumber(string raw)
        => new string(raw.Where(char.IsDigit).ToArray());
}
