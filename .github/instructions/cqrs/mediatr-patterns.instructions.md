---
applyTo: "EscrowApp/Features/**/*.cs"
---

# MediatR & CQRS Patterns — NexTruzt.io EscrowApp

## Vertical Slice Structure

Each use case is a self-contained slice within `Features/Escrow/`:

```
Features/
└── Escrow/
    ├── HoldFunds/
    │   ├── HoldFundsCommand.cs          ← IRequest<HoldFundsResult>
    │   ├── HoldFundsCommandValidator.cs ← FluentValidation
    │   ├── HoldFundsHandler.cs          ← IRequestHandler<,>
    │   └── HoldFundsResult.cs           ← Result DTO
    ├── ReleaseFunds/
    │   ├── ReleaseFundsCommand.cs
    │   ├── ReleaseFundsCommandValidator.cs
    │   ├── ReleaseFundsHandler.cs
    │   └── ReleaseFundsResult.cs
    ├── DisputeFunds/
    │   ├── DisputeFundsCommand.cs
    │   ├── DisputeFundsCommandValidator.cs
    │   ├── DisputeFundsHandler.cs
    │   └── DisputeFundsResult.cs
    └── GetTransactions/
        ├── GetEscrowTransactionsQuery.cs
        ├── GetEscrowTransactionsHandler.cs
        └── EscrowTransactionDto.cs
```

**One command/query, one handler, one result per folder.** No shared handlers.

---

## Command vs Query Separation

| Aspect | Command (Write) | Query (Read) |
|---|---|---|
| Purpose | Mutate state | Return data |
| Naming | `{Verb}{Noun}Command` | `Get{Noun}Query` / `List{Noun}Query` |
| Returns | Result DTO with success/error | DTO or collection |
| Side effects | Yes — DB writes, events, payments | None — read-only |
| Validation | Always — FluentValidation required | Optional |
| Idempotency | Required for payment commands | N/A |
| EF Tracking | Default tracking | `AsNoTracking()` |

**Examples:**
- Commands: `HoldFundsCommand`, `ReleaseFundsCommand`, `DisputeFundsCommand`, `CancelEscrowCommand`
- Queries: `GetEscrowTransactionsQuery`, `GetTransactionByIdQuery`, `ListDisputesQuery`

---

## Command Definition

Commands are immutable `record` types implementing `IRequest<TResult>`.

```csharp
namespace EscrowApp.Features.Escrow.HoldFunds;

public sealed record HoldFundsCommand(
    Guid TransactionId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<HoldFundsResult>;
```

### Naming Conventions

- `{Action}{Aggregate}Command` — e.g., `HoldFundsCommand`, `ReleaseFundsCommand`
- `{Action}{Aggregate}Query` — e.g., `GetEscrowTransactionsQuery`
- Use the business language, not technical language (`HoldFunds` not `CreatePaymentIntent`)

---

## Handler Structure

Handlers are `sealed` classes with a **single responsibility**: orchestrate one use case.

```csharp
namespace EscrowApp.Features.Escrow.HoldFunds;

public sealed class HoldFundsHandler(
    IEscrowTransactionRepository repository,
    IFundHoldable fundHoldStrategy,
    IEventBus eventBus,
    ILogger<HoldFundsHandler> logger) : IRequestHandler<HoldFundsCommand, HoldFundsResult>
{
    public async Task<HoldFundsResult> Handle(
        HoldFundsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Holding funds for transaction {TransactionId}", request.TransactionId);

        var transaction = await repository.GetByIdAsync(
            request.TransactionId, cancellationToken);

        if (transaction is null)
            return HoldFundsResult.NotFound(request.TransactionId);

        var holdResult = await fundHoldStrategy.HoldAsync(
            transaction, request.Amount, request.Currency,
            request.IdempotencyKey, cancellationToken);

        if (!holdResult.IsSuccess)
            return HoldFundsResult.PaymentFailed(holdResult.ErrorMessage);

        transaction.HoldFunds();
        await repository.UpdateAsync(transaction, cancellationToken);

        await eventBus.PublishAsync(
            new FundsHeldEvent(transaction.Id, request.Amount), cancellationToken);

        return HoldFundsResult.Success(transaction.Id);
    }
}
```

### Handler Rules

- **Inject interfaces only** — never concrete types, never `DbContext`
- **Propagate `CancellationToken`** through every async call
- **Log with structured data** — use correlation IDs, never PII
- **One handler per command/query** — no reuse across slices
- **No business logic** — delegate to domain entities and strategy services

---

## Result DTOs

Use result objects for flow control. **Never throw exceptions for business errors.**

```csharp
namespace EscrowApp.Features.Escrow.HoldFunds;

public sealed record HoldFundsResult
{
    public bool IsSuccess { get; init; }
    public Guid? TransactionId { get; init; }
    public string? ErrorMessage { get; init; }
    public HoldFundsErrorCode? ErrorCode { get; init; }

    public static HoldFundsResult Success(Guid transactionId) =>
        new() { IsSuccess = true, TransactionId = transactionId };

    public static HoldFundsResult NotFound(Guid transactionId) =>
        new() { IsSuccess = false, ErrorCode = HoldFundsErrorCode.NotFound,
                ErrorMessage = $"Transaction {transactionId} not found." };

    public static HoldFundsResult PaymentFailed(string? reason) =>
        new() { IsSuccess = false, ErrorCode = HoldFundsErrorCode.PaymentFailed,
                ErrorMessage = reason ?? "Payment processing failed." };
}

public enum HoldFundsErrorCode
{
    NotFound,
    PaymentFailed,
    InvalidState,
    DuplicateRequest
}
```

### Result Rules

- Include `IsSuccess` boolean for quick checks
- Include typed `ErrorCode` enum for programmatic handling
- Include `ErrorMessage` for human-readable context
- Static factory methods for each outcome — makes handler code readable
- Never expose domain entities in results — map to DTOs

---

## Pipeline Behaviors

Register cross-cutting concerns as MediatR pipeline behaviors.

### Validation Behavior

Runs FluentValidation before the handler executes:

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Logging Behavior

Logs request entry/exit with elapsed time:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

        return response;
    }
}
```

### Registration

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<HoldFundsCommand>();
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

---

## Calling from Blazor Components

**Never call services directly from components.** Always go through MediatR.

```csharp
// ✅ Component code-behind — dispatches through MediatR
public sealed partial class HoldFundsPage : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;

    private async Task OnHoldFundsAsync()
    {
        var result = await Mediator.Send(new HoldFundsCommand(
            TransactionId: _transactionId,
            Amount: _amount,
            Currency: "USD",
            IdempotencyKey: Guid.CreateVersion7().ToString()));

        if (result.IsSuccess)
            NavigationManager.NavigateTo("/escrow/dashboard");
        else
            _errorMessage = result.ErrorMessage;
    }
}
```

```csharp
// ❌ VIOLATION — calling infrastructure directly from component
public sealed partial class HoldFundsPage : ComponentBase
{
    [Inject] private IEscrowTransactionRepository Repository { get; set; } = default!;
    [Inject] private IFundHoldable FundHoldService { get; set; } = default!;

    private async Task OnHoldFundsAsync()
    {
        var tx = await Repository.GetByIdAsync(_transactionId);
        await FundHoldService.HoldAsync(tx, _amount, "USD", _key);
        // VIOLATION — bypasses validation, logging, and event publishing
    }
}
```

---

## Idempotency for Payment Commands

All commands that trigger financial operations **must** include an `IdempotencyKey`:

```csharp
public sealed record HoldFundsCommand(
    Guid TransactionId,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<HoldFundsResult>;

public sealed record ReleaseFundsCommand(
    Guid TransactionId,
    string IdempotencyKey) : IRequest<ReleaseFundsResult>;
```

- Generate keys client-side using `Guid.CreateVersion7().ToString()`
- Check for existing idempotency key in the handler before processing
- Return the cached result for duplicate requests
- Pass the key to Stripe API calls via `RequestOptions.IdempotencyKey`

---

## Quick Reference

| Concept | Convention |
|---|---|
| Folder structure | `Features/Escrow/{Action}/{Command,Handler,Validator,Result}.cs` |
| Command naming | `{Verb}{Noun}Command` — `HoldFundsCommand` |
| Query naming | `Get{Noun}Query` — `GetEscrowTransactionsQuery` |
| Handler class | `sealed class`, primary constructor, inject interfaces |
| Result type | `sealed record` with `IsSuccess`, `ErrorCode`, `ErrorMessage` |
| Validation | FluentValidation `AbstractValidator<TCommand>` per command |
| Pipeline | `ValidationBehavior` → `LoggingBehavior` → Handler |
| Component access | `IMediator.Send()` only — never bypass the pipeline |
| Payment commands | Always include `IdempotencyKey` property |
| CancellationToken | Propagate through every async call in the chain |
