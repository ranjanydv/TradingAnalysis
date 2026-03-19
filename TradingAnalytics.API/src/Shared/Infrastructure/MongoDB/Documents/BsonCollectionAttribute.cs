namespace TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

/// <summary>
/// Specifies the MongoDB collection name for a document type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BsonCollectionAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the collection name.
    /// </summary>
    public string Name { get; } = name;
}
