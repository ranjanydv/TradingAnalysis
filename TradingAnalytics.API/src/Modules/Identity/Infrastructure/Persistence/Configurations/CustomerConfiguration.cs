using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Identity.Domain.Entities;

namespace TradingAnalytics.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the customer entity.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Image).HasMaxLength(500);
        builder.Property(x => x.Banned).HasDefaultValue(false);
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
        builder.HasIndex(x => x.Phone).IsUnique().HasFilter("\"Phone\" IS NOT NULL");
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
