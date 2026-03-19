namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Defines cross-module notification operations.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates and dispatches a notification across the requested surfaces.
    /// </summary>
    /// <param name="request">The notification request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(SendNotificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets unread in-app notifications for a recipient.
    /// </summary>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="surface">The notification surface.</param>
    /// <param name="limit">The maximum number of items to return.</param>
    /// <param name="afterId">The pagination cursor identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of inbox items.</returns>
    Task<List<NotificationInboxItem>> GetInAppAsync(
        string recipientId,
        NotificationSurface surface,
        int limit = 20,
        string? afterId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the state of a notification surface.
    /// </summary>
    /// <param name="notificationId">The notification identifier.</param>
    /// <param name="surface">The surface being updated.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateSurfaceStateAsync(
        string notificationId,
        NotificationSurface surface,
        NotificationSurfaceState newState,
        CancellationToken ct = default);

    /// <summary>
    /// Marks all in-app notifications as read for a recipient.
    /// </summary>
    /// <param name="recipientId">The recipient identifier.</param>
    /// <param name="surface">The surface to update.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task MarkAllReadAsync(string recipientId, NotificationSurface surface, CancellationToken ct = default);
}

/// <summary>
/// Represents a notification dispatch request.
/// </summary>
public sealed class SendNotificationRequest
{
    /// <summary>
    /// Gets or sets the recipient identifier.
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Gets or sets an external correlation identifier.
    /// </summary>
    public string? ExternalRef { get; init; }

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the notification body.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets or sets an optional image URL.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// Gets or sets arbitrary notification data.
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Transactional;

    /// <summary>
    /// Gets or sets the notification priority.
    /// </summary>
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>
    /// Gets or sets the target notification surfaces.
    /// </summary>
    public List<NotificationSurface> Surfaces { get; init; } = [NotificationSurface.WebInApp];
}

/// <summary>
/// Represents an in-app notification item.
/// </summary>
public sealed class NotificationInboxItem
{
    /// <summary>
    /// Gets or sets the notification identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the recipient identifier.
    /// </summary>
    public required string RecipientId { get; init; }

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the notification body.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets or sets the optional image.
    /// </summary>
    public string? Image { get; init; }

    /// <summary>
    /// Gets or sets arbitrary payload data.
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Gets or sets the creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the notification surface.
    /// </summary>
    public NotificationSurface Surface { get; init; }

    /// <summary>
    /// Gets or sets the current notification state.
    /// </summary>
    public NotificationSurfaceState State { get; init; }
}

/// <summary>
/// Defines notification delivery surfaces.
/// </summary>
public enum NotificationSurface
{
    /// <summary>
    /// Mobile in-app inbox.
    /// </summary>
    MobileInApp,

    /// <summary>
    /// Web in-app inbox.
    /// </summary>
    WebInApp,

    /// <summary>
    /// Mobile push notification.
    /// </summary>
    MobilePush,

    /// <summary>
    /// Web push notification.
    /// </summary>
    WebPush,

    /// <summary>
    /// Email notification.
    /// </summary>
    Email,

    /// <summary>
    /// SMS notification.
    /// </summary>
    Sms,
}

/// <summary>
/// Defines the lifecycle state of a notification surface.
/// </summary>
public enum NotificationSurfaceState
{
    /// <summary>
    /// Awaiting delivery.
    /// </summary>
    Pending,

    /// <summary>
    /// Successfully delivered.
    /// </summary>
    Delivered,

    /// <summary>
    /// Seen by the recipient.
    /// </summary>
    Seen,

    /// <summary>
    /// Marked as read.
    /// </summary>
    Read,

    /// <summary>
    /// Archived by the recipient.
    /// </summary>
    Archived,

    /// <summary>
    /// Deleted by the recipient or system.
    /// </summary>
    Deleted,
}

/// <summary>
/// Defines the business classification of a notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Transactional notification.
    /// </summary>
    Transactional,

    /// <summary>
    /// Promotional notification.
    /// </summary>
    Promotional,

    /// <summary>
    /// System notification.
    /// </summary>
    System,

    /// <summary>
    /// Personal notification.
    /// </summary>
    Personal,
}

/// <summary>
/// Defines the notification priority.
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// High priority notification.
    /// </summary>
    High,

    /// <summary>
    /// Normal priority notification.
    /// </summary>
    Normal,

    /// <summary>
    /// Low priority notification.
    /// </summary>
    Low,
}
