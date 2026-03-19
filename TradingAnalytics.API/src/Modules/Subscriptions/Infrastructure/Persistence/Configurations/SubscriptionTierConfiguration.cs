using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures subscription tiers.
/// </summary>
internal sealed class SubscriptionTierConfiguration : IEntityTypeConfiguration<SubscriptionTier>
{
    public void Configure(EntityTypeBuilder<SubscriptionTier> builder)
    {
        builder.ToTable("subscription_tiers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.ModuleId, x.Name }).IsUnique();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
