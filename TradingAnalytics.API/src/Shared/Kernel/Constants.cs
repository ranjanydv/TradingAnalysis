namespace TradingAnalytics.Shared.Kernel;

/// <summary>
/// Provides application-wide constant values.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Claim type constants.
    /// </summary>
    public static class ClaimTypes
    {
        /// <summary>
        /// Subject claim.
        /// </summary>
        public const string UserId = "sub";

        /// <summary>
        /// Actor type claim.
        /// </summary>
        public const string ActorType = "actor_type";

        /// <summary>
        /// Role claim.
        /// </summary>
        public const string Role = "role";

        /// <summary>
        /// Email claim.
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// Phone claim.
        /// </summary>
        public const string Phone = "phone";
    }

    /// <summary>
    /// Actor type constants.
    /// </summary>
    public static class ActorTypes
    {
        /// <summary>
        /// Customer actor type.
        /// </summary>
        public const string Customer = "customer";

        /// <summary>
        /// Admin actor type.
        /// </summary>
        public const string Admin = "admin";
    }

    /// <summary>
    /// External authentication provider constants.
    /// </summary>
    public static class Providers
    {
        /// <summary>
        /// Credential provider.
        /// </summary>
        public const string Credential = "credential";

        /// <summary>
        /// Google provider.
        /// </summary>
        public const string Google = "google";
    }

    /// <summary>
    /// Role constants.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Super administrator role.
        /// </summary>
        public const string SuperAdmin = "super_admin";

        /// <summary>
        /// Administrator role.
        /// </summary>
        public const string Admin = "admin";

        /// <summary>
        /// Moderator role.
        /// </summary>
        public const string Moderator = "moderator";
    }
}
