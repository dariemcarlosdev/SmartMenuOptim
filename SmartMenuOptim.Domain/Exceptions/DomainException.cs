namespace SmartMenuOptim.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific business rule violations in the SmartMenuOptimizer application.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Provides a base exception type for distinguishing domain/business rule violations from
/// infrastructure or application-level errors. All domain-specific exceptions should inherit from
/// this class to enable consistent error handling and meaningful error responses.</para>
///
/// <para><strong>Clean Architecture Role:</strong></para>
/// <para>Lives in the Domain layer as part of the domain model. The Infrastructure layer
/// (e.g., <c>GlobalExceptionHandlingMiddleware</c>) catches this exception type to return
/// appropriate HTTP 422 (Unprocessable Entity) responses, distinguishing business rule
/// violations from generic 400 Bad Request errors.</para>
///
/// <para><strong>Usage Guidelines:</strong></para>
/// <list type="bullet">
///   <item><description>Use for business rule violations that are expected and meaningful to the domain</description></item>
///   <item><description>Prefer specific derived exceptions (e.g., <see cref="OrderDomainException"/>) over the base class</description></item>
///   <item><description>Include a clear, user-friendly message describing the violated business rule</description></item>
///   <item><description>Do not use for infrastructure failures (network, database) or programming errors</description></item>
/// </list>
///
/// <para><strong>Example:</strong></para>
/// <code>
/// throw new DomainException("Cannot perform this operation because the entity is in an invalid state.");
/// </code>
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">A message describing the business rule violation.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message
    /// and a reference to the inner exception that caused this exception.
    /// </summary>
    /// <param name="message">A message describing the business rule violation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
