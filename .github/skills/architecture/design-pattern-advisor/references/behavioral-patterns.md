# Behavioral Patterns — Strategy, Observer, Mediator, Command

## When to Consider Behavioral Patterns

- Algorithm or behavior varies based on context and should be swappable
- Objects need to communicate without tight coupling
- You need to encapsulate requests as objects for queuing, logging, or undo

## Strategy Pattern

**Intent:** Define a family of algorithms, encapsulate each one, and make them interchangeable.

**Use when:** You have a `switch` or `if-else` chain selecting behavior at runtime (≥3 branches).

### .NET Implementation — Escrow Fee Calculation

```csharp
// Strategy interface (Application layer)
public interface IFeeCalculationStrategy
{
    Money Calculate(Money transactionAmount);
    bool AppliesTo(EscrowType type);
}

// Concrete strategies
public sealed class StandardFeeStrategy : IFeeCalculationStrategy
{
    public bool AppliesTo(EscrowType type) => type == EscrowType.Standard;
    public Money Calculate(Money amount) => amount * 0.025m; // 2.5%
}

public sealed class PremiumFeeStrategy : IFeeCalculationStrategy
{
    public bool AppliesTo(EscrowType type) => type == EscrowType.Premium;
    public Money Calculate(Money amount) => amount * 0.015m; // 1.5%
}

// Strategy resolver
public sealed class FeeCalculator(IEnumerable<IFeeCalculationStrategy> strategies)
{
    public Money Calculate(EscrowType type, Money amount)
        => strategies.First(s => s.AppliesTo(type)).Calculate(amount);
}

// DI Registration — all strategies auto-discovered
services.AddScoped<IFeeCalculationStrategy, StandardFeeStrategy>();
services.AddScoped<IFeeCalculationStrategy, PremiumFeeStrategy>();
services.AddScoped<IFeeCalculationStrategy, EnterpriseFeeStrategy>();
services.AddScoped<FeeCalculator>();
```

## Observer Pattern (Domain Events)

**Intent:** Define a one-to-many dependency so that when one object changes state, all dependents are notified.

**In .NET/MediatR:** Domain Events implement the Observer pattern via `INotification`.

```csharp
// Domain Event (Domain layer)
public sealed record EscrowFundedEvent(EscrowTransactionId Id, Money Amount) : INotification;

// Raising the event (Domain entity)
public sealed class EscrowTransaction : AggregateRoot<EscrowTransactionId>
{
    public void Fund(Money amount)
    {
        Status = EscrowStatus.Funded;
        AddDomainEvent(new EscrowFundedEvent(Id, amount));
    }
}

// Handlers (observers) — each in its own file
public sealed class SendFundingConfirmation(INotificationService notifier)
    : INotificationHandler<EscrowFundedEvent>
{
    public async Task Handle(EscrowFundedEvent notification, CancellationToken ct)
        => await notifier.SendAsync(new FundingConfirmedEmail(notification.Id), ct);
}

public sealed class UpdateDashboardMetrics(IMetricsService metrics)
    : INotificationHandler<EscrowFundedEvent>
{
    public Task Handle(EscrowFundedEvent notification, CancellationToken ct)
    {
        metrics.IncrementFundedCount(notification.Amount);
        return Task.CompletedTask;
    }
}
```

## Mediator Pattern (MediatR / CQRS)

**Intent:** Define an object that encapsulates how a set of objects interact. Promotes loose coupling.

**In .NET:** MediatR is the standard Mediator implementation for CQRS.

```csharp
// Command (Application layer)
public sealed record CreateEscrowCommand(
    UserId BuyerId, UserId SellerId, Money Amount) : IRequest<Result<EscrowId>>;

// Handler
public sealed class CreateEscrowHandler(
    IEscrowRepository repository, IUnitOfWork uow)
    : IRequestHandler<CreateEscrowCommand, Result<EscrowId>>
{
    public async Task<Result<EscrowId>> Handle(
        CreateEscrowCommand request, CancellationToken ct)
    {
        var escrow = EscrowTransaction.Create(
            request.BuyerId, request.SellerId, request.Amount);

        await repository.AddAsync(escrow, ct);
        await uow.SaveChangesAsync(ct);

        return Result.Success(escrow.Id);
    }
}

// Client sends through Mediator — no direct handler dependency
var result = await mediator.Send(new CreateEscrowCommand(buyerId, sellerId, amount));
```

## Command Pattern

**Intent:** Encapsulate a request as an object, allowing parameterization, queuing, and undo.

**Use when:** You need audit trails, undo/redo, or command queuing.

```csharp
// Command with undo support for escrow operations
public interface IUndoableCommand
{
    Task ExecuteAsync(CancellationToken ct);
    Task UndoAsync(CancellationToken ct);
}

public sealed class ReleaseEscrowCommand(
    IEscrowRepository repo, EscrowTransactionId id) : IUndoableCommand
{
    private EscrowStatus _previousStatus;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var escrow = await repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(EscrowTransaction), id);
        _previousStatus = escrow.Status;
        escrow.Release();
    }

    public async Task UndoAsync(CancellationToken ct)
    {
        var escrow = await repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(EscrowTransaction), id);
        escrow.RevertTo(_previousStatus);
    }
}
```

## Decision Matrix

| Problem | Pattern | Complexity | When to Use |
|---------|---------|-----------|-------------|
| Runtime algorithm selection | Strategy | Low | 3+ fee calcs, validators, formatters |
| React to state changes | Observer/Events | Low-Medium | Domain events, notifications |
| Decouple request/handler | Mediator (MediatR) | Low | CQRS commands and queries |
| Encapsulate operations | Command | Medium | Audit trails, undo/redo, queuing |
| Chain of processing | Chain of Responsibility | Medium | Validation pipelines, middleware |
