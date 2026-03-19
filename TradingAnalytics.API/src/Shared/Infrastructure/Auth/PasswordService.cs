using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Provides Argon2id-based password hashing.
/// </summary>
public sealed class PasswordService : IPasswordService
{
    /// <inheritdoc />
    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            TimeCost = 3,
            MemoryCost = 65536,
            Lanes = 2,
            Threads = 2,
            HashLength = 32,
            Password = Encoding.UTF8.GetBytes(plaintext),
            Salt = RandomNumberGenerator.GetBytes(16),
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        return config.EncodeString(hash.Buffer);
    }

    /// <inheritdoc />
    public bool Verify(string plaintext, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return Argon2.Verify(hash, Encoding.UTF8.GetBytes(plaintext));
    }
}
