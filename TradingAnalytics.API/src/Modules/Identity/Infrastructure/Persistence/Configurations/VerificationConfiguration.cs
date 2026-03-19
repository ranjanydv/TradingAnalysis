using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Extensions;

namespace TradingAnalytics.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the verification entity.
/// </summary>
public sealed class VerificationConfiguration : IEntityTypeConfiguration<Verification>
{
    public void Configure(EntityTypeBuilder<Verification> builder)
    {
        builder.ToTable("verification");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Identifier).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Target).HasMaxLength(255).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OtpHash).HasMaxLength(128);
        builder.Property(x => x.Purpose)
            .HasConversion(
                x => x.ToString().ToSnakeCase()!,
                x => Enum.Parse<VerificationPurpose>(x.ToPascalCase()!, true));
        builder.Property(x => x.Channel)
            .HasConversion(
                x => x.ToString().ToLowerInvariant(),
                x => Enum.Parse<VerificationChannel>(x, true));
        builder.HasIndex(x => new { x.TokenHash, x.Purpose });
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.DeletedAt);
    }
}
