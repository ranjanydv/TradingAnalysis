namespace TradingAnalytics.Shared.Kernel.Extensions;

/// <summary>
/// Provides string transformation helpers used across the application.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The snake_case value.</returns>
    public static string? ToSnakeCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? "_" + char.ToLowerInvariant(character)
                : char.ToLowerInvariant(character).ToString()));
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The PascalCase value.</returns>
    public static string? ToPascalCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..].ToLowerInvariant()));
    }
}
