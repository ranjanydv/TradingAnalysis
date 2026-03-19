using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

/// <summary>
/// Represents a persisted notification document.
/// </summary>
[BsonCollection("notifications")]
public sealed class NotificationDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("recipientId")]
    public string RecipientId { get; set; } = string.Empty;

    [BsonElement("externalRef")]
    public string? ExternalRef { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("body")]
    public string Body { get; set; } = string.Empty;

    [BsonElement("image")]
    public string? Image { get; set; }

    [BsonElement("data")]
    public BsonDocument? Data { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "transactional";

    [BsonElement("priority")]
    public string Priority { get; set; } = "normal";

    [BsonElement("surfaces")]
    public List<string> Surfaces { get; set; } = ["web_in_app"];

    [BsonElement("statusBySurface")]
    public Dictionary<string, SurfaceStatusDocument> StatusBySurface { get; set; } = [];

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(15);

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents per-surface delivery state for a notification.
/// </summary>
public sealed class SurfaceStatusDocument
{
    [BsonElement("state")]
    public string State { get; set; } = "pending";

    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    [BsonElement("seenAt")]
    public DateTime? SeenAt { get; set; }

    [BsonElement("readAt")]
    public DateTime? ReadAt { get; set; }

    [BsonElement("archivedAt")]
    public DateTime? ArchivedAt { get; set; }

    [BsonElement("providerMetadata")]
    public BsonDocument? ProviderMetadata { get; set; }
}
