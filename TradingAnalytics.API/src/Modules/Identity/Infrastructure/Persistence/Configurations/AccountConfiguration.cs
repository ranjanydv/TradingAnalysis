using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Extensions;

namespace TradingAnalytics.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the account entity.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountId).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProviderId).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(500);
        builder.Property(x => x.ActorType)
            .HasConversion(
                x => x.ToString().ToLowerInvariant(),
                x => Enum.Parse<AccountActorType>(x, true));
        builder.HasIndex(x => new { x.ProviderId, x.AccountId, x.ActorType }).IsUnique();
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
