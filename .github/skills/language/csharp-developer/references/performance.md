# Performance Optimization Reference

> **Load when:** Using Span&lt;T&gt;, optimizing async patterns, reducing memory allocations, or preparing for AOT.

## Span&lt;T&gt; and Memory&lt;T&gt;

### String Parsing Without Allocations

```csharp
// Parse escrow ID format: "ESC-A1B2C3D4" without allocating substrings
public static bool TryParseEscrowId(ReadOnlySpan<char> input, out Guid id)
{
    id = Guid.Empty;
    
    if (input.Length < 12 || !input.StartsWith("ESC-"))
        return false;

    var hexPart = input[4..]; // No allocation — just a slice
    return Guid.TryParse(hexPart, out id);
}

// Process CSV data without string allocations
public static IEnumerable<(string BuyerId, decimal Amount)> ParseTransactionCsv(
    ReadOnlySpan<char> csvLine)
{
    var results = new List<(string, decimal)>();
    
    foreach (var line in csvLine.EnumerateLines())
    {
        var commaIndex = line.IndexOf(',');
        if (commaIndex < 0) continue;
        
        var buyerId = line[..commaIndex].ToString(); // Allocate only for the field we keep
        if (decimal.TryParse(line[(commaIndex + 1)..], out var amount))
            results.Add((buyerId, amount));
    }
    
    return results;
}
```

### Buffer Pooling

```csharp
// Rent a buffer instead of allocating
public static async Task<string> ReadTransactionDataAsync(Stream stream, CancellationToken ct)
{
    var buffer = ArrayPool<byte>.Shared.Rent(4096);
    try
    {
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(), ct);
        return Encoding.UTF8.GetString(buffer.AsSpan(0, bytesRead));
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

## Async Best Practices

### Avoid Common Pitfalls

```csharp
// BAD — async void (fire-and-forget, exceptions lost)
async void ProcessPayment() { ... }

// GOOD — always return Task
async Task ProcessPaymentAsync(CancellationToken ct) { ... }

// BAD — blocking on async code (deadlock risk)
var result = GetEscrowAsync(id).Result;

// GOOD — await throughout
var result = await GetEscrowAsync(id, ct);

// BAD — unnecessary async state machine
async Task<Escrow?> GetAsync(EscrowId id, CancellationToken ct)
{
    return await repository.FindByIdAsync(id, ct); // Pointless wrapper
}

// GOOD — pass through directly (avoid state machine overhead)
Task<Escrow?> GetAsync(EscrowId id, CancellationToken ct) =>
    repository.FindByIdAsync(id, ct);
```

### CancellationToken Propagation

```csharp
// Always propagate CancellationToken through the entire call chain
public async Task<Result<EscrowDto>> ProcessReleaseAsync(
    ReleaseEscrowCommand command, CancellationToken ct)
{
    var escrow = await repository.GetByIdAsync(command.EscrowId, ct);
    var payment = await paymentGateway.InitiateTransferAsync(escrow.Amount, ct);
    await repository.UpdateStatusAsync(escrow.Id, EscrowStatus.Released, ct);
    await unitOfWork.SaveChangesAsync(ct);
    await notificationService.SendReleaseNotificationAsync(escrow, ct);
    
    return Result<EscrowDto>.Success(escrow.ToDto());
}
```

### ValueTask for Hot Paths

```csharp
// Use ValueTask when result is often synchronous (e.g., cached)
public ValueTask<EscrowDto?> GetCachedEscrowAsync(EscrowId id, CancellationToken ct)
{
    if (_cache.TryGetValue(id, out var cached))
        return ValueTask.FromResult<EscrowDto?>(cached); // No allocation

    return LoadAndCacheAsync(id, ct); // Fallback to async
}

private async ValueTask<EscrowDto?> LoadAndCacheAsync(EscrowId id, CancellationToken ct)
{
    var escrow = await repository.GetByIdAsync(id, ct);
    if (escrow is not null)
        _cache.Set(id, escrow.ToDto(), TimeSpan.FromMinutes(5));
    return escrow?.ToDto();
}
```

## Memory Optimization

### Object Pooling

```csharp
// Pool frequently-created objects
private static readonly ObjectPool<StringBuilder> _sbPool =
    new DefaultObjectPoolProvider().CreateStringBuilderPool();

public static string BuildEscrowReport(IEnumerable<EscrowDto> escrows)
{
    var sb = _sbPool.Get();
    try
    {
        foreach (var escrow in escrows)
        {
            sb.Append("ESC-").Append(escrow.Id).Append(": ")
              .Append(escrow.Amount).AppendLine(escrow.Currency);
        }
        return sb.ToString();
    }
    finally
    {
        _sbPool.Return(sb);
    }
}
```

### Frozen Collections (Read-Only Hot Path)

```csharp
// FrozenDictionary for frequently-read, rarely-updated lookups
private static readonly FrozenDictionary<string, decimal> CurrencyRates =
    new Dictionary<string, decimal>
    {
        ["USD"] = 1.0m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["CAD"] = 1.36m,
    }.ToFrozenDictionary();

private static readonly FrozenSet<string> SupportedCurrencies =
    new HashSet<string> { "USD", "EUR", "GBP", "CAD" }.ToFrozenSet();
```

### Struct vs Class Decision

| Criteria | Use `struct` / `record struct` | Use `class` / `record` |
|---|---|---|
| Size | ≤ 16 bytes | > 16 bytes |
| Lifetime | Short-lived, stack-allocated | Long-lived, heap |
| Collections | Rarely stored in large collections | Frequently in collections |
| Equality | Value semantics needed | Reference semantics OK |
| Example | `Money`, `EscrowId`, `DateRange` | `Escrow`, `Payment`, `User` |

## AOT Compilation

### Trimming-Safe Code

```csharp
// BAD — reflection-based serialization (breaks AOT)
JsonSerializer.Deserialize<EscrowDto>(json);

// GOOD — source-generated serialization
[JsonSerializable(typeof(EscrowDto))]
[JsonSerializable(typeof(CreateEscrowResult))]
[JsonSerializable(typeof(PaginatedList<EscrowSummaryDto>))]
internal sealed partial class AppJsonContext : JsonSerializerContext;

// Usage
JsonSerializer.Deserialize(json, AppJsonContext.Default.EscrowDto);
```

### Minimal API AOT Configuration

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0,
        AppJsonContext.Default);
});

// Ensure all types used in endpoints are registered in AppJsonContext
```

## Benchmarking

```csharp
// Use BenchmarkDotNet for performance-critical code
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net100)]
public class EscrowIdParsingBenchmarks
{
    private const string Input = "ESC-A1B2C3D4-E5F6-7890-ABCD-EF1234567890";

    [Benchmark(Baseline = true)]
    public Guid ParseWithSubstring()
    {
        var hex = Input.Substring(4);
        return Guid.Parse(hex);
    }

    [Benchmark]
    public Guid ParseWithSpan()
    {
        ReadOnlySpan<char> span = Input.AsSpan();
        return Guid.Parse(span[4..]);
    }
}
```
