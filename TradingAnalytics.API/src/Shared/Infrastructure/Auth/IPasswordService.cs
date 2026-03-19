namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Hashes and verifies passwords.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    /// <param name="plaintext">The plaintext password.</param>
    /// <returns>The hashed value.</returns>
    string Hash(string plaintext);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    /// <param name="plaintext">The plaintext password.</param>
    /// <param name="hash">The stored hash.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    bool Verify(string plaintext, string hash);
}
