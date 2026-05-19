using Microsoft.Extensions.Caching.Memory;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

// Implementación in-memory de IAppCache.
// Usa SemaphoreSlim por clave para evitar el problema del "stampede" (varios requests
// pegándole a la BD a la vez cuando la entrada expira).
public class MemoryAppCache : IAppCache
{
    private readonly IMemoryCache _cache;
    private static readonly Dictionary<string, SemaphoreSlim> _locks = new();
    private static readonly object _locksRoot = new();

    public MemoryAppCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(key, out T? hit) && hit is not null)
            return hit;

        var sem = GetLock(key);
        await sem.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(key, out T? second) && second is not null)
                return second;

            var value = await factory(ct);
            _cache.Set(key, value, ttl);
            return value;
        }
        finally
        {
            sem.Release();
        }
    }

    public void Remove(string key) => _cache.Remove(key);

    private static SemaphoreSlim GetLock(string key)
    {
        lock (_locksRoot)
        {
            if (!_locks.TryGetValue(key, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                _locks[key] = sem;
            }
            return sem;
        }
    }
}
