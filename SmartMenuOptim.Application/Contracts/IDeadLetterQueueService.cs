using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Service for managing events that fail after retry attempts. It provides methods to enqueue failed events, retrieve them for review, and retry or resolve them.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>Dead Letter Queue (DLQ) stores events that could not be processed successfully
/// after all retry attempts have been exhausted. This enables:</para>
/// <list type="bullet">
///     <item><description>Manual review and reprocessing of failed events</description></item>
///     <item><description>Analysis of failure patterns for system improvement</description></item>
///     <item><description>Preservation of data for audit and compliance</description></item>
/// </list>
/// 
/// <para><strong>Implementation Notes:</strong></para>
/// <para>Development: In-memory or database-backed implementation</para>
/// <para>Production: Azure Service Bus DLQ, Amazon SQS DLQ, or similar</para>
/// </remarks>
public interface IDeadLetterQueueService
{
    /// <summary>
    /// Enqueues a failed event to the dead letter queue.
    /// </summary>
    /// <param name="failedEvent">The failed event with context information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(FailedDomainEvent failedEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all failed events for review.
    /// </summary>
    /// <param name="limit">Maximum number of events to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<FailedDomainEvent>> GetFailedEventsAsync(int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retries a failed event by its ID.
    /// </summary>
    /// <param name="failedEventId">The ID of the failed event to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> RetryEventAsync(Guid failedEventId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a failed event as resolved.
    /// </summary>
    /// <param name="failedEventId">The ID of the failed event.</param>
    /// <param name="resolution">Description of how the event was resolved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsResolvedAsync(Guid failedEventId, string resolution, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a domain event that failed to process.
/// </summary>
public class FailedDomainEvent
{
    /// <summary>
    /// Unique identifier for this failed event record.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The original domain event that failed.
    /// </summary>
    public IDomainEvent Event { get; init; } = null!;
    
    /// <summary>
    /// The fully qualified type name of the event.
    /// </summary>
    public string EventTypeName { get; init; } = string.Empty;
    
    /// <summary>
    /// The name of the handler that failed.
    /// </summary>
    public string HandlerName { get; init; } = string.Empty;
    
    /// <summary>
    /// When the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// The error message from the final failure.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
    
    /// <summary>
    /// The full exception details including stack trace.
    /// </summary>
    public string ExceptionDetails { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of retry attempts before failure.
    /// </summary>
    public int RetryCount { get; init; }
    
    /// <summary>
    /// Whether this event has been resolved.
    /// </summary>
    public bool IsResolved { get; set; }
    
    /// <summary>
    /// When the event was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// How the event was resolved.
    /// </summary>
    public string? Resolution { get; set; }
}
