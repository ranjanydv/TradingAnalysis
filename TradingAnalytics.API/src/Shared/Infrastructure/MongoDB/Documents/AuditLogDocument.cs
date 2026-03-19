using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

/// <summary>
/// Represents an audit log document stored in MongoDB.
/// </summary>
[BsonCollection("audit_logs")]
internal sealed class AuditLogDocument
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("userName")]
    public string? UserName { get; set; }

    [BsonElement("userRole")]
    public string? UserRole { get; set; }

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("module")]
    public string Module { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "success";

    [BsonElement("resource")]
    public AuditResourceDocument? Resource { get; set; }

    [BsonElement("changes")]
    public List<AuditChangeDocument>? Changes { get; set; }

    [BsonElement("reason")]
    public string? Reason { get; set; }

    [BsonElement("metadata")]
    public BsonDocument? Metadata { get; set; }

    [BsonElement("request")]
    public AuditRequestDocument? Request { get; set; }

    [BsonElement("error")]
    public AuditErrorDocument? Error { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

internal sealed class AuditResourceDocument
{
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("type")]
    public string? Type { get; set; }
}

internal sealed class AuditChangeDocument
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonElement("from")]
    public BsonValue? From { get; set; }

    [BsonElement("to")]
    public BsonValue? To { get; set; }
}

internal sealed class AuditRequestDocument
{
    [BsonElement("ip")]
    public string? Ip { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("endpoint")]
    public string? Endpoint { get; set; }

    [BsonElement("method")]
    public string? Method { get; set; }

    [BsonElement("statusCode")]
    public int? StatusCode { get; set; }

    [BsonElement("duration")]
    public long? DurationMs { get; set; }
}

internal sealed class AuditErrorDocument
{
    [BsonElement("code")]
    public string? Code { get; set; }

    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("stack")]
    public string? Stack { get; set; }
}
