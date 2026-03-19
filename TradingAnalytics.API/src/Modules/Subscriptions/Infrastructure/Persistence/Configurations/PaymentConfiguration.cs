using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures payment records.
/// </summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(200);
        builder.HasIndex(x => x.UserSubscriptionId);
        builder.HasIndex(x => x.ExternalReference).IsUnique().HasFilter("\"ExternalReference\" IS NOT NULL");
    }
}
