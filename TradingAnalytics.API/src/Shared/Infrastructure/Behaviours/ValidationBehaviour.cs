using FluentValidation;
using MediatR;
using TradingAnalytics.Shared.Kernel.Results;

namespace TradingAnalytics.Shared.Infrastructure.Behaviours;

/// <summary>
/// Applies FluentValidation validators to MediatR requests.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators ?? throw new ArgumentNullException(nameof(validators));

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(static result => result.Errors)
            .Where(static error => error is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var errors = string.Join("; ", failures.Select(static error => error.ErrorMessage));

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod(nameof(Result<object>.Failure), [typeof(string)]);
            if (failureMethod is not null)
            {
                return (TResponse)failureMethod.Invoke(null, [errors])!;
            }
        }

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors);
        }

        throw new ValidationException(failures);
    }
}
