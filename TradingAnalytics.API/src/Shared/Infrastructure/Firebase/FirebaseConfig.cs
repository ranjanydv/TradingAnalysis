using System.ComponentModel.DataAnnotations;

namespace TradingAnalytics.Shared.Infrastructure.Firebase;

/// <summary>
/// Represents Firebase configuration values.
/// </summary>
public sealed class FirebaseConfig
{
    /// <summary>
    /// Gets or sets the service-account credential file path.
    /// </summary>
    [Required]
    public string CredentialFilePath { get; set; } = "firebase-service-account.json";
}
