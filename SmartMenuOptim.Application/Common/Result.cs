/*
 * File: Result.cs
 * Result pattern implementation for operation outcomes
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Encapsulates operation outcomes with success/failure semantics,
 * following the Result Pattern to avoid exceptions for expected failures.
 * 
 * Design Patterns:
 * - Result Pattern: Encapsulates success/failure without exceptions
 * - Railway Oriented Programming: Allows chaining operations
 */

namespace SmartMenuOptim.Application.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <remarks>
/// <para><strong>Result Pattern Benefits:</strong></para>
/// <list type="bullet">
///   <item><description>Explicit success/failure handling</description></item>
///   <item><description>No exceptions for expected business failures</description></item>
///   <item><description>Clear error messages without stack traces</description></item>
///   <item><description>Composable operation chains</description></item>
/// </list>
/// </remarks>
public class Result
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error message if the operation failed.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Protected constructor for Result.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error message if failed.</param>
    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A successful result cannot have an error message.");

        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A failed result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result.</returns>
    public static Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result.</returns>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful Result with the value.</returns>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>
    /// Creates a failed result with a value type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result.</returns>
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// <para><strong>Usage:</strong></para>
/// <code>
/// var result = await service.GetByIdAsync(id);
/// if (result.IsSuccess)
/// {
///     var entity = result.Value;
///     // Use entity
/// }
/// else
/// {
///     var error = result.Error;
///     // Handle error
/// }
/// </code>
/// </remarks>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessing value on a failed result.</exception>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access value of a failed result. Check IsSuccess first.");

            return _value!;
        }
    }

    /// <summary>
    /// Private constructor for Result with value.
    /// </summary>
    /// <param name="value">The value if successful.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error message if failed.</param>
    private Result(T? value, bool isSuccess, string error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful Result containing the value.</returns>
    public static Result<T> Success(T value) => new(value, true, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result.</returns>
    public new static Result<T> Failure(string error) => new(default, false, error);

    /// <summary>
    /// Implicit conversion to bool for easy success checks.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}

