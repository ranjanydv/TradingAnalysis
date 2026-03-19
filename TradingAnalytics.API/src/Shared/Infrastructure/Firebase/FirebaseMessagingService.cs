using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingAnalytics.Modules.Identity.Domain.Entities;
using TradingAnalytics.Shared.Infrastructure.Persistence;

namespace TradingAnalytics.Shared.Infrastructure.Firebase;

/// <summary>
/// Implements Firebase push messaging.
/// </summary>
public sealed class FirebaseMessagingService(
    ILogger<FirebaseMessagingService> logger,
    AppDbContext dbContext) : IFirebaseMessagingService
{
    private readonly ILogger<FirebaseMessagingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<string?> SendAsync(FcmMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var tokens = await _dbContext.Set<UserDevice>()
            .Where(device => device.CustomerId.ToString() == message.RecipientId && device.IsActive && device.FcmToken != null)
            .Select(device => device.FcmToken!)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (tokens.Count == 0)
        {
            return null;
        }

        var failed = await SendMulticastAsync(tokens, message, ct).ConfigureAwait(false);
        return failed.Count < tokens.Count ? "sent" : null;
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
