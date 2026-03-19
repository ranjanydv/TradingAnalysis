namespace TradingAnalytics.Shared.Kernel.Interfaces;

/// <summary>
/// Writes audit information for security-sensitive or administrative actions.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs an audit entry.
    /// </summary>
    /// <param name="entry">The entry to log.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);
}

/// <summary>
/// Represents a single audit event.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the acting user identifier.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets or sets the acting user display name.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets or sets the acting user role.
    /// </summary>
    public string? UserRole { get; init; }

    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Gets or sets the module that produced the audit entry.
    /// </summary>
    public required string Module { get; init; }

    /// <summary>
    /// Gets or sets the outcome status.
    /// </summary>
    public string Status { get; init; } = "success";

    /// <summary>
    /// Gets or sets the affected resource identifier.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets or sets the affected resource type.
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Gets or sets the field-level changes.
    /// </summary>
    public List<AuditChange>? Changes { get; init; }

    /// <summary>
    /// Gets or sets the business reason for the action.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    public AuditRequestContext? Request { get; init; }

    /// <summary>
    /// Gets or sets the error context.
    /// </summary>
    public AuditErrorContext? Error { get; init; }
}

/// <summary>
/// Represents a single field change in an audit entry.
/// </summary>
/// <param name="Field">The field name.</param>
/// <param name="From">The previous value.</param>
/// <param name="To">The current value.</param>
public sealed record AuditChange(string Field, object? From, object? To);

/// <summary>
/// Represents HTTP request data captured for auditing.
/// </summary>
/// <param name="Ip">The client IP address.</param>
/// <param name="UserAgent">The client user agent.</param>
/// <param name="Endpoint">The request endpoint.</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="StatusCode">The response status code.</param>
/// <param name="DurationMs">The request duration in milliseconds.</param>
public sealed record AuditRequestContext(
    string? Ip,
    string? UserAgent,
    string? Endpoint,
    string? Method,
    int? StatusCode,
    long? DurationMs);

/// <summary>
/// Represents error details captured for auditing.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="Stack">The error stack trace.</param>
public sealed record AuditErrorContext(string? Code, string? Message, string? Stack);
