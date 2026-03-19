using TradingAnalytics.Modules.Identity.Domain.Enums;
using TradingAnalytics.Shared.Infrastructure.Auth;
using TradingAnalytics.Shared.Kernel;
using TradingAnalytics.Shared.Kernel.Entities;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents an authentication account.
/// </summary>
public sealed class Account : AggregateRoot
{
    private Account()
    {
    }

    /// <summary>
    /// Gets the provider account identifier.
    /// </summary>
    public string AccountId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the actor type.
    /// </summary>
    public AccountActorType ActorType { get; private set; }

    /// <summary>
    /// Gets the linked customer identifier.
    /// </summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>
    /// Gets the linked admin identifier.
    /// </summary>
    public Guid? AdminId { get; private set; }

    /// <summary>
    /// Gets the external access token.
    /// </summary>
    public string? AccessToken { get; private set; }

    /// <summary>
    /// Gets the external refresh token.
    /// </summary>
    public string? RefreshToken { get; private set; }

    /// <summary>
    /// Gets the external id token.
    /// </summary>
    public string? IdToken { get; private set; }

    /// <summary>
    /// Gets the access-token expiration timestamp in UTC.
    /// </summary>
    public DateTime? AccessTokenExpiresAt { get; private set; }

    /// <summary>
    /// Gets the refresh-token expiration timestamp in UTC.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    /// <summary>
    /// Gets the granted scope.
    /// </summary>
    public string? Scope { get; private set; }

    /// <summary>
    /// Gets the hashed password.
    /// </summary>
    public string? Password { get; private set; }

    /// <summary>
    /// Creates a credential account for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="email">The customer email.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <returns>The created account.</returns>
    public static Account CreateCredentialForCustomer(Guid customerId, string email, string passwordHash) =>
        new()
        {
            AccountId = email.Trim().ToLowerInvariant(),
            ProviderId = Constants.Providers.Credential,
            ActorType = AccountActorType.Customer,
            CustomerId = customerId,
            Password = passwordHash,
        };

    /// <summary>
    /// Creates a credential account for an admin.
    /// </summary>
    /// <param name="adminId">The admin identifier.</param>
    /// <param name="email">The admin email.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <returns>The created account.</returns>
    public static Account CreateCredentialForAdmin(Guid adminId, string email, string passwordHash) =>
        new()
        {
            AccountId = email.Trim().ToLowerInvariant(),
            ProviderId = Constants.Providers.Credential,
            ActorType = AccountActorType.Admin,
            AdminId = adminId,
            Password = passwordHash,
        };

    /// <summary>
    /// Creates an OAuth account for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="oauthSub">The OAuth subject.</param>
    /// <returns>The created account.</returns>
    public static Account CreateOAuthForCustomer(Guid customerId, string providerId, string oauthSub) =>
        new()
        {
            AccountId = oauthSub.Trim(),
            ProviderId = providerId.Trim().ToLowerInvariant(),
            ActorType = AccountActorType.Customer,
            CustomerId = customerId,
        };

    /// <summary>
    /// Updates the stored password hash.
    /// </summary>
    /// <param name="newHash">The new password hash.</param>
    public void UpdatePassword(string newHash)
    {
        Password = newHash;
        Touch();
    }

    /// <summary>
    /// Updates provider-issued tokens.
    /// </summary>
    /// <param name="access">The access token.</param>
    /// <param name="refresh">The refresh token.</param>
    /// <param name="idToken">The id token.</param>
    /// <param name="accessExpiry">The access-token expiry time.</param>
    /// <param name="refreshExpiry">The refresh-token expiry time.</param>
    /// <param name="scope">The granted scope.</param>
    public void UpdateTokens(string? access, string? refresh, string? idToken, DateTime? accessExpiry, DateTime? refreshExpiry, string? scope = null)
    {
        AccessToken = access;
        RefreshToken = refresh;
        IdToken = idToken;
        AccessTokenExpiresAt = accessExpiry;
        RefreshTokenExpiresAt = refreshExpiry;
        Scope = scope;
        Touch();
    }

    /// <summary>
    /// Verifies a plaintext password against the stored hash.
    /// </summary>
    /// <param name="passwordService">The password service.</param>
    /// <param name="plaintext">The plaintext password.</param>
    /// <returns><see langword="true"/> when the password matches.</returns>
    public bool VerifyPassword(IPasswordService passwordService, string plaintext) =>
        Password is not null && passwordService.Verify(plaintext, Password);
}
