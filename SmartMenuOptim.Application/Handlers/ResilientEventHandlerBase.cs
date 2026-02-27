using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Application.Handlers;

/// <summary>
/// Abstract base class providing resilient event handling with retry logic and dead letter queue support. It is designed to be inherited by specific event handlers to ensure consistent error handling and retry strategies across the application. This base class implements the Resilient Handler Pattern, allowing derived handlers to focus on their core processing logic while benefiting from built-in resilience features.
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
/// <remarks>
/// <para><strong>Pattern:</strong> Resilient Handler Pattern</para>
/// 
/// <para><strong>Features:</strong></para>
/// <list type="bullet">
///     <item><description>Exponential backoff retry strategy (3 attempts by default)</description></item>
///     <item><description>Dead letter queue for events that fail after all retries</description></item>
///     <item><description>Comprehensive error logging with structured context</description></item>
///     <item><description>Exception isolation to prevent cascade failures</description></item>
/// </list>
/// 
/// <para><strong>Retry Strategy:</strong></para>
/// <code>
/// Attempt 1: Immediate
/// Attempt 2: Wait 2 seconds (2^1)
/// Attempt 3: Wait 4 seconds (2^2)
/// Failure: Send to Dead Letter Queue
/// </code>
/// 
/// <para><strong>Usage:</strong></para>
/// <code>
/// public class MyHandler : ResilientEventHandlerBase&lt;MyEvent&gt;
/// {
///     protected override async Task ProcessEventAsync(MyEvent @event, CancellationToken ct)
///     {
///         // Your handler logic here
///     }
/// }
/// </code>
/// 
/// <para><strong>Related Documentation:</strong></para>
/// <para>See SmartMenuOptim.Application/docs/03-EventHandlers/EVENT_HANDLER_IMPLEMENTATION.md for handler patterns.</para>
/// </remarks>
public abstract class ResilientEventHandlerBase<TEvent> : INotificationHandler<TEvent>
    where TEvent : class, IDomainEvent
{
    private readonly ILogger _logger;
    private readonly IDeadLetterQueueService? _deadLetterQueue;
    
    /// <summary>
    /// Maximum number of retry attempts before sending to dead letter queue.
    /// </summary>
    protected virtual int MaxRetries => 3;
    
    /// <summary>
    /// Base delay in seconds for exponential backoff calculation.
    /// </summary>
    protected virtual int BaseDelaySeconds => 2;
    
    /// <summary>
    /// Gets the handler name for logging and dead letter queue tracking.
    /// </summary>
    protected virtual string HandlerName => GetType().Name;
    
    /// <summary>
    /// Initializes a new instance of the resilient event handler.
    /// </summary>
    /// <param name="logger">Logger instance for this handler.</param>
    /// <param name="deadLetterQueue">Optional dead letter queue service (null in development).</param>
    protected ResilientEventHandlerBase(
        ILogger logger,
        IDeadLetterQueueService? deadLetterQueue = null)
    {
        _logger = logger;
        _deadLetterQueue = deadLetterQueue;
    }
    
    /// <summary>
    /// Handles the event with retry logic and error isolation.
    /// </summary>
    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        var retryCount = 0;
        Exception? lastException = null;
        
        while (retryCount < MaxRetries)
        {
            try
            {
                await ProcessEventAsync(notification, cancellationToken);
                
                if (retryCount > 0)
                {
                    _logger.LogInformation(
                        "{Handler} succeeded on retry attempt {Attempt} for EventId={EventId}",
                        HandlerName,
                        retryCount + 1,
                        notification.EventId);
                }
                
                return; // Success - exit the retry loop
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancellation requested - don't retry, rethrow to allow proper cleanup
                _logger.LogWarning(
                    "{Handler} cancelled for EventId={EventId}",
                    HandlerName,
                    notification.EventId);
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                
                _logger.LogWarning(ex,
                    "{Handler} failed on attempt {Attempt}/{MaxRetries} for EventId={EventId}. Error: {Error}",
                    HandlerName,
                    retryCount,
                    MaxRetries,
                    notification.EventId,
                    ex.Message);
                
                // Don't delay on the last retry attempt
                if (retryCount < MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(BaseDelaySeconds, retryCount));
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        
        // All retries exhausted - log error and send to dead letter queue
        _logger.LogError(lastException,
            "{Handler} failed after {MaxRetries} attempts for EventId={EventId}. Sending to Dead Letter Queue.",
            HandlerName,
            MaxRetries,
            notification.EventId);
        
        await SendToDeadLetterQueueAsync(notification, lastException!, retryCount, cancellationToken);
    }
    
    /// <summary>
    /// Implement this method with the actual event processing logic.
    /// </summary>
    /// <param name="event">The domain event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// This method should throw exceptions for transient failures that should be retried.
    /// For permanent failures that should not be retried, catch and log the exception,
    /// then return without throwing.
    /// </remarks>
    protected abstract Task ProcessEventAsync(TEvent @event, CancellationToken cancellationToken);
    
    /// <summary>
    /// Sends the failed event to the dead letter queue.
    /// </summary>
    private async Task SendToDeadLetterQueueAsync(
        TEvent @event,
        Exception exception,
        int retryCount,
        CancellationToken cancellationToken)
    {
        if (_deadLetterQueue == null)
        {
            _logger.LogWarning(
                "Dead Letter Queue service not available. Failed event EventId={EventId} will not be persisted for later processing.",
                @event.EventId);
            return;
        }
        
        try
        {
            var failedEvent = new FailedDomainEvent
            {
                Event = @event,
                EventTypeName = @event.GetType().FullName ?? @event.GetType().Name,
                HandlerName = HandlerName,
                FailedAt = DateTime.UtcNow,
                ErrorMessage = exception.Message,
                ExceptionDetails = exception.ToString(),
                RetryCount = retryCount
            };
            
            await _deadLetterQueue.EnqueueAsync(failedEvent, cancellationToken);
            
            _logger.LogInformation(
                "Failed event EventId={EventId} sent to Dead Letter Queue with FailedEventId={FailedEventId}",
                @event.EventId,
                failedEvent.Id);
        }
        catch (Exception dlqEx)
        {
            _logger.LogError(dlqEx,
                "Failed to send event EventId={EventId} to Dead Letter Queue. Original error: {OriginalError}",
                @event.EventId,
                exception.Message);
        }
    }
}
