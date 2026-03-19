using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents an administrative user.
/// </summary>
public sealed class AdminUser : AggregateRoot
{
    private AdminUser()
    {
    }

    /// <summary>
    /// Gets the admin name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the admin email.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the email is verified.
    /// </summary>
    public bool EmailVerified { get; private set; }

    /// <summary>
    /// Gets the phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the phone is verified.
    /// </summary>
    public bool PhoneVerified { get; private set; }

    /// <summary>
    /// Gets the image URL.
    /// </summary>
    public string? Image { get; private set; }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public int? RoleId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the admin is banned.
    /// </summary>
    public bool Banned { get; private set; }

    /// <summary>
    /// Gets the ban reason.
    /// </summary>
    public string? BanReason { get; private set; }

    /// <summary>
    /// Creates an admin user.
    /// </summary>
    public static Result<AdminUser> Create(string name, string email, int? roleId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<AdminUser>.Failure("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<AdminUser>.Failure("Email is required.");
        }

        return Result<AdminUser>.Success(new AdminUser
        {
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            RoleId = roleId,
        });
    }

    /// <summary>
    /// Marks the email as verified.
    /// </summary>
    public void MarkEmailVerified()
    {
        EmailVerified = true;
        Touch();
    }

    /// <summary>
    /// Assigns a role to the admin user.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    public void AssignRole(int? roleId)
    {
        RoleId = roleId;
        Touch();
    }
}
