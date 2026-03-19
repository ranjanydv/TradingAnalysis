using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures user subscriptions.
/// </summary>
internal sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("user_subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.CustomerId, x.ModuleId });
        builder.HasIndex(x => new { x.CustomerId, x.Status, x.EndsAt });
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
