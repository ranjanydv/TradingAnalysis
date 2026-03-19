using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using TradingAnalytics.Shared.Infrastructure.Firebase;
using TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;
using TradingAnalytics.Shared.Kernel.Extensions;
using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB;

/// <summary>
/// Persists notifications and dispatches push delivery when applicable.
/// </summary>
internal sealed class NotificationService(
    IMongoRepository<NotificationDocument> repository,
    IFirebaseMessagingService firebaseMessagingService,
    IMongoRepository<NotificationSettingDocument> settingsRepository) : INotificationService
{
    private readonly IMongoRepository<NotificationDocument> _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IFirebaseMessagingService _firebaseMessagingService = firebaseMessagingService ?? throw new ArgumentNullException(nameof(firebaseMessagingService));
    private readonly IMongoRepository<NotificationSettingDocument> _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));

    /// <inheritdoc />
    public async Task SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var enabledSurfaces = await FilterByUserSettingsAsync(request.RecipientId, request.Type, request.Surfaces, ct).ConfigureAwait(false);
        if (enabledSurfaces.Count == 0)
        {
            return;
        }

        var statusBySurface = enabledSurfaces.ToDictionary(
            static surface => SurfaceToString(surface),
            static _ => new SurfaceStatusDocument { State = "pending" });

        var document = new NotificationDocument
        {
            RecipientId = request.RecipientId,
            ExternalRef = request.ExternalRef,
            Title = request.Title,
            Body = request.Body,
            Image = request.Image,
            Data = request.Data is not null ? BsonDocument.Parse(JsonSerializer.Serialize(request.Data)) : null,
            Type = request.Type.ToString().ToLowerInvariant(),
            Priority = request.Priority.ToString().ToLowerInvariant(),
            Surfaces = enabledSurfaces.Select(static surface => SurfaceToString(surface)).ToList(),
            StatusBySurface = statusBySurface,
        };

        await _repository.InsertAsync(document, ct).ConfigureAwait(false);

        foreach (var surface in enabledSurfaces.Where(static x => x is NotificationSurface.MobilePush or NotificationSurface.WebPush))
        {
            _ = Task.Run(
                async () =>
                {
                    await _firebaseMessagingService.SendAsync(
                        new FcmMessage(
                            request.RecipientId,
                            request.Title,
                            request.Body,
                            request.Data?.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value?.ToString() ?? string.Empty),
                            document.Id,
                            request.Image),
                        ct).ConfigureAwait(false);
                },
                ct);
        }
    }

    /// <inheritdoc />
    public async Task<List<NotificationInboxItem>> GetInAppAsync(string recipientId, NotificationSurface surface, int limit = 20, string? afterId = null, CancellationToken ct = default)
    {
        var surfaceKey = SurfaceToString(surface);
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(x => x.RecipientId, recipientId),
            Builders<NotificationDocument>.Filter.AnyEq(x => x.Surfaces, surfaceKey));

        if (!string.IsNullOrWhiteSpace(afterId))
        {
            filter &= Builders<NotificationDocument>.Filter.Lt("_id", new ObjectId(afterId));
        }

        var documents = await _repository.FindAsync(
            filter,
            Builders<NotificationDocument>.Sort.Descending(x => x.CreatedAt),
            limit: limit,
            ct: ct).ConfigureAwait(false);

        return documents.Select(document => new NotificationInboxItem
        {
            Id = document.Id,
            RecipientId = document.RecipientId,
            Title = document.Title,
            Body = document.Body,
            Image = document.Image,
            Data = document.Data is null
                ? null
                : document.Data.Elements.ToDictionary(
                    static element => element.Name,
                    static element => (object)(element.Value.ToString() ?? string.Empty)),
            CreatedAtUtc = document.CreatedAt,
            Surface = surface,
            State = document.StatusBySurface.TryGetValue(surfaceKey, out var status)
                ? ParseSurfaceState(status.State)
                : NotificationSurfaceState.Pending,
        }).ToList();
    }

    /// <inheritdoc />
    public async Task UpdateSurfaceStateAsync(string notificationId, NotificationSurface surface, NotificationSurfaceState newState, CancellationToken ct = default)
    {
        var surfaceKey = SurfaceToString(surface);
        var field = $"statusBySurface.{surfaceKey}";
        var update = Builders<NotificationDocument>.Update.Set($"{field}.state", StateToString(newState));

        update = newState switch
        {
            NotificationSurfaceState.Delivered => update.Set($"{field}.deliveredAt", DateTime.UtcNow),
            NotificationSurfaceState.Seen => update.Set($"{field}.seenAt", DateTime.UtcNow),
            NotificationSurfaceState.Read => update.Set($"{field}.readAt", DateTime.UtcNow),
            NotificationSurfaceState.Archived => update.Set($"{field}.archivedAt", DateTime.UtcNow),
            _ => update
        };

        update = update.Set("updatedAt", DateTime.UtcNow);
        await _repository.UpdateAsync(notificationId, update, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task MarkAllReadAsync(string recipientId, NotificationSurface surface, CancellationToken ct = default)
    {
        var surfaceKey = SurfaceToString(surface);
        var filter = Builders<NotificationDocument>.Filter.And(
            Builders<NotificationDocument>.Filter.Eq(x => x.RecipientId, recipientId),
            Builders<NotificationDocument>.Filter.AnyEq(x => x.Surfaces, surfaceKey),
            Builders<NotificationDocument>.Filter.Not(
                Builders<NotificationDocument>.Filter.Exists($"statusBySurface.{surfaceKey}.readAt")));

        var update = Builders<NotificationDocument>.Update
            .Set($"statusBySurface.{surfaceKey}.state", "read")
            .Set($"statusBySurface.{surfaceKey}.readAt", DateTime.UtcNow)
            .Set("updatedAt", DateTime.UtcNow);

        return _repository.UpdateManyAsync(filter, update, ct);
    }

    private async Task<List<NotificationSurface>> FilterByUserSettingsAsync(string recipientId, NotificationType type, List<NotificationSurface> requested, CancellationToken ct)
    {
        var filter = Builders<NotificationSettingDocument>.Filter.And(
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.UserId, recipientId),
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.NotificationType, type.ToString().ToLowerInvariant()),
            Builders<NotificationSettingDocument>.Filter.Eq(x => x.Enabled, false));

        var optedOut = await _settingsRepository.FindAsync(filter, ct: ct).ConfigureAwait(false);
        var optedOutSurfaces = optedOut.Select(static x => x.Surface).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requested.Where(surface => !optedOutSurfaces.Contains(SurfaceToString(surface))).ToList();
    }

    private static string SurfaceToString(NotificationSurface surface) => surface switch
    {
        NotificationSurface.MobileInApp => "mobile_in_app",
        NotificationSurface.WebInApp => "web_in_app",
        NotificationSurface.MobilePush => "mobile_push",
        NotificationSurface.WebPush => "web_push",
        NotificationSurface.Email => "email",
        NotificationSurface.Sms => "sms",
        _ => surface.ToString().ToSnakeCase() ?? surface.ToString().ToLowerInvariant(),
    };

    private static string StateToString(NotificationSurfaceState state) => state switch
    {
        NotificationSurfaceState.Pending => "pending",
        NotificationSurfaceState.Delivered => "delivered",
        NotificationSurfaceState.Seen => "seen",
        NotificationSurfaceState.Read => "read",
        NotificationSurfaceState.Archived => "archived",
        NotificationSurfaceState.Deleted => "deleted",
        _ => state.ToString().ToLowerInvariant(),
    };

    private static NotificationSurfaceState ParseSurfaceState(string state) => state switch
    {
        "pending" => NotificationSurfaceState.Pending,
        "delivered" => NotificationSurfaceState.Delivered,
        "seen" => NotificationSurfaceState.Seen,
        "read" => NotificationSurfaceState.Read,
        "archived" => NotificationSurfaceState.Archived,
        "deleted" => NotificationSurfaceState.Deleted,
        _ => NotificationSurfaceState.Pending,
    };
}
