using MongoDB.Driver;
using TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB;

/// <summary>
/// Creates required MongoDB indexes at startup.
/// </summary>
public static class MongoIndexInitializer
{
    /// <summary>
    /// Ensures all required infrastructure indexes exist.
    /// </summary>
    /// <param name="database">The Mongo database.</param>
    public static async Task EnsureIndexesAsync(IMongoDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        var notifications = database.GetCollection<NotificationDocument>("notifications");
        await notifications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.RecipientId).Descending(x => x.CreatedAt)),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.RecipientId).Ascending("surfaces").Descending(x => x.CreatedAt)),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.RecipientId).Ascending("statusBySurface.web_in_app.readAt").Descending(x => x.CreatedAt),
                new CreateIndexOptions { Sparse = true }),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.RecipientId).Ascending("statusBySurface.mobile_in_app.readAt").Descending(x => x.CreatedAt),
                new CreateIndexOptions { Sparse = true }),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.ExternalRef),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            new CreateIndexModel<NotificationDocument>(
                Builders<NotificationDocument>.IndexKeys.Ascending(x => x.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero })
        ]).ConfigureAwait(false);

        var settings = database.GetCollection<NotificationSettingDocument>("notification_settings");
        await settings.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationSettingDocument>(
                Builders<NotificationSettingDocument>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.NotificationType)
                    .Ascending(x => x.Surface),
                new CreateIndexOptions { Unique = true })).ConfigureAwait(false);

        var auditLogs = database.GetCollection<AuditLogDocument>("audit_logs");
        await auditLogs.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.Module).Descending(x => x.CreatedAt)),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.CreatedAt)),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.Module).Ascending(x => x.Action).Descending(x => x.CreatedAt)),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.Module).Ascending(x => x.Status).Descending(x => x.CreatedAt)),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.Module).Ascending(x => x.UserId).Descending(x => x.CreatedAt)),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending("resource.id").Ascending("resource.type")),
            new CreateIndexModel<AuditLogDocument>(
                Builders<AuditLogDocument>.IndexKeys.Ascending(x => x.CreatedAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90) })
        ]).ConfigureAwait(false);
    }
}
