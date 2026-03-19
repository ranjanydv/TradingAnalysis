namespace TradingAnalytics.Shared.Infrastructure.Firebase;

/// <summary>
/// Sends push notifications through Firebase Cloud Messaging.
/// </summary>
public interface IFirebaseMessagingService
{
    /// <summary>
    /// Sends a message for a recipient.
    /// </summary>
    Task<string?> SendAsync(FcmMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends a multicast message for specific device tokens.
    /// </summary>
    Task<List<string>> SendMulticastAsync(List<string> tokens, FcmMessage message, CancellationToken ct = default);
}
