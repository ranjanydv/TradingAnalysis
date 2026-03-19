namespace TradingAnalytics.Shared.Infrastructure.Firebase;

/// <summary>
/// Represents a Firebase push message.
/// </summary>
/// <param name="RecipientId">The recipient identifier.</param>
/// <param name="Title">The message title.</param>
/// <param name="Body">The message body.</param>
/// <param name="Data">The optional data payload.</param>
/// <param name="NotificationId">The correlated notification identifier.</param>
/// <param name="ImageUrl">The optional image URL.</param>
public sealed record FcmMessage(
    string RecipientId,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null,
    string? NotificationId = null,
    string? ImageUrl = null);
