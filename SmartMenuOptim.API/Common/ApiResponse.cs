/*
 * File: ApiResponse.cs
 * Standardized API response wrapper
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Provides a consistent wrapper for API responses, enabling uniform
 * success and error response structures across all API endpoints.
 */

namespace SmartMenuOptim.API.Common;

/// <summary>
/// Standardized API response wrapper for successful operations.
/// </summary>
/// <typeparam name="T">The type of data being returned.</typeparam>
/// <remarks>
/// <para><strong>Response Structure:</strong></para>
/// <code>
/// {
///   "success": true,
///   "data": { ... },
///   "message": "Operation completed successfully"
/// }
/// </code>
/// </remarks>
public sealed record ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The response data (null if failed).
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Optional message providing additional context.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null) 
        => new() { Success = true, Data = data, Message = message };

    /// <summary>
    /// Creates a failed response with a message.
    /// </summary>
    public static ApiResponse<T> Fail(string message) 
        => new() { Success = false, Data = default, Message = message };
}

/// <summary>
/// Standardized API response without data payload.
/// </summary>
public sealed record ApiResponse
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Optional message providing additional context.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse Ok(string? message = null) 
        => new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed response with a message.
    /// </summary>
    public static ApiResponse Fail(string message) 
        => new() { Success = false, Message = message };
}
