using MediatR;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Infrastructure.EventDispatching;

/// <summary>
/// MediatR-based implementation of <see cref="IDomainEventDispatcher"/>.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This class serves as the bridge between aggregates that raise domain events and the
/// MediatR pipeline that routes events to their registered handlers.</para>
/// 
/// <para><strong>Integration:</strong></para>
/// <para>This dispatcher is called by the <c>AppDbContext.SaveChangesAsync()</c> method after
/// successful persistence to ensure events are only dispatched when changes are committed.</para>
/// 
/// <para><strong>Error Handling:</strong></para>
/// <para>Event handlers should be resilient and not throw exceptions. However, if handlers do throw,
/// this dispatcher logs the error and continues processing remaining events to prevent cascade failures.</para>
/// 
/// <para><strong>Related Documentation:</strong></para>
/// <para>See SmartMenuOptim.Application/docs/03-EventHandlers/EVENT_HANDLER_IMPLEMENTATION.md for handler patterns.</para>
/// <para>See SmartMenuOptim.Domain/docs/06-Events/DOMAIN_EVENTS_GUIDE.md for event specifications.</para>
/// </remarks>
public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediatRDomainEventDispatcher"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger for event dispatching diagnostics.</param>
    public MediatRDomainEventDispatcher(
        IMediator mediator,
        ILogger<MediatRDomainEventDispatcher> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();

        if (eventList.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "Dispatching {Count} domain events",
            eventList.Count);

        foreach (var domainEvent in eventList)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        _logger.LogDebug(
            "Completed dispatching {Count} domain events",
            eventList.Count);
    }

    /// <inheritdoc />
    public async Task DispatchEventAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        var eventTypeName = domainEvent.EventType;
        var eventId = domainEvent.EventId;

        _logger.LogDebug(
            "Dispatching domain event: {EventType}, EventId: {EventId}, OccurredOn: {OccurredOn}",
            eventTypeName,
            eventId,
            domainEvent.OccurredOn);

        try
        {
            // Publish to all handlers via MediatR
            await _mediator.Publish(domainEvent, cancellationToken);

            _logger.LogDebug(
                "Successfully dispatched event: {EventType}, EventId: {EventId}",
                eventTypeName,
                eventId);
        }
        catch (Exception ex)
        {
            // Log error but don't rethrow - event handlers should be resilient
            // Consider adding to a dead letter queue or retry mechanism
            _logger.LogError(ex,
                "Error dispatching domain event: {EventType}, EventId: {EventId}. Error: {Message}",
                eventTypeName,
                eventId,
                ex.Message);

            // In production, you might want to:
            // 1. Add to a retry queue
            // 2. Store in a dead letter table
            // 3. Trigger an alert
            // 4. Rethrow for critical events that must be processed
        }
    }
}
