using UUIDNext;

namespace TradingAnalytics.Shared.Kernel;

/// <summary>
/// Generates monotonic UUIDv7 identifiers optimized for PostgreSQL storage.
/// </summary>
public static class NewId
{
    /// <summary>
    /// Creates a new database-friendly UUIDv7 value.
    /// </summary>
    /// <returns>A generated unique identifier.</returns>
    public static Guid Next() => Uuid.NewDatabaseFriendly();
}
