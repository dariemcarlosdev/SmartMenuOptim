# .NET Review Patterns

Specific review checklist items and detection patterns for C#/.NET and Blazor Server code.

## EF Core Review Points

```csharp
// CHECK: AsNoTracking for read-only queries
var orders = await _context.Orders.ToListAsync(ct); // Tracking unnecessarily
var orders = await _context.Orders.AsNoTracking().ToListAsync(ct); // ✅ Better

// CHECK: Projection over full entity loading
var dto = await _context.Orders
    .Where(o => o.CustomerId == customerId)
    .Select(o => new OrderSummaryDto(o.Id, o.Status, o.TotalAmount))
    .ToListAsync(ct); // ✅ Only loads needed columns

// CHECK: Unbounded queries
var all = await _context.AuditLogs.ToListAsync(); // ❌ Could be millions of rows
var page = await _context.AuditLogs
    .OrderByDescending(l => l.Timestamp)
    .Skip(pageIndex * pageSize)
    .Take(pageSize)
    .ToListAsync(ct); // ✅ Bounded

// CHECK: Split queries for multiple Includes
var order = await _context.Orders
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .Include(o => o.Payments)
    .AsSplitQuery() // ✅ Avoids cartesian explosion
    .FirstOrDefaultAsync(o => o.Id == id, ct);
```

## Blazor Server Review Points

```csharp
// CHECK: Code-behind pattern (not inline @code)
// ✅ OrderList.razor + OrderList.razor.cs + OrderList.razor.css
// ❌ Everything in OrderList.razor @code { } block

// CHECK: Authorize attribute on routable components
@page "/escrow/dashboard"
@attribute [Authorize(Policy = "EscrowManager")] // ✅ Required

// CHECK: IDisposable for event subscriptions
public partial class EscrowDashboard : ComponentBase, IDisposable
{
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync(_cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

// CHECK: StateHasChanged called within InvokeAsync
_timer.Elapsed += async (_, _) =>
{
    await InvokeAsync(StateHasChanged); // ✅ Thread-safe
};

// CHECK: AuthenticationState via CascadingParameter
[CascadingParameter]
private Task<AuthenticationState> AuthState { get; set; } = default!;
// ❌ Don't use IHttpContextAccessor in Blazor components
```

## MediatR / CQRS Review Points

```csharp
// CHECK: Commands and queries are separated
public record CreateEscrowCommand(/* ... */) : IRequest<Result<EscrowId>>; // ✅ Write
public record GetEscrowQuery(EscrowId Id) : IRequest<EscrowDto>;          // ✅ Read

// CHECK: FluentValidation for command validation
public sealed class CreateEscrowCommandValidator
    : AbstractValidator<CreateEscrowCommand>
{
    public CreateEscrowCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(Money.Zero);
        RuleFor(x => x.BuyerId).NotEmpty();
    }
}

// CHECK: CancellationToken in handlers
public sealed class CreateEscrowHandler
    : IRequestHandler<CreateEscrowCommand, Result<EscrowId>>
{
    public async Task<Result<EscrowId>> Handle(
        CreateEscrowCommand request,
        CancellationToken cancellationToken) // ✅ Must propagate
    {
        // All async calls pass cancellationToken
    }
}
```

## DI Registration Review

```csharp
// CHECK: Correct lifetimes
services.AddScoped<IEscrowService, EscrowService>();      // ✅ Per-request
services.AddSingleton<IMemoryCache, MemoryCache>();        // ✅ Shared state
services.AddTransient<IValidator<CreateEscrowCommand>,     // ✅ Stateless
    CreateEscrowCommandValidator>();

// CHECK: No captive dependencies (Singleton capturing Scoped)
// CHECK: IHttpClientFactory instead of new HttpClient()
services.AddHttpClient<IPaymentGateway, StripeGateway>(client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

## Async/Await Patterns

```csharp
// CHECK: ConfigureAwait in library code
var data = await _client.GetAsync(url, ct).ConfigureAwait(false);

// CHECK: ValueTask for frequently synchronous paths
public ValueTask<Escrow?> GetFromCacheAsync(EscrowId id)
{
    if (_cache.TryGetValue(id, out Escrow? cached))
        return ValueTask.FromResult(cached); // No allocation
    return new ValueTask<Escrow?>(LoadFromDatabaseAsync(id));
}

// CHECK: Async disposal
public sealed class EscrowProcessor : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public async ValueTask DisposeAsync() => _semaphore.Dispose();
}
```

## Security Patterns in .NET

```csharp
// CHECK: Policy-based authorization (not role strings)
[Authorize(Policy = "CanManageEscrow")] // ✅
[Authorize(Roles = "Admin")]            // ⚠️ Prefer policies

// CHECK: Options pattern for configuration
public sealed class EscrowSettings
{
    public decimal MaxAmount { get; init; }
    public int TimeoutDays { get; init; }
}
services.Configure<EscrowSettings>(config.GetSection("Escrow"));
// ❌ Don't inject IConfiguration directly into services

// CHECK: Structured logging (no string interpolation)
_logger.LogInformation("Escrow {EscrowId} created for {Amount}",
    escrow.Id, escrow.Amount); // ✅ Structured
_logger.LogInformation($"Escrow {escrow.Id} created"); // ❌ No structured params
```
