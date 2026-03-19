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

    public string AccountId { get; private set; } = string.Empty;
    public string ProviderId { get; private set; } = string.Empty;
    public AccountActorType ActorType { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? AdminId { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? IdToken { get; private set; }
    public DateTime? AccessTokenExpiresAt { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public string? Scope { get; private set; }
    public string? Password { get; private set; }

    public static Account CreateCredentialForCustomer(Guid customerId, string email, string passwordHash) =>
        new()
        {
            AccountId = email.Trim().ToLowerInvariant(),
            ProviderId = Constants.Providers.Credential,
            ActorType = AccountActorType.Customer,
            CustomerId = customerId,
            Password = passwordHash,
        };

    public static Account CreateCredentialForAdmin(Guid adminId, string email, string passwordHash) =>
        new()
        {
            AccountId = email.Trim().ToLowerInvariant(),
            ProviderId = Constants.Providers.Credential,
            ActorType = AccountActorType.Admin,
            AdminId = adminId,
            Password = passwordHash,
        };

    public static Account CreateOAuthForCustomer(Guid customerId, string providerId, string oauthSub) =>
        new()
        {
            AccountId = oauthSub.Trim(),
            ProviderId = providerId.Trim().ToLowerInvariant(),
            ActorType = AccountActorType.Customer,
            CustomerId = customerId,
        };

    public void UpdatePassword(string newHash)
    {
        Password = newHash;
        Touch();
    }

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

    public bool VerifyPassword(IPasswordService passwordService, string plaintext) =>
        Password is not null && passwordService.Verify(plaintext, Password);
}
