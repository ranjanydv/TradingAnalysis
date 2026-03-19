namespace TradingAnalytics.Shared.Infrastructure.Session;

/// <summary>
/// Represents the stored session payload.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="ActorType">The actor type.</param>
/// <param name="Role">The role claim.</param>
/// <param name="DeviceId">The optional device identifier.</param>
/// <param name="SessionType">The session type.</param>
/// <param name="ExpiresAt">The expiration timestamp in UTC.</param>
public sealed record SessionData(
    Guid UserId,
    string ActorType,
    string Role,
    Guid? DeviceId,
    string SessionType,
    DateTime ExpiresAt);
