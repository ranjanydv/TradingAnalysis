namespace TradingAnalytics.Shared.Kernel.Results;

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation succeeded.</param>
    /// <param name="value">The resulting value when successful.</param>
    /// <param name="error">The error message when failed.</param>
    protected Result(bool isSuccess, T? value, string? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the returned value when the operation succeeds.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message when the operation fails.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string error) => new(false, default, error);
}

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// </summary>
public sealed class Result : Result<Unit>
{
    private Result(bool isSuccess, string? error)
        : base(isSuccess, Unit.Value, error)
    {
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static new Result Failure(string error) => new(false, error);
}

/// <summary>
/// Represents the absence of a specific value.
/// </summary>
public sealed record Unit
{
    /// <summary>
    /// Gets the singleton unit value.
    /// </summary>
    public static readonly Unit Value = new();
}
