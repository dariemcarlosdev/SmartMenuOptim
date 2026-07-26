# Structured Logging Reference

> **Load when:** Setting up Serilog, implementing structured log patterns, or configuring log enrichers.

## Serilog Configuration

### Full Setup for ASP.NET Core + Blazor Server

```csharp
// Program.cs — Bootstrap Serilog before host build
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "EscrowApp")
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.Seq("http://localhost:5341")
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/escrowapp-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 100_000_000)
    .CreateLogger();

try
{
    Log.Information("Starting EscrowApp");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    // ... configure services
    var app = builder.Build();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    });
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### Required NuGet Packages

```xml
<ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="8.*" />
    <PackageReference Include="Serilog.Enrichers.Environment" Version="3.*" />
    <PackageReference Include="Serilog.Formatting.Compact" Version="3.*" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="8.*" />
    <PackageReference Include="Serilog.Sinks.File" Version="6.*" />
</ItemGroup>
```

## Structured Logging Patterns

### DO: Use Message Templates with Named Properties

```csharp
// CORRECT — Structured, queryable, type-safe
Log.Information("Escrow {EscrowId} created for {Amount:C} by {BuyerId}",
    escrow.Id, escrow.Amount, escrow.BuyerId);

// Output (JSON):
// {"@t":"2024-01-15T10:30:00","@mt":"Escrow {EscrowId} created...","EscrowId":"ESC-001","Amount":5000,"BuyerId":"USR-123"}
```

### DON'T: Use String Interpolation

```csharp
// WRONG — Loses structure, can't query by EscrowId
Log.Information($"Escrow {escrow.Id} created for {escrow.Amount} by {escrow.BuyerId}");

// Output (JSON):
// {"@t":"2024-01-15T10:30:00","@mt":"Escrow ESC-001 created for 5000 by USR-123"}
// No separate EscrowId, Amount, or BuyerId properties!
```

### Log Destructuring

```csharp
// Destructure complex objects with @ operator
Log.Information("Processing escrow: {@Escrow}", new
{
    escrow.Id,
    escrow.Status,
    escrow.Amount,
    escrow.CreatedAt
});

// Use $ for ToString() representation
Log.Information("Escrow status: {$Status}", escrow.Status);
```

## Correlation IDs

### MediatR Pipeline Behavior for Correlation

```csharp
public sealed class CorrelationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<CorrelationBehavior<TRequest, TResponse>> _logger;

    public CorrelationBehavior(ILogger<CorrelationBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestType", typeof(TRequest).Name))
        {
            _logger.LogInformation("Handling {RequestType} — {@Request}", typeof(TRequest).Name, request);
            var sw = Stopwatch.StartNew();
            var response = await next();
            sw.Stop();
            _logger.LogInformation("Handled {RequestType} in {ElapsedMs}ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
            return response;
        }
    }
}
```

## Log Level Guidelines

| Level | When to Use | Example |
|---|---|---|
| `Verbose` | Framework-level details | "Serializing response body" |
| `Debug` | Developer diagnostics | "Cache miss for key {Key}" |
| `Information` | Business events | "Escrow {EscrowId} created" |
| `Warning` | Recoverable issues | "Payment retry {Attempt} for {EscrowId}" |
| `Error` | Failures needing attention | "Payment failed for {EscrowId}: {Error}" |
| `Fatal` | App-ending failures | "Database connection pool exhausted" |

### Per-Environment Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System.Net.Http": "Warning"
      }
    }
  }
}
```

## Sensitive Data Protection

```csharp
// NEVER log sensitive financial or personal data
// BAD:
Log.Information("Payment processed: card {CardNumber}, amount {Amount}",
    payment.CardNumber, payment.Amount); // Leaks card number!

// GOOD: Mask sensitive fields
Log.Information("Payment processed: card ending {CardLast4}, ref {PaymentRef}",
    payment.CardNumber[^4..], payment.ReferenceId);

// Use Serilog destructuring policies to auto-mask
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<PaymentInfo>(p => new
    {
        p.ReferenceId,
        CardNumber = "****" + p.CardNumber[^4..],
        p.Amount,
        p.Currency
    })
    .CreateLogger();
```
