# OpenTelemetry Reference

> **Load when:** Implementing distributed tracing, OTLP export, or custom spans.

## OpenTelemetry Setup for .NET

### Full Configuration

```csharp
// Program.cs — Complete OpenTelemetry setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "EscrowApp",
            serviceVersion: typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
            options.FilterHttpRequestMessage = request =>
                request.RequestUri?.Host != "localhost"; // skip local calls
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true; // include SQL in spans
        })
        .AddSource("EscrowApp.*") // custom activity sources
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
            options.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("EscrowApp.*") // custom meters
        .AddPrometheusExporter());
```

### Required Packages

```xml
<ItemGroup>
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.*" />
    <PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.*" />
</ItemGroup>
```

## Custom Activity Sources (Spans)

### Business Operation Tracing

```csharp
public sealed class EscrowActivitySource
{
    public static readonly ActivitySource Source = new("EscrowApp.Escrows", "1.0.0");

    public static Activity? StartCreateEscrow(string buyerId, string sellerId, decimal amount)
    {
        var activity = Source.StartActivity("escrow.create", ActivityKind.Internal);
        activity?.SetTag("escrow.buyer_id", buyerId);
        activity?.SetTag("escrow.seller_id", sellerId);
        activity?.SetTag("escrow.amount", amount);
        activity?.SetTag("escrow.currency", "USD");
        return activity;
    }

    public static Activity? StartProcessPayment(string escrowId, string provider)
    {
        var activity = Source.StartActivity("payment.process", ActivityKind.Client);
        activity?.SetTag("escrow.id", escrowId);
        activity?.SetTag("payment.provider", provider);
        return activity;
    }
}
```

### Usage in MediatR Handlers

```csharp
public sealed class CreateEscrowHandler : IRequestHandler<CreateEscrowCommand, EscrowResult>
{
    private readonly IEscrowRepository _repository;

    public async Task<EscrowResult> Handle(CreateEscrowCommand cmd, CancellationToken ct)
    {
        using var activity = EscrowActivitySource.StartCreateEscrow(
            cmd.BuyerId, cmd.SellerId, cmd.Amount);

        try
        {
            var escrow = Escrow.Create(cmd.BuyerId, cmd.SellerId, cmd.Amount);
            await _repository.AddAsync(escrow, ct);

            activity?.SetTag("escrow.id", escrow.Id.Value);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new EscrowResult(escrow.Id);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## Context Propagation

### W3C TraceContext (Default)

OpenTelemetry uses W3C TraceContext by default. The `traceparent` header propagates across HTTP boundaries:

```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
              │  │                                │                  │
              │  trace-id (128-bit)               span-id (64-bit)  sampled
              version
```

### Baggage for Cross-Service Context

```csharp
// Add business context that propagates across all services
Baggage.SetBaggage("escrow.id", escrowId);
Baggage.SetBaggage("tenant.id", tenantId);

// Read baggage in downstream services
var escrowId = Baggage.GetBaggage("escrow.id");
```

### MediatR Tracing Behavior

```csharp
public sealed class TracingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly ActivitySource ActivitySource = new("EscrowApp.MediatR");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity(
            $"MediatR.{typeof(TRequest).Name}",
            ActivityKind.Internal);

        activity?.SetTag("mediatr.request_type", typeof(TRequest).FullName);

        try
        {
            var response = await next();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## OTLP Collector Configuration

```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: "0.0.0.0:4317"
      http:
        endpoint: "0.0.0.0:4318"

processors:
  batch:
    timeout: 5s
    send_batch_size: 1024

exporters:
  jaeger:
    endpoint: "jaeger:14250"
    tls:
      insecure: true
  prometheus:
    endpoint: "0.0.0.0:8889"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

## Span Naming Conventions

| Category | Pattern | Example |
|---|---|---|
| HTTP Server | `{HTTP_METHOD} {route}` | `POST /api/escrows` |
| HTTP Client | `HTTP {method}` | `HTTP POST` |
| Database | `{operation} {table}` | `SELECT escrows` |
| Message Queue | `{queue} {operation}` | `escrow-events publish` |
| Custom Business | `{domain}.{operation}` | `escrow.create` |
