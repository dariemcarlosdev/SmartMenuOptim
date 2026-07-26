# Prometheus Metrics Reference

> **Load when:** Implementing counters, histograms, gauges, or the .NET metrics API with Prometheus.

## .NET Metrics API + Prometheus

### Setup with prometheus-net

```csharp
// Program.cs — Add Prometheus metrics endpoint
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint(); // Exposes /metrics
```

### Alternative: prometheus-net Direct

```xml
<PackageReference Include="prometheus-net.AspNetCore" Version="8.*" />
```

```csharp
app.UseHttpMetrics();        // Auto HTTP metrics
app.MapMetrics();            // /metrics endpoint
```

## Metric Types

### Counter — Monotonically Increasing Values

Use for: total requests, errors, events processed.

```csharp
public sealed class EscrowMetrics
{
    private readonly Counter<long> _escrowsCreated;
    private readonly Counter<long> _escrowsFailed;

    public EscrowMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("EscrowApp.Escrows");
        _escrowsCreated = meter.CreateCounter<long>(
            "escrow.created.total",
            unit: "escrows",
            description: "Total number of escrows created");
        _escrowsFailed = meter.CreateCounter<long>(
            "escrow.failed.total",
            unit: "escrows",
            description: "Total number of failed escrow operations");
    }

    public void RecordCreated(string escrowType)
        => _escrowsCreated.Add(1, new KeyValuePair<string, object?>("type", escrowType));

    public void RecordFailed(string reason)
        => _escrowsFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));
}
```

### Histogram — Distribution of Values

Use for: request latency, response sizes, processing durations.

```csharp
public sealed class PaymentMetrics
{
    private readonly Histogram<double> _processingDuration;
    private readonly Histogram<double> _paymentAmount;

    public PaymentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("EscrowApp.Payments");

        _processingDuration = meter.CreateHistogram<double>(
            "payment.processing.duration",
            unit: "ms",
            description: "Payment processing duration in milliseconds");

        _paymentAmount = meter.CreateHistogram<double>(
            "payment.amount",
            unit: "USD",
            description: "Payment amounts processed");
    }

    public void RecordDuration(double ms, string provider)
        => _processingDuration.Record(ms, new KeyValuePair<string, object?>("provider", provider));

    public void RecordAmount(decimal amount)
        => _paymentAmount.Record((double)amount);
}
```

### Gauge — Point-in-Time Values

Use for: active connections, queue depth, cache size.

```csharp
public sealed class SystemMetrics
{
    private readonly ObservableGauge<int> _activeCircuits;
    private readonly ObservableGauge<long> _queueDepth;

    public SystemMetrics(IMeterFactory meterFactory, ICircuitTracker tracker, IQueueMonitor queue)
    {
        var meter = meterFactory.Create("EscrowApp.System");

        _activeCircuits = meter.CreateObservableGauge(
            "blazor.circuits.active",
            () => tracker.ActiveCount,
            unit: "circuits",
            description: "Number of active Blazor Server circuits");

        _queueDepth = meter.CreateObservableGauge(
            "escrow.queue.depth",
            () => queue.PendingCount,
            unit: "items",
            description: "Number of pending escrow operations in queue");
    }
}
```

## Naming Conventions

Follow OpenTelemetry semantic conventions:

```
{namespace}.{entity}.{action}[.{suffix}]

Examples:
  escrow.created.total          — Counter
  escrow.processing.duration    — Histogram
  payment.amount                — Histogram
  blazor.circuits.active        — Gauge
  http.server.request.duration  — Histogram (built-in)
```

**Label Guidelines:**
- Keep cardinality low (< 100 unique values per label)
- Good labels: `status`, `type`, `method`, `endpoint`
- Bad labels: `user_id`, `escrow_id`, `request_id` (high cardinality)

## Prometheus Scrape Configuration

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'escrowapp'
    scrape_interval: 15s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['escrowapp:8080']
        labels:
          environment: 'production'
          service: 'escrow-api'

  - job_name: 'escrowapp-blazor'
    scrape_interval: 15s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['escrowapp-web:8080']
        labels:
          environment: 'production'
          service: 'escrow-web'
```

## PromQL Query Examples

```promql
# Request rate (requests per second)
rate(http_server_request_duration_seconds_count{service="escrow-api"}[5m])

# Error rate percentage
100 * rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])
/ rate(http_server_request_duration_seconds_count[5m])

# P99 latency
histogram_quantile(0.99, rate(http_server_request_duration_seconds_bucket{service="escrow-api"}[5m]))

# Escrows created per minute
rate(escrow_created_total[1m]) * 60

# Active Blazor circuits
blazor_circuits_active{environment="production"}
```

## DI Registration Pattern

```csharp
// Register all metrics as singletons
public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddEscrowMetrics(this IServiceCollection services)
    {
        services.AddSingleton<EscrowMetrics>();
        services.AddSingleton<PaymentMetrics>();
        services.AddSingleton<SystemMetrics>();
        return services;
    }
}

// Usage in a MediatR handler
public sealed class CreateEscrowHandler(
    IEscrowRepository repository,
    EscrowMetrics metrics) : IRequestHandler<CreateEscrowCommand, EscrowResult>
{
    public async Task<EscrowResult> Handle(CreateEscrowCommand request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var escrow = Escrow.Create(request.BuyerId, request.SellerId, request.Amount);
            await repository.AddAsync(escrow, ct);
            metrics.RecordCreated(request.Type);
            return new EscrowResult(escrow.Id);
        }
        catch (Exception)
        {
            metrics.RecordFailed("creation_error");
            throw;
        }
        finally
        {
            // Always record duration, even on failure
            metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "create");
        }
    }
}
```
