using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Kernel.Extensions;

namespace TradingAnalytics.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the user device entity.
/// </summary>
internal sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("user_device");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeviceId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FcmToken).HasMaxLength(500);
        builder.Property(x => x.DeviceName).HasMaxLength(200);
        builder.Property(x => x.DeviceType)
            .HasConversion(
                x => x.ToString().ToSnakeCase()!,
                x => Enum.Parse<DeviceType>(x.ToPascalCase()!, true));
        builder.HasIndex(x => new { x.CustomerId, x.DeviceId }).IsUnique();
    }
}
