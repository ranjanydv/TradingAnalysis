using TradingAnalytics.Modules.Identity.Domain.Events;
using TradingAnalytics.Shared.Kernel.Entities;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a customer actor.
/// </summary>
public sealed class Customer : AggregateRoot
{
    private Customer()
    {
    }

    /// <summary>
    /// Gets the customer name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the email address.
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
    /// Gets the customer image URL.
    /// </summary>
    public string? Image { get; private set; }

    /// <summary>
    /// Gets the role identifier.
    /// </summary>
    public int? RoleId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the customer is banned.
    /// </summary>
    public bool Banned { get; private set; }

    /// <summary>
    /// Gets the ban reason.
    /// </summary>
    public string? BanReason { get; private set; }

    /// <summary>
    /// Creates a customer.
    /// </summary>
    public static Result<Customer> Create(string name, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Customer>.Failure("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            return Result<Customer>.Failure("Email or phone is required.");
        }

        var customer = new Customer
        {
            Name = name.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        };
        customer.RaiseDomainEvent(new CustomerRegisteredEvent(customer.Id, customer.Email, customer.Phone));
        return Result<Customer>.Success(customer);
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
    /// Marks the phone as verified.
    /// </summary>
    public void MarkPhoneVerified()
    {
        PhoneVerified = true;
        Touch();
    }

    /// <summary>
    /// Updates profile values.
    /// </summary>
    public Result UpdateProfile(string name, string? image)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure("Name is required.");
        }

        Name = name.Trim();
        Image = string.IsNullOrWhiteSpace(image) ? null : image.Trim();
        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Bans the customer.
    /// </summary>
    public Result Ban(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Ban reason is required.");
        }

        if (Banned)
        {
            return Result.Failure("Customer is already banned.");
        }

        Banned = true;
        BanReason = reason.Trim();
        Touch();
        RaiseDomainEvent(new CustomerBannedEvent(Id, BanReason));
        return Result.Success();
    }

    /// <summary>
    /// Removes a ban from the customer.
    /// </summary>
    public Result Unban()
    {
        if (!Banned)
        {
            return Result.Failure("Customer is not banned.");
        }

        Banned = false;
        BanReason = null;
        Touch();
        return Result.Success();
    }
}
