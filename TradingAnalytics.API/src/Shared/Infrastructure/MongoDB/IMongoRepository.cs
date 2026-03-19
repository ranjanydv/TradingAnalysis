using MongoDB.Driver;

namespace TradingAnalytics.Shared.Infrastructure.MongoDB;

/// <summary>
/// Provides generic MongoDB document persistence helpers.
/// </summary>
public interface IMongoRepository<T>
{
    Task InsertAsync(T document, CancellationToken ct = default);
    Task<T?> FindByIdAsync(string id, CancellationToken ct = default);
    Task<List<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T>? sort = null, int skip = 0, int limit = 50, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<T> update, CancellationToken ct = default);
    Task UpdateManyAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken ct = default);
    Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken ct = default);
    IMongoCollection<T> Collection { get; }
}
