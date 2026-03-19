using System.Security.Cryptography;
using System.Text;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Generates and hashes refresh tokens.
/// </summary>
public static class RefreshTokenHasher
{
    /// <summary>
    /// Generates a raw refresh token and its SHA-256 hash.
    /// </summary>
    /// <returns>The raw token and stored hash.</returns>
    public static (string RawToken, string Hash) Generate()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (raw, Hash(raw));
    }

    /// <summary>
    /// Hashes a raw refresh token using SHA-256.
    /// </summary>
    /// <param name="rawToken">The raw refresh token.</param>
    /// <returns>The lowercase hexadecimal hash.</returns>
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}
