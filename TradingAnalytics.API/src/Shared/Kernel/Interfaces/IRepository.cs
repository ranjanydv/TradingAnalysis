using TradingAnalytics.Shared.Kernel.Entities;

namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Defines the basic persistence contract for aggregate roots.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
public interface IRepository<T>
    where T : AggregateRoot
{
    /// <summary>
    /// Gets an aggregate by identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The aggregate when found; otherwise <see langword="null"/>.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Adds a new aggregate.
    /// </summary>
    /// <param name="entity">The aggregate instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing aggregate.
    /// </summary>
    /// <param name="entity">The aggregate instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an aggregate.
    /// </summary>
    /// <param name="entity">The aggregate instance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
