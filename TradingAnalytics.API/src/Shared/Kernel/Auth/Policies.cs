namespace TradingAnalytics.Shared.Kernel.Auth;

/// <summary>
/// Defines authorization policy names used by the API.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Administrator-only policy name.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Customer-only policy name.
    /// </summary>
    public const string CustomerOnly = "CustomerOnly";

    /// <summary>
    /// Policy name for any authenticated actor.
    /// </summary>
    public const string AnyActor = "AnyActor";
}
