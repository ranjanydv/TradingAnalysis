using System.Reflection;
using MongoDB.Driver;
using TradingAnalytics.Shared.Infrastructure.MongoDB.Documents;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB;

/// <summary>
/// Implements a generic MongoDB repository.
/// </summary>
public sealed class MongoRepository<T>(IMongoDatabase database) : IMongoRepository<T>
{
    /// <inheritdoc />
    public IMongoCollection<T> Collection { get; } = (database ?? throw new ArgumentNullException(nameof(database)))
        .GetCollection<T>(
            typeof(T).GetCustomAttribute<BsonCollectionAttribute>()?.Name
            ?? typeof(T).Name.ToLowerInvariant());

    /// <inheritdoc />
    public Task InsertAsync(T document, CancellationToken ct = default) => Collection.InsertOneAsync(document, cancellationToken: ct);

    /// <inheritdoc />
    public async Task<T?> FindByIdAsync(string id, CancellationToken ct = default) =>
        await Collection.Find(Builders<T>.Filter.Eq("_id", id)).FirstOrDefaultAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<List<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T>? sort = null, int skip = 0, int limit = 50, CancellationToken ct = default)
    {
        var query = Collection.Find(filter).Skip(skip).Limit(limit);
        if (sort is not null)
        {
            query = query.Sort(sort);
        }

        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(string id, UpdateDefinition<T> update, CancellationToken ct = default) =>
        Collection.UpdateOneAsync(Builders<T>.Filter.Eq("_id", id), update, cancellationToken: ct);

    /// <inheritdoc />
    public Task UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken ct = default) =>
        Collection.UpdateManyAsync(filter, update, cancellationToken: ct);

    /// <inheritdoc />
    public Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken ct = default) =>
        Collection.CountDocumentsAsync(filter, cancellationToken: ct);
}
