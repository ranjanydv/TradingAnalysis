using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using TradingAnalytics.Shared.Kernel;

namespace TradingAnalytics.Shared.Infrastructure.Persistence;

/// <summary>
/// Generates UUIDv7 identifiers for EF Core entities.
/// </summary>
public sealed class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    /// <inheritdoc />
    public override bool GeneratesTemporaryValues => false;

    /// <inheritdoc />
    public override Guid Next(EntityEntry entry) => NewId.Next();
}
