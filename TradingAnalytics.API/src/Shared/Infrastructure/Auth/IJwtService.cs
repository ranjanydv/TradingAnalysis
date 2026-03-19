namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Generates JWT access tokens for customers and administrators.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT for a customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="email">The optional customer email.</param>
    /// <param name="phone">The optional customer phone.</param>
    /// <param name="role">The role code.</param>
    /// <param name="permissions">The granted permission codes.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A signed JWT.</returns>
    string GenerateCustomerToken(
        Guid customerId,
        string? email,
        string? phone,
        string role,
        IEnumerable<string> permissions,
        Guid sessionId);

    /// <summary>
    /// Generates a JWT for an administrator.
    /// </summary>
    /// <param name="adminId">The administrator identifier.</param>
    /// <param name="email">The administrator email.</param>
    /// <param name="role">The role code.</param>
    /// <param name="permissions">The granted permission codes.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A signed JWT.</returns>
    string GenerateAdminToken(
        Guid adminId,
        string email,
        string role,
        IEnumerable<string> permissions,
        Guid sessionId);
}
