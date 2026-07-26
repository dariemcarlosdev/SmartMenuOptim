# Common Bug Patterns Reference

> **Load when:** Investigating race conditions, memory leaks, null references, or deadlocks.

## Null Reference Exceptions

The most common .NET exception. Systematic approaches to diagnose and prevent.

### Common Causes in .NET

| Cause | Example | Fix |
|---|---|---|
| Uninitialized navigation property | `escrow.Buyer.Name` when `Buyer` not loaded | Use `Include()` or null check |
| Missing DTO mapping | AutoMapper returns null for unmapped field | Verify mapping configuration |
| Async void event handler | State nullified before callback executes | Use `async Task` pattern |
| Optional dependency not registered | `IService` resolves to null | Use `GetRequiredService<T>()` |
| Dictionary key miss | `dict[key]` when key absent | Use `TryGetValue()` |

### Prevention Pattern

```csharp
// Guard clause pattern — fail fast with meaningful message
public async Task<EscrowDto> GetEscrowAsync(string escrowId, CancellationToken ct)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(escrowId);

    var escrow = await _context.Escrows
        .Include(e => e.Buyer)
        .Include(e => e.Seller)
        .FirstOrDefaultAsync(e => e.Id == escrowId, ct);

    if (escrow is null)
        throw new NotFoundException(nameof(Escrow), escrowId);

    return _mapper.Map<EscrowDto>(escrow);
}
```

## Race Conditions

Bugs that depend on timing — the hardest category to reproduce and fix.

### Symptoms

- Intermittent test failures ("flaky tests")
- Works in debug mode, fails in release mode
- Works on developer machine, fails in CI
- Data corruption that appears randomly

### Common .NET Race Conditions

**1. Shared mutable state without synchronization:**

```csharp
// BUG: Dictionary is not thread-safe
private readonly Dictionary<string, EscrowState> _cache = new();

public void UpdateState(string id, EscrowState state)
{
    _cache[id] = state; // Race condition under concurrent access
}

// FIX: Use ConcurrentDictionary
private readonly ConcurrentDictionary<string, EscrowState> _cache = new();
```

**2. Check-then-act (TOCTOU):**

```csharp
// BUG: Another thread can change balance between check and debit
if (account.Balance >= amount)
{
    account.Balance -= amount; // May overdraft if concurrent debit
}

// FIX: Use optimistic concurrency with EF Core
// Add [ConcurrencyCheck] or RowVersion to entity
[Timestamp]
public byte[] RowVersion { get; set; }
```

**3. Double initialization in Blazor:**

```csharp
// BUG: OnInitializedAsync fires twice in Blazor Server (prerender + connect)
protected override async Task OnInitializedAsync()
{
    _data = await _service.LoadDataAsync(); // Runs twice, may cause issues
}

// FIX: Guard against double initialization
private bool _initialized;
protected override async Task OnInitializedAsync()
{
    if (_initialized) return;
    _initialized = true;
    _data = await _service.LoadDataAsync();
}
```

## Memory Leaks

### Common .NET Memory Leak Patterns

| Pattern | Cause | Detection |
|---|---|---|
| Event handler not unsubscribed | `+=` without `-=` | Growing Gen2 heap, increasing handle count |
| Static collections growing | `static List<T>` appended without pruning | `dotnet-gcdump` shows large static roots |
| Blazor circuit holding references | Component not implementing `IDisposable` | Memory growth per user connection |
| Timer not disposed | `System.Timers.Timer` without `Dispose()` | Thread count growth |
| Closure capturing `this` | Lambda in long-lived context captures component | GC roots analysis |

### Detection with dotnet-gcdump

```bash
# Capture GC dump
dotnet-gcdump collect -p <PID>

# Analyze — find top types by count and size
dotnet-gcdump report <file>.gcdump

# Compare two dumps to find growth
# Take dump at T=0, wait, take dump at T=1, compare top types
```

### Blazor Memory Leak Prevention

```csharp
public partial class EscrowDashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private IEscrowNotificationService Notifications { get; set; } = default!;
    private CancellationTokenSource _cts = new();

    protected override void OnInitialized()
    {
        Notifications.OnEscrowUpdated += HandleEscrowUpdated;
    }

    public async ValueTask DisposeAsync()
    {
        Notifications.OnEscrowUpdated -= HandleEscrowUpdated;
        await _cts.CancelAsync();
        _cts.Dispose();
    }
}
```

## Deadlocks

### Classic .NET Deadlock: Sync-over-Async

```csharp
// BUG: Deadlock in ASP.NET (synchronization context blocks)
public EscrowDto GetEscrow(string id)
{
    return _service.GetEscrowAsync(id).Result; // DEADLOCK
}

// FIX: Use async all the way down
public async Task<EscrowDto> GetEscrowAsync(string id)
{
    return await _service.GetEscrowAsync(id);
}
```

### Database Deadlocks

```sql
-- Detect deadlocks in PostgreSQL
SELECT blocked.pid AS blocked_pid,
       blocked.query AS blocked_query,
       blocking.pid AS blocking_pid,
       blocking.query AS blocking_query
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked ON blocked.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
    AND blocking_locks.relation = blocked_locks.relation
    AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking ON blocking.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;
```

### Deadlock Prevention Checklist

1. Never call `.Result` or `.Wait()` on async code — use `await` throughout
2. Always acquire locks in the same order across all code paths
3. Use `ConfigureAwait(false)` in library code
4. Set timeouts on all lock acquisitions and database commands
5. Use optimistic concurrency (row versioning) instead of pessimistic locks
