# EF Core Optimization — .NET 10 / PostgreSQL

Reference guide for optimizing Entity Framework Core queries in the NexTruzt escrow platform. All examples use Npgsql as the PostgreSQL provider.

## N+1 Detection and Fix Patterns

### The Problem

N+1 occurs when accessing a navigation property triggers a separate query for each parent entity. With 100 escrow transactions, that's 101 queries (1 for transactions + 100 for buyers).

### Detection

- EF Core logs show repeated `SELECT` statements inside a loop
- `Microsoft.EntityFrameworkCore.Database.Command` log category at `Information` level
- MiniProfiler or OpenTelemetry traces show sequential identical queries with different parameter values

### Fix: Include / ThenInclude

```csharp
// ❌ N+1: lazy loading fires per iteration
var transactions = await _context.EscrowTransactions.ToListAsync(ct);
foreach (var t in transactions)
{
    var buyerName = t.Buyer.Name;       // SELECT from users WHERE id = @p0
    var sellerName = t.Seller.Name;     // SELECT from users WHERE id = @p1
}

// ✅ Eager loading: 1 query with JOINs
var transactions = await _context.EscrowTransactions
    .Include(t => t.Buyer)
    .Include(t => t.Seller)
    .Where(t => t.Status == EscrowStatus.Pending)
    .ToListAsync(ct);
```

### Fix: AsSplitQuery (Cartesian Explosion Prevention)

When a parent has multiple collection navigations, a single query produces a cartesian product. Split queries issue separate SQL statements instead.

```csharp
// ❌ Cartesian explosion: 1 transaction × N milestones × M documents = N×M rows
var tx = await _context.EscrowTransactions
    .Include(t => t.Milestones)
    .Include(t => t.Documents)
    .FirstOrDefaultAsync(t => t.Id == id, ct);

// ✅ Split query: 3 separate SELECTs, no cartesian product
var tx = await _context.EscrowTransactions
    .Include(t => t.Milestones)
    .Include(t => t.Documents)
    .AsSplitQuery()
    .FirstOrDefaultAsync(t => t.Id == id, ct);
```

**Trade-off:** Split queries make 3 round trips instead of 1. Use when the cartesian product is large; prefer single query when result sets are small.

## AsNoTracking and AsNoTrackingWithIdentityResolution

### AsNoTracking

Disables the change tracker for read-only queries. Reduces memory and CPU overhead significantly on large result sets.

```csharp
// ✅ Read-only dashboard query — no need for tracking
var summaries = await _context.EscrowTransactions
    .AsNoTracking()
    .Where(t => t.CreatedAt >= startDate)
    .Select(t => new TransactionSummaryDto
    {
        Id = t.Id,
        Amount = t.Amount,
        Status = t.Status
    })
    .ToListAsync(ct);
```

**When NOT to use:** If the query feeds an update flow (fetch → modify → SaveChanges), tracking is required.

### AsNoTrackingWithIdentityResolution

Use when you need no-tracking performance but the result set includes duplicate entities from JOINs. Ensures each entity instance is shared, not duplicated.

```csharp
// Multiple transactions may reference the same Buyer
var transactions = await _context.EscrowTransactions
    .AsNoTrackingWithIdentityResolution()
    .Include(t => t.Buyer)
    .ToListAsync(ct);

// Now: transactions[0].Buyer and transactions[5].Buyer are the same object instance
// (if they reference the same user), reducing memory allocations
```

## Projections with Select()

Always project to DTOs when you don't need the full entity. This reduces data transfer, skips change tracking, and often produces better SQL.

```csharp
// ❌ Loads all 30+ columns from escrow_transactions
var list = await _context.EscrowTransactions.ToListAsync(ct);

// ✅ Only fetches the 4 columns the UI grid needs
var list = await _context.EscrowTransactions
    .AsNoTracking()
    .Where(t => t.BuyerId == buyerId)
    .OrderByDescending(t => t.CreatedAt)
    .Select(t => new EscrowListItemDto
    {
        Id = t.Id,
        Amount = t.Amount,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt,
        SellerName = t.Seller.Name  // translated to a JOIN — no N+1
    })
    .Take(50)
    .ToListAsync(ct);
```

**Key benefits:**
- SQL `SELECT` only includes projected columns
- Navigation access inside `Select()` is translated to JOINs (no lazy loading)
- No change tracker overhead
- Smaller network payload between PostgreSQL and the application

## Compiled Queries

For hot-path queries executed thousands of times per second. Eliminates the LINQ expression tree compilation overhead on each call.

```csharp
public sealed class EscrowTransactionRepository
{
    // Compiled once, reused across all invocations
    private static readonly Func<EscrowDbContext, Guid, CancellationToken, Task<EscrowTransaction?>>
        GetByIdCompiled = EF.CompileAsyncQuery(
            (EscrowDbContext ctx, Guid id, CancellationToken ct) =>
                ctx.EscrowTransactions
                    .AsNoTracking()
                    .Include(t => t.Buyer)
                    .FirstOrDefault(t => t.Id == id));

    private readonly EscrowDbContext _context;

    public EscrowTransactionRepository(EscrowDbContext context) => _context = context;

    public Task<EscrowTransaction?> GetByIdAsync(Guid id, CancellationToken ct)
        => GetByIdCompiled(_context, id, ct);
}
```

**When to use:**
- Query is on a hot path (>1000 calls/minute)
- Query shape is fixed (no dynamic WHERE clauses)
- Profiling confirms LINQ compilation is a measurable cost

**Limitations:**
- Cannot use dynamic filters, conditional Includes, or runtime query composition
- Parameters must be simple types (not lists or complex objects)

## Raw SQL Fallback

Use `FromSqlInterpolated` when EF Core's LINQ translation is insufficient. Always parameterize.

```csharp
// ✅ Parameterized — safe from SQL injection
var results = await _context.EscrowTransactions
    .FromSqlInterpolated(
        $@"SELECT * FROM escrow_transactions 
           WHERE status = {status} 
           AND created_at > {cutoffDate}
           ORDER BY created_at DESC
           LIMIT {pageSize}")
    .AsNoTracking()
    .ToListAsync(ct);

// ✅ For non-entity results, use Dapper or ADO.NET
await using var connection = _context.Database.GetDbConnection();
await connection.OpenAsync(ct);
var stats = await connection.QueryAsync<TransactionStatsDto>(
    @"SELECT status, COUNT(*) as count, SUM(amount) as total
      FROM escrow_transactions
      WHERE created_at >= @StartDate
      GROUP BY status",
    new { StartDate = startDate });
```

**When to use raw SQL:**
- Window functions (ROW_NUMBER, RANK, LAG/LEAD)
- CTEs (WITH clauses) for complex hierarchical queries
- PostgreSQL-specific features (LATERAL joins, array_agg, jsonb_agg)
- Bulk operations that don't map to EF Core entities

## Bulk Operations

EF Core's SaveChanges is per-entity. For bulk inserts/updates, use specialized libraries or raw SQL.

```csharp
// ❌ Slow: 1000 individual INSERT statements
foreach (var log in auditLogs)
{
    _context.AuditLogs.Add(log);
}
await _context.SaveChangesAsync(ct);

// ✅ Npgsql COPY for bulk inserts (fastest)
await using var writer = await _context.Database.GetDbConnection()
    .BeginBinaryImportAsync(
        "COPY audit_logs (id, entity_id, action, created_at) FROM STDIN (FORMAT BINARY)", ct);
foreach (var log in auditLogs)
{
    await writer.StartRowAsync(ct);
    await writer.WriteAsync(log.Id, ct);
    await writer.WriteAsync(log.EntityId, ct);
    await writer.WriteAsync(log.Action, ct);
    await writer.WriteAsync(log.CreatedAt, ct);
}
await writer.CompleteAsync(ct);

// ✅ EF Core 8+ ExecuteUpdate for bulk updates (no entity loading)
await _context.EscrowTransactions
    .Where(t => t.Status == EscrowStatus.Pending && t.CreatedAt < expiryDate)
    .ExecuteUpdateAsync(s => s
        .SetProperty(t => t.Status, EscrowStatus.Expired)
        .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);

// ✅ EF Core 8+ ExecuteDelete for bulk deletes
await _context.Notifications
    .Where(n => n.IsRead && n.CreatedAt < archiveDate)
    .ExecuteDeleteAsync(ct);
```

## CancellationToken Propagation

Every async EF Core method accepts a `CancellationToken`. Always propagate it from the MediatR handler or API controller to prevent orphaned queries when the user disconnects.

```csharp
// ✅ Full CancellationToken chain: Controller → MediatR → Repository → EF Core
public sealed class GetPendingTransactionsHandler
    : IRequestHandler<GetPendingTransactionsQuery, List<TransactionSummaryDto>>
{
    private readonly EscrowDbContext _context;

    public GetPendingTransactionsHandler(EscrowDbContext context) => _context = context;

    public async Task<List<TransactionSummaryDto>> Handle(
        GetPendingTransactionsQuery request,
        CancellationToken cancellationToken)  // from MediatR pipeline
    {
        return await _context.EscrowTransactions
            .AsNoTracking()
            .Where(t => t.Status == EscrowStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                Amount = t.Amount,
                BuyerName = t.Buyer.Name,
                CreatedAt = t.CreatedAt
            })
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);  // propagated to Npgsql
    }
}
```

**What happens without CancellationToken:** If a Blazor Server user navigates away, the circuit may close but the PostgreSQL query continues running, consuming resources until it completes or times out. With proper propagation, Npgsql sends a cancellation signal to PostgreSQL, terminating the query immediately.
