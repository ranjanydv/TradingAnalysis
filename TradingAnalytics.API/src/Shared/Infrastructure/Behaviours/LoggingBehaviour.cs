using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TradingAnalytics.Shared.Infrastructure.Behaviours;

/// <summary>
/// Logs MediatR request execution time.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("Slow handler {Request} took {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogDebug("Handled {Request} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
