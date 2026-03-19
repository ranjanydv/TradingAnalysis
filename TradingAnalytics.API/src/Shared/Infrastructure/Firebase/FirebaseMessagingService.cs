using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace TradingAnalytics.Shared.Infrastructure.Firebase;

/// <summary>
/// Implements Firebase push messaging.
/// </summary>
public sealed class FirebaseMessagingService(ILogger<FirebaseMessagingService> logger) : IFirebaseMessagingService
{
    private readonly ILogger<FirebaseMessagingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<string?> SendAsync(FcmMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogDebug("FCM recipient lookup for {RecipientId} is deferred until device entities are introduced.", message.RecipientId);
        await Task.CompletedTask.ConfigureAwait(false);
        return null;
    }

    /// <inheritdoc />
    public async Task<List<string>> SendMulticastAsync(List<string> tokens, FcmMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(message);

        if (tokens.Count == 0)
        {
            return [];
        }

        try
        {
            var result = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(
                new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = message.Title,
                        Body = message.Body,
                        ImageUrl = message.ImageUrl,
                    },
                    Data = message.Data ?? []
                },
                ct).ConfigureAwait(false);

            var failedTokens = new List<string>();
            for (var index = 0; index < result.Responses.Count; index++)
            {
                if (!result.Responses[index].IsSuccess)
                {
                    failedTokens.Add(tokens[index]);
                }
            }

            return failedTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FCM multicast failed.");
            return tokens;
        }
    }
}
