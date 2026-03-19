using System.Text.Json;
using MongoDB.Bson;
using TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;
using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB;

/// <summary>
/// Persists audit entries into MongoDB.
/// </summary>
public sealed class AuditLogger(IMongoRepository<AuditLogDocument> repository) : IAuditLogger
{
    private readonly IMongoRepository<AuditLogDocument> _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var document = new AuditLogDocument
        {
            UserId = entry.UserId,
            UserName = entry.UserName,
            UserRole = entry.UserRole,
            Action = entry.Action,
            Module = entry.Module,
            Status = entry.Status,
            Resource = entry.ResourceId is not null
                ? new AuditResourceDocument
                {
                    Id = entry.ResourceId,
                    Type = entry.ResourceType,
                }
                : null,
            Changes = entry.Changes?.Select(static change => new AuditChangeDocument
            {
                Field = change.Field,
                From = change.From is not null ? BsonValue.Create(change.From) : BsonNull.Value,
                To = change.To is not null ? BsonValue.Create(change.To) : BsonNull.Value,
            }).ToList(),
            Reason = entry.Reason,
            Metadata = entry.Metadata is not null ? BsonDocument.Parse(JsonSerializer.Serialize(entry.Metadata)) : null,
            Request = entry.Request is not null
                ? new AuditRequestDocument
                {
                    Ip = entry.Request.Ip,
                    UserAgent = entry.Request.UserAgent,
                    Endpoint = entry.Request.Endpoint,
                    Method = entry.Request.Method,
                    StatusCode = entry.Request.StatusCode,
                    DurationMs = entry.Request.DurationMs,
                }
                : null,
            Error = entry.Error is not null
                ? new AuditErrorDocument
                {
                    Code = entry.Error.Code,
                    Message = entry.Error.Message,
                    Stack = entry.Error.Stack,
                }
                : null,
        };

        return _repository.InsertAsync(document, ct);
    }
}
