using System.Security.Cryptography;
using System.Text;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Provides HMAC-based hashing for short-lived OTP values.
/// </summary>
public static class OtpHasher
{
    private static byte[]? _secret;

    /// <summary>
    /// Configures the HMAC secret.
    /// </summary>
    /// <param name="secret">The secret key.</param>
    public static void Configure(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        _secret = Encoding.UTF8.GetBytes(secret);
    }

    /// <summary>
    /// Hashes an OTP value.
    /// </summary>
    /// <param name="otp">The OTP value.</param>
    /// <returns>A lowercase hex hash.</returns>
    public static string Hash(string otp)
    {
        EnsureConfigured();
        return Convert.ToHexString(HMACSHA256.HashData(_secret!, Encoding.UTF8.GetBytes(otp))).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies an OTP against a stored hash.
    /// </summary>
    /// <param name="rawOtp">The input OTP.</param>
    /// <param name="storedHash">The stored hash.</param>
    /// <returns><see langword="true"/> when the OTP matches.</returns>
    public static bool Verify(string rawOtp, string storedHash)
    {
        var expected = Hash(rawOtp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static void EnsureConfigured()
    {
        if (_secret is null)
        {
            throw new InvalidOperationException("OtpHasher has not been configured.");
        }
    }
}
