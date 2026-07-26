# .NET Diagnostics Reference

> **Load when:** Using VS debugging features, dotnet-dump, dotnet-trace, or other .NET diagnostic tools.

## dotnet-dump — Memory Dump Analysis

Capture and analyze process memory dumps for crash investigation, memory leaks, and deadlock detection.

### Capture a Dump

```bash
# Capture a full dump (includes heap — large file)
dotnet-dump collect -p <PID> -o dump-full.dmp --type Full

# Capture a mini dump (smaller, stack traces only)
dotnet-dump collect -p <PID> -o dump-mini.dmp --type Mini

# Capture on crash (set environment variables before starting the app)
export DOTNET_DbgEnableMiniDump=1
export DOTNET_DbgMiniDumpType=4  # Full dump
export DOTNET_DbgMiniDumpName=/dumps/crash_%p_%t.dmp
```

### Analyze a Dump

```bash
dotnet-dump analyze dump-full.dmp

# Common analysis commands:
> clrstack              # Show managed call stack for current thread
> clrstack -all         # Show all managed thread stacks
> dumpheap -stat        # Heap statistics — find large/numerous types
> dumpheap -type Escrow # Find all Escrow objects on heap
> dumpobj <address>     # Inspect a specific object
> gcroot <address>      # Find what keeps an object alive (leak detection)
> dso                   # Dump stack objects
> syncblk               # Show sync block info (deadlock detection)
> threads               # List all threads with their states
> pe                    # Print last exception on current thread
```

### Memory Leak Investigation Workflow

```bash
# Step 1: Take baseline dump
dotnet-dump collect -p <PID> -o baseline.dmp

# Step 2: Exercise the suspected leak (repeat the operation many times)

# Step 3: Take comparison dump
dotnet-dump collect -p <PID> -o after.dmp

# Step 4: Compare heap statistics
dotnet-dump analyze baseline.dmp
> dumpheap -stat > baseline-heap.txt
> exit

dotnet-dump analyze after.dmp
> dumpheap -stat > after-heap.txt

# Step 5: Diff the heap stats — look for types with significantly more instances
# Growing object counts indicate the leak
```

## dotnet-trace — Performance Tracing

Collect detailed runtime event traces for performance profiling and event analysis.

### Collect Traces

```bash
# Collect for 30 seconds with default providers
dotnet-trace collect -p <PID> --duration 00:00:30

# Collect with specific providers for GC analysis
dotnet-trace collect -p <PID> \
    --providers Microsoft-DotNETCore-SampleProfiler,Microsoft-Windows-DotNETRuntime:0x1:5

# Common provider configurations:
# CPU profiling: Microsoft-DotNETCore-SampleProfiler
# GC events:     Microsoft-Windows-DotNETRuntime:0x1:5
# HTTP events:   Microsoft-Extensions-HttpClientFactory
# EF Core:       Microsoft.EntityFrameworkCore
```

### Analyze Traces

```bash
# Convert to Speedscope format for browser-based flame graph
dotnet-trace convert trace.nettrace --format Speedscope

# Open in Speedscope (https://www.speedscope.app/)
# Or open .nettrace directly in Visual Studio or PerfView
```

## dotnet-counters — Real-Time Monitoring

Monitor runtime performance counters in real time — the quickest diagnostic tool.

```bash
# Monitor common counters
dotnet-counters monitor -p <PID> --counters \
    System.Runtime,\
    Microsoft.AspNetCore.Hosting,\
    Microsoft.AspNetCore.Http.Connections,\
    System.Net.Http

# Key counters to watch:
# System.Runtime:
#   cpu-usage                    - CPU usage percentage
#   working-set                  - Working set (MB)
#   gc-heap-size                 - GC heap size (MB)
#   gen-0-gc-count               - Gen 0 GC count
#   gen-2-gc-count               - Gen 2 GC count (frequent = problem)
#   threadpool-queue-length      - ThreadPool queue length (high = saturation)
#   exception-count              - Exception rate

# Microsoft.AspNetCore.Hosting:
#   requests-per-second          - Request rate
#   total-requests               - Total requests
#   current-requests             - Active requests
#   failed-requests              - Failed request count
```

## EF Core Debugging

### Query Logging and Analysis

```csharp
// In DbContext configuration — Development only
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options
        .UseNpgsql(_connectionString)
        .LogTo(message => Debug.WriteLine(message),
               new[] { DbLoggerCategory.Database.Command.Name },
               LogLevel.Information)
        .EnableSensitiveDataLogging()  // Shows parameter values
        .EnableDetailedErrors();       // Better error messages
}
```

### Detecting N+1 Queries

```csharp
// BAD: N+1 query — one query per escrow to load payments
var escrows = await _context.Escrows.ToListAsync(ct);
foreach (var escrow in escrows)
{
    var payments = escrow.Payments; // Lazy load fires N queries
}

// GOOD: Eager loading with Include
var escrows = await _context.Escrows
    .Include(e => e.Payments)
    .AsNoTracking()
    .ToListAsync(ct);

// BETTER: Projection to DTO — only loads needed columns
var escrows = await _context.Escrows
    .Select(e => new EscrowSummaryDto
    {
        Id = e.Id,
        Status = e.Status,
        PaymentCount = e.Payments.Count
    })
    .ToListAsync(ct);
```

## Blazor Debugging

### Circuit Debugging (Blazor Server)

```csharp
// Custom CircuitHandler to track connection lifecycle
public sealed class DiagnosticCircuitHandler : CircuitHandler
{
    private readonly ILogger<DiagnosticCircuitHandler> _logger;

    public DiagnosticCircuitHandler(ILogger<DiagnosticCircuitHandler> logger)
        => _logger = logger;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation("Circuit opened: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogWarning("Connection down: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation("Circuit closed: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}

// Register in DI
services.AddScoped<CircuitHandler, DiagnosticCircuitHandler>();
```

### SignalR Debugging

```csharp
// Enable detailed SignalR logging
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Development only
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// Client-side JS logging
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .configureLogging(signalR.LogLevel.Debug)
    .build();
```

## Exception Analysis Patterns

### Unwinding AggregateException

```csharp
try
{
    await Task.WhenAll(tasks);
}
catch (AggregateException aex)
{
    foreach (var inner in aex.Flatten().InnerExceptions)
    {
        _logger.LogError(inner, "Task failed: {ErrorType}: {Message}",
            inner.GetType().Name, inner.Message);
    }
}
```

### MediatR Pipeline Exception Correlation

```csharp
// Behavior that adds correlation context to all exceptions in the pipeline
public sealed class ExceptionEnrichmentBehavior<TReq, TRes>
    : IPipelineBehavior<TReq, TRes> where TReq : notnull
{
    private readonly ILogger<ExceptionEnrichmentBehavior<TReq, TRes>> _logger;

    public ExceptionEnrichmentBehavior(ILogger<ExceptionEnrichmentBehavior<TReq, TRes>> logger)
        => _logger = logger;

    public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MediatR handler failed for {RequestType}: {@Request}",
                typeof(TReq).Name, request);
            throw;
        }
    }
}
```
