using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Subscriptions.Domain.Entities;

namespace TradingAnalytics.Modules.Subscriptions.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures subscription access modules.
/// </summary>
internal sealed class AccessModuleConfiguration : IEntityTypeConfiguration<AccessModule>
{
    public void Configure(EntityTypeBuilder<AccessModule> builder)
    {
        builder.ToTable("access_modules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
