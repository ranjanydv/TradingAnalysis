using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;

namespace TradingAnalytics.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the customer session entity.
/// </summary>
internal sealed class CustomerSessionConfiguration : IEntityTypeConfiguration<CustomerSession>
{
    public void Configure(EntityTypeBuilder<CustomerSession> builder)
    {
        builder.ToTable("customer_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(100);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Type)
            .HasConversion(
                x => x.ToString().ToLowerInvariant(),
                x => Enum.Parse<SessionType>(x, true));
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.CustomerId);
    }
}
