namespace RepagroSuite.Application.Common.Interfaces;

// Cache en memoria con TTL e invalidación por clave.
// Para alta carga real (multi-réplica) migrar a IDistributedCache (Redis).
public interface IAppCache
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default);
    void Remove(string key);
}

public static class CacheKeys
{
    public const string ActiveFeatures = "rooms:features:active";
    public const string SystemSettingsAll = "settings:all";
    public const string SystemSettingsByModule = "settings:module:";
    public const string SmtpConfig = "email:smtp-config";
    public static string RoomsByPage(int page, int pageSize, string? search, string? status)
        => $"rooms:paged:{page}:{pageSize}:{search ?? "_"}:{status ?? "_"}";
}
