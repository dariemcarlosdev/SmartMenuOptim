/*
 * File: DomainError.cs
 * Domain-level error representation
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Represents a domain error with a code and message for consistent error handling.
 */

namespace SmartMenuOptim.Domain.Common;

/// <summary>
/// Represents a domain error with a code and message.
/// </summary>
/// <remarks>
/// <para><strong>Error Codes Convention:</strong></para>
/// <list type="bullet">
///   <item><description>Format: {Aggregate}.{ErrorType} (e.g., "Menu.NotFound", "Restaurant.InvalidStatus")</description></item>
///   <item><description>Use PascalCase for code parts</description></item>
///   <item><description>Keep codes consistent across the domain</description></item>
/// </list>
/// </remarks>
/// <param name="Code">The error code identifying the type of error.</param>
/// <param name="Message">A human-readable description of the error.</param>
public sealed record DomainError(string Code, string Message)
{
    /// <summary>
    /// Creates a Not Found error for an entity.
    /// </summary>
    public static DomainError NotFound(string entity, object id) 
        => new($"{entity}.NotFound", $"{entity} with ID '{id}' was not found.");

    /// <summary>
    /// Creates a Validation error.
    /// </summary>
    public static DomainError Validation(string entity, string message) 
        => new($"{entity}.ValidationError", message);

    /// <summary>
    /// Creates an Invalid Operation error.
    /// </summary>
    public static DomainError InvalidOperation(string entity, string message) 
        => new($"{entity}.InvalidOperation", message);

    /// <summary>
    /// Creates a Conflict error (e.g., duplicate).
    /// </summary>
    public static DomainError Conflict(string entity, string message) 
        => new($"{entity}.Conflict", message);

    /// <summary>
    /// Creates a generic domain error.
    /// </summary>
    public static DomainError Create(string code, string message) 
        => new(code, message);

    /// <summary>
    /// Implicit conversion to string returns the message.
    /// </summary>
    public static implicit operator string(DomainError error) => error.Message;

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {Message}";
}
