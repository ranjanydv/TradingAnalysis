using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TradingAnalytics.Shared.Infrastructure.Cache;

/// <summary>
/// Implements cache access using <see cref="IDistributedCache"/>.
/// </summary>
public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct).ConfigureAwait(false);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await _cache.SetAsync(
            key,
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default) => _cache.RemoveAsync(key, ct);

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory().ConfigureAwait(false);
        await SetAsync(key, value, ttl, ct).ConfigureAwait(false);
        return value;
    }
}
