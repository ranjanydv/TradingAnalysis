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

    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public bool EmailVerified { get; private set; }
    public string? Phone { get; private set; }
    public bool PhoneVerified { get; private set; }
    public string? Image { get; private set; }
    public int? RoleId { get; private set; }
    public bool Banned { get; private set; }
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

    public void MarkEmailVerified()
    {
        EmailVerified = true;
        Touch();
    }

    public void AssignRole(int? roleId)
    {
        RoleId = roleId;
        Touch();
    }
}
