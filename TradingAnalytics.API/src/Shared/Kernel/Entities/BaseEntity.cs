using TradingAnalytics.Shared.Kernel.Interfaces;

namespace TradingAnalytics.Shared.Kernel.Entities;

/// <summary>
/// Provides common fields and domain-event handling for entities.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class for EF Core.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public Guid Id { get; private set; } = NewId.Next();

    /// <summary>
    /// Gets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the entity is soft deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the UTC deletion timestamp when the entity is soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Gets the domain events raised by the entity.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Marks the entity as updated.
    /// </summary>
    protected void Touch() => UpdatedAt = DateTime.UtcNow;

    /// <summary>
    /// Raises a domain event for the entity.
    /// </summary>
    /// <param name="domainEvent">The event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears any collected domain events.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Soft deletes the entity.
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }
}

/// <summary>
/// Represents an aggregate root in the domain model.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
}
