using TradingAnalytics.Modules.Identity.Application.Dtos;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Shared.Infrastructure.Session;

namespace TradingAnalytics.Modules.Identity.Application.Mappings;

/// <summary>
/// Provides Identity DTO mapping helpers.
/// </summary>
internal static class IdentityMappings
{
    public static CustomerProfileDto ToProfileDto(this Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        EmailVerified = customer.EmailVerified,
        Phone = customer.Phone,
        PhoneVerified = customer.PhoneVerified,
        Image = customer.Image,
        RoleId = customer.RoleId,
        Banned = customer.Banned,
        BanReason = customer.BanReason,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
    };

    public static CustomerSummaryDto ToSummaryDto(this Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone,
        Banned = customer.Banned,
        CreatedAt = customer.CreatedAt,
    };

    public static CustomerDetailDto ToDetailDto(this Customer customer) => new()
    {
        Id = customer.Id,
        Name = customer.Name,
        Email = customer.Email,
        EmailVerified = customer.EmailVerified,
        Phone = customer.Phone,
        PhoneVerified = customer.PhoneVerified,
        Image = customer.Image,
        RoleId = customer.RoleId,
        Banned = customer.Banned,
        BanReason = customer.BanReason,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt,
    };

    public static SessionDto ToDto(this CustomerSession session) => new()
    {
        Id = session.Id,
        Token = session.Token,
        CreatedAt = session.CreatedAt,
        ExpiresAt = session.ExpiresAt,
        Type = session.Type,
        UserDeviceId = session.UserDeviceId,
        IpAddress = session.IpAddress,
        UserAgent = session.UserAgent,
        IsExpired = session.IsExpired,
    };

    public static DeviceDto ToDto(this UserDevice device) => new()
    {
        Id = device.Id,
        DeviceId = device.DeviceId,
        FcmToken = device.FcmToken,
        DeviceType = device.DeviceType,
        DeviceName = device.DeviceName,
        IsActive = device.IsActive,
        LastActiveAt = device.LastActiveAt,
        CreatedAt = device.CreatedAt,
        UpdatedAt = device.UpdatedAt,
    };

    public static SessionData ToSessionData(this CustomerSession session, string role) => new(
        session.CustomerId,
        "customer",
        role,
        session.UserDeviceId,
        session.Type.ToString().ToLowerInvariant(),
        session.ExpiresAt);

    public static SessionData ToSessionData(this AdminSession session, string role) => new(
        session.AdminId,
        "admin",
        role,
        null,
        session.Type.ToString().ToLowerInvariant(),
        session.ExpiresAt);
}
