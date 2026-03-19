using System.Security.Cryptography;
using System.Text;
using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a verification request.
/// </summary>
public sealed class Verification : AggregateRoot
{
    private Verification()
    {
    }

    public string Identifier { get; private set; } = string.Empty;
    public string Target { get; private set; } = string.Empty;
    public VerificationPurpose Purpose { get; private set; }
    public VerificationChannel Channel { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? OtpHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; } = 5;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsExhausted => Attempts >= MaxAttempts;
    public bool IsUsable => !IsExpired && !IsConsumed && !IsExhausted;

    public static (Verification Verification, string RawToken, string? RawOtp) Create(
        string identifier,
        string target,
        VerificationPurpose purpose,
        VerificationChannel channel,
        int expiryMinutes = 10)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        string? rawOtp = null;
        if (channel == VerificationChannel.Sms || purpose is VerificationPurpose.PhoneLogin or VerificationPurpose.PhoneRegistration or VerificationPurpose.PhoneVerification)
        {
            rawOtp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        }

        var verification = new Verification
        {
            Identifier = identifier.Trim(),
            Target = target.Trim(),
            Purpose = purpose,
            Channel = channel,
            TokenHash = ComputeSha256(rawToken),
            OtpHash = rawOtp is null ? null : OtpHasher.Hash(rawOtp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
        };

        return (verification, rawToken, rawOtp);
    }

    public Result TryConsumeWithToken(string rawToken)
    {
        if (!IsUsable)
        {
            return Result.Failure("Verification is no longer usable.");
        }

        if (!string.Equals(TokenHash, ComputeSha256(rawToken), StringComparison.Ordinal))
        {
            return Result.Failure("Invalid verification token.");
        }

        ConsumedAt = DateTime.UtcNow;
        Touch();
        return Result.Success();
    }

    public Result TryConsumeWithOtp(string rawOtp)
    {
        if (!IsUsable)
        {
            return Result.Failure("Verification is no longer usable.");
        }

        Attempts++;
        Touch();

        if (OtpHash is null || !OtpHasher.Verify(rawOtp, OtpHash))
        {
            return Result.Failure("Invalid verification code.");
        }

        ConsumedAt = DateTime.UtcNow;
        Touch();
        return Result.Success();
    }

    private static string ComputeSha256(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
