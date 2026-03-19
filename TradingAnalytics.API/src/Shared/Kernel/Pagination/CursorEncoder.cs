using System.Text;
using System.Text.Json;

namespace TradingAnalytics.Shared.Kernel.Pagination;

/// <summary>
/// Encodes and decodes cursor payloads.
/// </summary>
public static class CursorEncoder
{
    /// <summary>
    /// Encodes a cursor identifier.
    /// </summary>
    /// <param name="id">The identifier to encode.</param>
    /// <returns>A base64url-encoded cursor.</returns>
    public static string Encode(Guid id)
    {
        var json = JsonSerializer.Serialize(new CursorPayload(id.ToString()));
        return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>
    /// Decodes the identifier from a cursor.
    /// </summary>
    /// <param name="cursor">The encoded cursor.</param>
    /// <returns>The decoded identifier, or <see langword="null"/> when invalid.</returns>
    public static Guid? DecodeId(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var bytes = Base64UrlDecode(cursor);
            var payload = JsonSerializer.Deserialize<CursorPayload>(bytes);
            return payload is not null && Guid.TryParse(payload.Id, out var id) ? id : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        return Convert.FromBase64String(normalized);
    }

    private sealed record CursorPayload(string Id);
}
