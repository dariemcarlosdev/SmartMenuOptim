using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using System.Collections.Concurrent;

namespace SmartMenuOptim.Infrastructure.Services.DeadLetterQueue;

/// <summary>
/// In-memory implementation of <see cref="IDeadLetterQueueService"/> for development and testing.
/// </summary>
/// <remarks>
/// <para><strong>Development Only:</strong></para>
/// <para>This implementation stores failed events in memory and is suitable for development
/// and testing scenarios. For production, use a durable implementation backed by Azure
/// Service Bus, Amazon SQS, Redis, or a database to ensure events are not lost on application restart and can be processed reliably.</para>
/// 
/// <para><strong>Production Alternatives:</strong></para>
/// <list type="bullet">
///     <item><description>Azure Service Bus Dead Letter Queue</description></item>
///     <item><description>Amazon SQS Dead Letter Queue</description></item>
///     <item><description>Database-backed DLQ with background processing</description></item>
///     <item><description>Redis-backed DLQ</description></item>
/// </list>
/// 
/// <para><strong>Limitations:</strong></para>
/// <list type="bullet">
///     <item><description>Events are lost on application restart</description></item>
///     <item><description>Not suitable for distributed scenarios</description></item>
///     <item><description>Memory consumption grows with failed events</description></item>
/// </list>
/// </remarks>
public class InMemoryDeadLetterQueueService : IDeadLetterQueueService
{
    private readonly ConcurrentDictionary<Guid, FailedDomainEvent> _failedEvents = new();
    private readonly ILogger<InMemoryDeadLetterQueueService> _logger;

    public InMemoryDeadLetterQueueService(ILogger<InMemoryDeadLetterQueueService> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Asynchronously adds a failed domain event to the dead letter queue for later inspection or reprocessing.
    /// </summary>
    /// <remarks>The method logs a warning when an event is added to the dead letter queue. The dead letter
    /// queue is intended for events that could not be processed successfully and require manual intervention or later
    /// retries.</remarks>
    /// <param name="failedEvent">The failed domain event to enqueue. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the enqueue operation.</param>
    /// <returns>A task that represents the asynchronous enqueue operation.</returns>
    public Task EnqueueAsync(FailedDomainEvent failedEvent, CancellationToken cancellationToken = default)
    {
        _failedEvents.TryAdd(failedEvent.Id, failedEvent);
        
        _logger.LogWarning(
            "Event added to Dead Letter Queue. FailedEventId={FailedEventId}, EventType={EventType}, " +
            "Handler={Handler}, Error={Error}, RetryCount={RetryCount}",
            failedEvent.Id,
            failedEvent.EventTypeName,
            failedEvent.HandlerName,
            failedEvent.ErrorMessage,
            failedEvent.RetryCount);
        
        _logger.LogDebug(
            "Dead Letter Queue now contains {Count} failed events",
            _failedEvents.Count);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously retrieves a list of unresolved failed domain events, ordered by most recent failure.
    /// </summary>
    /// <param name="limit">The maximum number of failed events to return. Must be greater than zero. The default is 100.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of unresolved
    /// failed domain events, ordered from most recent to oldest. The list may be empty if there are no unresolved
    /// failed events.</returns>
    public Task<IReadOnlyList<FailedDomainEvent>> GetFailedEventsAsync(
        int limit = 100, 
        CancellationToken cancellationToken = default)
    {
        var events = _failedEvents.Values
            .Where(e => !e.IsResolved)
            .OrderByDescending(e => e.FailedAt)
            .Take(limit)
            .ToList();
        
        return Task.FromResult<IReadOnlyList<FailedDomainEvent>>(events);
    }

    /// <summary>
    /// Attempts to retry processing of a previously failed event identified by its unique identifier.
    /// </summary>
    /// <remarks>Use this method to trigger a retry for an event that previously failed processing. If the
    /// specified event does not exist, the method returns <see langword="false"/> and no action is taken.</remarks>
    /// <param name="failedEventId">The unique identifier of the failed event to retry.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the retry operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the failed event
    /// was found and the retry was initiated; otherwise, <see langword="false"/>.</returns>
    public Task<bool> RetryEventAsync(Guid failedEventId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would:
        // 1. Retrieve the event
        // 2. Re-publish it to MediatR
        // 3. Mark as resolved on success or increment retry count on failure
        
        if (_failedEvents.TryGetValue(failedEventId, out var failedEvent))
        {
            _logger.LogInformation(
                "Retry requested for FailedEventId={FailedEventId}, EventType={EventType}",
                failedEventId,
                failedEvent.EventTypeName);
            
            // This is a placeholder - in production, inject IMediator and republish
            return Task.FromResult(true);
        }
        
        _logger.LogWarning(
            "Retry requested for non-existent FailedEventId={FailedEventId}",
            failedEventId);
        
        return Task.FromResult(false);
    }

    /// <summary>
    /// Marks the specified failed event as resolved with the provided resolution message.
    /// </summary>
    /// <remarks>If the specified failed event does not exist, no action is taken and a warning is logged. The
    /// method completes synchronously.</remarks>
    /// <param name="failedEventId">The unique identifier of the failed event to mark as resolved.</param>
    /// <param name="resolution">A description of how the failed event was resolved. This information is recorded for auditing or troubleshooting
    /// purposes.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. The default value is <see
    /// cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task MarkAsResolvedAsync(
        Guid failedEventId, 
        string resolution, 
        CancellationToken cancellationToken = default)
    {
        if (_failedEvents.TryGetValue(failedEventId, out var failedEvent))
        {
            failedEvent.IsResolved = true;
            failedEvent.ResolvedAt = DateTime.UtcNow;
            failedEvent.Resolution = resolution;
            
            _logger.LogInformation(
                "Failed event marked as resolved. FailedEventId={FailedEventId}, Resolution={Resolution}",
                failedEventId,
                resolution);
        }
        else
        {
            _logger.LogWarning(
                "Could not mark non-existent event as resolved. FailedEventId={FailedEventId}",
                failedEventId);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the number of failed events that have not been resolved.
    /// </summary>
    /// <returns>The total count of unresolved failed events. Returns 0 if there are no unresolved events.</returns>
    public int GetUnresolvedCount()
    {
        return _failedEvents.Values.Count(e => !e.IsResolved);
    }
    
    /// <summary>
    /// Removes all events from the dead letter queue.
    /// </summary>
    /// <remarks>Use this method to reset the dead letter queue to an empty state. After calling this method,
    /// any previously failed events will no longer be available for inspection or reprocessing.</remarks>
    public void Clear()
    {
        _failedEvents.Clear();
        _logger.LogDebug("Dead Letter Queue cleared");
    }
}
