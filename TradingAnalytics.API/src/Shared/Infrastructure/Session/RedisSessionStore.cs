using System.Text.Json;
using StackExchange.Redis;

namespace TradingAnalytics.Shared.Infrastructure.Session;

/// <summary>
/// Implements session storage using Redis.
/// </summary>
public sealed class RedisSessionStore(IConnectionMultiplexer redis) : ISessionStore
{
    private const string UserSessionsSetPrefix = "user_sessions:";
    private readonly IDatabase _database = (redis ?? throw new ArgumentNullException(nameof(redis))).GetDatabase();

    /// <inheritdoc />
    public async Task SetAsync(string token, SessionData data, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = SessionKey(token);
        var json = JsonSerializer.Serialize(data);

        await _database.StringSetAsync(key, json, ttl).ConfigureAwait(false);
        await _database.SetAddAsync(UserSetKey(data.UserId), key).ConfigureAwait(false);
        await _database.KeyExpireAsync(UserSetKey(data.UserId), TimeSpan.FromDays(35)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionData?> GetAsync(string token, CancellationToken ct = default)
    {
        var value = await _database.StringGetAsync(SessionKey(token)).ConfigureAwait(false);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SessionData>(value.ToString());
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string token, CancellationToken ct = default)
    {
        var data = await GetAsync(token, ct).ConfigureAwait(false);
        await _database.KeyDeleteAsync(SessionKey(token)).ConfigureAwait(false);

        if (data is not null)
        {
            await _database.SetRemoveAsync(UserSetKey(data.UserId), SessionKey(token)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var members = await _database.SetMembersAsync(UserSetKey(userId)).ConfigureAwait(false);
        if (members.Length == 0)
        {
            return;
        }

        var keys = members.Select(static x => (RedisKey)(string)x!).ToArray();
        await _database.KeyDeleteAsync(keys).ConfigureAwait(false);
        await _database.KeyDeleteAsync(UserSetKey(userId)).ConfigureAwait(false);
    }

    private static string SessionKey(string token) => $"session:{token}";

    private static string UserSetKey(Guid userId) => $"{UserSessionsSetPrefix}{userId}";
}
