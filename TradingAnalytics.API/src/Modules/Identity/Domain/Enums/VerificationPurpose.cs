namespace TradingAnalytics.Modules.Identity.Domain.Enums;

/// <summary>
/// Defines verification business purposes.
/// </summary>
public enum VerificationPurpose
{
    /// <summary>
    /// Email-address verification flow.
    /// </summary>
    EmailVerification,

    /// <summary>
    /// Password-reset flow.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Phone-number verification flow.
    /// </summary>
    PhoneVerification,

    /// <summary>
    /// Phone-login flow.
    /// </summary>
    PhoneLogin,

    /// <summary>
    /// Phone-registration flow.
    /// </summary>
    PhoneRegistration,
}
