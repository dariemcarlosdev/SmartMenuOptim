# Common Issues Detection Guide

Patterns for identifying the most frequent code quality issues in .NET/Blazor projects.

## N+1 Query Detection

**Pattern:** Loading related data inside a loop instead of eager loading.

```csharp
// ISSUE: N+1 — one query per order to load items
var orders = await _context.Orders.ToListAsync();
foreach (var order in orders)
{
    order.Items = await _context.OrderItems
        .Where(i => i.OrderId == order.Id).ToListAsync(); // N additional queries
}

// FIX: Eager load with Include
var orders = await _context.Orders
    .Include(o => o.Items)
    .ToListAsync(); // Single query with JOIN

// FIX: Projection for read-only scenarios
var orderDtos = await _context.Orders
    .Select(o => new OrderDto(o.Id, o.Items.Count))
    .ToListAsync();
```

## Magic Numbers and Strings

**Pattern:** Hardcoded values without named constants.

```csharp
// ISSUE: Magic numbers
if (order.Amount > 10000) { /* ... */ }
if (retryCount >= 3) { /* ... */ }
await Task.Delay(5000);

// FIX: Named constants
private const decimal HighValueOrderThreshold = 10_000m;
private const int MaxRetryAttempts = 3;
private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
```

## Dead Code Patterns

**What to look for:**
- Unused `using` statements
- Commented-out code blocks (remove or create a ticket)
- Unreachable code after `return`/`throw`
- Unused private methods and fields
- Empty catch blocks
- Parameters that are never read
- `#if DEBUG` blocks with stale code

```csharp
// ISSUE: Dead code in catch
catch (Exception ex)
{
    // TODO: handle this later
}

// FIX: At minimum, log; prefer specific exception types
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    _logger.LogWarning(ex, "Resource {Id} not found", resourceId);
    return Result.NotFound();
}
```

## Blocking Async Calls

```csharp
// ISSUE: Deadlock risk — blocking on async
var result = _service.GetDataAsync().Result;
_service.SaveAsync(data).Wait();
Task.Run(() => _service.ProcessAsync()).GetAwaiter().GetResult();

// FIX: Async all the way
var result = await _service.GetDataAsync();
await _service.SaveAsync(data);
await _service.ProcessAsync();
```

## Missing CancellationToken Propagation

```csharp
// ISSUE: Token not propagated
public async Task<Order> GetOrderAsync(int id, CancellationToken ct)
{
    var order = await _context.Orders.FindAsync(id); // Missing ct!
    var items = await _httpClient.GetAsync($"/api/items/{id}"); // Missing ct!
    return order;
}

// FIX: Pass token to every async call
public async Task<Order> GetOrderAsync(int id, CancellationToken ct)
{
    var order = await _context.Orders.FindAsync(new object[] { id }, ct);
    var items = await _httpClient.GetAsync($"/api/items/{id}", ct);
    return order;
}
```

## Improper DI Lifetimes

```csharp
// ISSUE: Captive dependency — Singleton captures Scoped service
services.AddSingleton<ICacheService, CacheService>(); // Singleton
services.AddScoped<IDbContext, AppDbContext>();         // Scoped
// CacheService injecting IDbContext = captive dependency bug

// ISSUE: Transient for expensive resources
services.AddTransient<HttpClient>(); // Creates new socket per request
// FIX: Use IHttpClientFactory
services.AddHttpClient<IPaymentClient, PaymentClient>();
```

## String Concatenation in Loops

```csharp
// ISSUE: O(n²) string allocations
var result = "";
foreach (var item in items)
    result += item.Name + ", "; // New string allocation each iteration

// FIX: StringBuilder
var sb = new StringBuilder();
foreach (var item in items)
    sb.Append(item.Name).Append(", ");

// FIX: LINQ Join (cleanest for simple cases)
var result = string.Join(", ", items.Select(i => i.Name));
```

## Missing Null/Guard Checks

```csharp
// ISSUE: No validation
public async Task<OrderDto> GetOrderAsync(int orderId)
{
    var order = await _repository.GetByIdAsync(orderId);
    return _mapper.Map<OrderDto>(order); // NRE if order is null
}

// FIX: Guard clause with meaningful error
public async Task<OrderDto> GetOrderAsync(int orderId)
{
    var order = await _repository.GetByIdAsync(orderId)
        ?? throw new NotFoundException(nameof(Order), orderId);
    return _mapper.Map<OrderDto>(order);
}
```
