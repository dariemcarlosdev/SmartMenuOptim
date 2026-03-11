/*
 * File: DomainResult.cs
 * Domain-level Result pattern for operation outcomes
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides a standardized way to represent operation outcomes in the Domain layer
 * without throwing exceptions for expected business failures.
 * 
 * Design Patterns:
 * - Result Pattern: Encapsulates success/failure semantics
 * - Railway Oriented Programming: Enables operation chaining
 */

namespace SmartMenuOptim.Domain.Common;

/// <summary>
/// Represents the result of a domain operation that can succeed or fail.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Used in the Domain layer for operations that can fail due to business rule violations,
/// allowing the caller to handle failures explicitly without exceptions.</para>
/// 
/// <para><strong>When to Use:</strong></para>
/// <list type="bullet">
///   <item><description>Domain service operations that may fail</description></item>
///   <item><description>Aggregate method results with validation</description></item>
///   <item><description>Value Object creation with validation</description></item>
/// </list>
/// </remarks>
public class DomainResult
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
    /// The error information if the operation failed.
    /// </summary>
    public DomainError? Error { get; }

    /// <summary>
    /// Protected constructor for DomainResult.
    /// </summary>
    protected DomainResult(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot have an error.");

        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DomainResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    public static DomainResult Failure(DomainError error) => new(false, error);

    /// <summary>
    /// Creates a failed result with a code and message.
    /// </summary>
    public static DomainResult Failure(string code, string message) 
        => new(false, new DomainError(code, message));

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static DomainResult<T> Success<T>(T value) => DomainResult<T>.Success(value);

    /// <summary>
    /// Creates a failed result for a specific type.
    /// </summary>
    public static DomainResult<T> Failure<T>(DomainError error) => DomainResult<T>.Failure(error);

    /// <summary>
    /// Creates a failed result for a specific type with code and message.
    /// </summary>
    public static DomainResult<T> Failure<T>(string code, string message) 
        => DomainResult<T>.Failure(new DomainError(code, message));
}

/// <summary>
/// Represents the result of a domain operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class DomainResult<T> : DomainResult
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
                throw new InvalidOperationException("Cannot access value of a failed result.");
            return _value!;
        }
    }

    private DomainResult(T? value, bool isSuccess, DomainError? error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static DomainResult<T> Success(T value) => new(value, true, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static DomainResult<T> Failure(DomainError error) => new(default, false, error);

    /// <summary>
    /// Creates a failed result with code and message.
    /// </summary>
    public new static DomainResult<T> Failure(string code, string message)
        => new(default, false, new DomainError(code, message));

    /// <summary>
    /// Implicit conversion to the value type (throws if failed).
    /// </summary>
    public static implicit operator T(DomainResult<T> result) => result.Value;
}
