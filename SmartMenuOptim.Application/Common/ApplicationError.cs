/*
 * File: ApplicationError.cs
 * Application-level error representation
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Represents an application error with code, message, and type for consistent error handling.
 * Maps from Domain errors and provides context for API responses.
 */

using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Application.Common;

/// <summary>
/// Represents an application-level error with code, message, and type.
/// </summary>
/// <remarks>
/// <para><strong>Error Types:</strong></para>
/// <list type="bullet">
///   <item><description>NotFound: Resource does not exist (404)</description></item>
///   <item><description>Validation: Input validation failed (400)</description></item>
///   <item><description>BusinessRule: Business rule violation (422)</description></item>
///   <item><description>Conflict: Resource conflict (409)</description></item>
///   <item><description>Unexpected: Unexpected error (500)</description></item>
/// </list>
/// </remarks>
public sealed record ApplicationError
{
    /// <summary>
    /// The error code identifying the type of error.
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// A human-readable description of the error.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// The type of error for HTTP status code mapping.
    /// </summary>
    public ErrorType Type { get; init; }

    /// <summary>
    /// Initializes a new ApplicationError.
    /// </summary>
    public ApplicationError(string code, string message, ErrorType type = ErrorType.Unexpected)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a Not Found error.
    /// </summary>
    public static ApplicationError NotFound(string entity, object id) 
        => new($"{entity}.NotFound", $"{entity} with ID '{id}' was not found.", ErrorType.NotFound);

    /// <summary>
    /// Creates a Not Found error with custom message.
    /// </summary>
    public static ApplicationError NotFound(string code, string message) 
        => new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates a Validation error.
    /// </summary>
    public static ApplicationError Validation(string code, string message) 
        => new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a Business Rule violation error.
    /// </summary>
    public static ApplicationError BusinessRule(string code, string message) 
        => new(code, message, ErrorType.BusinessRule);

    /// <summary>
    /// Creates a Conflict error.
    /// </summary>
    public static ApplicationError Conflict(string code, string message) 
        => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an Unexpected error.
    /// </summary>
    public static ApplicationError Unexpected(string message) 
        => new("General.UnexpectedError", message, ErrorType.Unexpected);

    /// <summary>
    /// Creates an error from a Domain error.
    /// </summary>
    public static ApplicationError FromDomainError(DomainError domainError, ErrorType type = ErrorType.BusinessRule)
        => new(domainError.Code, domainError.Message, type);

    // ═══════════════════════════════════════════════════════════════════════
    // CONVERSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Implicit conversion to string returns the message.
    /// </summary>
    public static implicit operator string(ApplicationError error) => error.Message;

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {Message}";
}

/// <summary>
/// Defines the type of error for HTTP status code mapping.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Resource not found (HTTP 404).
    /// </summary>
    NotFound,

    /// <summary>
    /// Input validation failed (HTTP 400).
    /// </summary>
    Validation,

    /// <summary>
    /// Business rule violation (HTTP 422).
    /// </summary>
    BusinessRule,

    /// <summary>
    /// Resource conflict, such as duplicate (HTTP 409).
    /// </summary>
    Conflict,

    /// <summary>
    /// Unexpected error (HTTP 500).
    /// </summary>
    Unexpected
}
