using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

/// <summary>
/// Represents user notification preferences.
/// </summary>
[BsonCollection("notification_settings")]
internal sealed class NotificationSettingDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    [BsonElement("surface")]
    public string Surface { get; set; } = string.Empty;

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
