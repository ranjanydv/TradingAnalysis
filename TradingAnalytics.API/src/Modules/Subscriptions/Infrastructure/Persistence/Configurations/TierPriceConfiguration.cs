using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures tier prices.
/// </summary>
internal sealed class TierPriceConfiguration : IEntityTypeConfiguration<TierPrice>
{
    public void Configure(EntityTypeBuilder<TierPrice> builder)
    {
        builder.ToTable("tier_prices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.TierId, x.Currency, x.BillingCycle });
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
