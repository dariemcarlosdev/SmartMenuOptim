/*
 * File: ClientResult.cs
 * Client-side Result pattern for Blazor components
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides a standardized way to handle API responses in Blazor components,
 * encapsulating success/failure states with user-friendly error messages.
 */

namespace SmartMenuOptim.Server.Common;

/// <summary>
/// Represents the result of a client-side operation.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Used in Blazor components and client services to handle API responses
/// with consistent success/failure semantics and user-friendly messages.</para>
/// </remarks>
public class HttpClientResult
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
    /// The error code if available.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Indicates if the error is a Not Found (404) error.
    /// </summary>
    public bool IsNotFound => ErrorCode?.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase) == true
        || Error.Contains("not found", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates if the error is a validation error.
    /// </summary>
    public bool IsValidationError => ErrorCode?.Contains("Validation", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Protected constructor.
    /// </summary>
    protected HttpClientResult(bool isSuccess, string error, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static HttpClientResult Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HttpClientResult Failure(string error, string? errorCode = null) 
        => new(false, error, errorCode);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static ClientResult<T> Success<T>(T value) => ClientResult<T>.Success(value);

    /// <summary>
    /// Creates a failed result with a value type.
    /// </summary>
    public static ClientResult<T> Failure<T>(string error, string? errorCode = null) 
        => ClientResult<T>.Failure(error, errorCode);
}

/// <summary>
/// Represents the result of a client-side operation that returns a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class ClientResult<T> : HttpClientResult
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access value of a failed result.");
            return _value!;
        }
    }

    /// <summary>
    /// Gets the value or default if failed.
    /// </summary>
    public T? ValueOrDefault => _value;

    private ClientResult(T? value, bool isSuccess, string error, string? errorCode = null) 
        : base(isSuccess, error, errorCode)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static ClientResult<T> Success(T value) => new(value, true, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public new static ClientResult<T> Failure(string error, string? errorCode = null) 
        => new(default, false, error, errorCode);

    /// <summary>
    /// Implicit conversion to bool for easy success checks.
    /// </summary>
    public static implicit operator bool(ClientResult<T> result) => result.IsSuccess;
}
