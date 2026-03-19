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

    /// <summary>
    /// Gets the identifier being verified.
    /// </summary>
    public string Identifier { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the target address or phone.
    /// </summary>
    public string Target { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the verification purpose.
    /// </summary>
    public VerificationPurpose Purpose { get; private set; }

    /// <summary>
    /// Gets the delivery channel.
    /// </summary>
    public VerificationChannel Channel { get; private set; }

    /// <summary>
    /// Gets the SHA-256 token hash.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional OTP hash.
    /// </summary>
    public string? OtpHash { get; private set; }

    /// <summary>
    /// Gets the expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the consumed timestamp in UTC.
    /// </summary>
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>
    /// Gets the number of attempts used.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Gets the maximum number of attempts allowed.
    /// </summary>
    public int MaxAttempts { get; private set; } = 5;

    /// <summary>
    /// Gets a value indicating whether the verification is expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Gets a value indicating whether the verification is consumed.
    /// </summary>
    public bool IsConsumed => ConsumedAt.HasValue;

    /// <summary>
    /// Gets a value indicating whether the verification has exhausted attempts.
    /// </summary>
    public bool IsExhausted => Attempts >= MaxAttempts;

    /// <summary>
    /// Gets a value indicating whether the verification can still be used.
    /// </summary>
    public bool IsUsable => !IsExpired && !IsConsumed && !IsExhausted;

    /// <summary>
    /// Creates a verification request.
    /// </summary>
    /// <param name="identifier">The identifier being verified.</param>
    /// <param name="target">The delivery target.</param>
    /// <param name="purpose">The verification purpose.</param>
    /// <param name="channel">The delivery channel.</param>
    /// <param name="expiryMinutes">The expiration time in minutes.</param>
    /// <returns>The created verification, raw token, and optional raw OTP.</returns>
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

    /// <summary>
    /// Consumes a verification using the raw token.
    /// </summary>
    /// <param name="rawToken">The raw verification token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Consumes a verification using the raw OTP.
    /// </summary>
    /// <param name="rawOtp">The raw OTP code.</param>
    /// <returns>The operation result.</returns>
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
