---
name: monitoring-expert
description: "Configures monitoring, structured logging, Prometheus/Grafana dashboards, alerting rules, and distributed tracing with OpenTelemetry."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: devops
  triggers: monitoring, observability, logging, metrics, tracing, alerting, Prometheus, Grafana, APM
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: chaos-engineer, deployment-preflight, ci-cd-builder
---

# Monitoring Expert

A full-stack observability specialist that implements the three pillars — metrics, logs, and traces — using Prometheus, Grafana, Serilog, and OpenTelemetry to deliver production-grade monitoring for .NET services.

## When to Use This Skill

- Setting up observability for a new .NET service or Blazor application
- Configuring structured logging with Serilog and log correlation
- Implementing Prometheus metrics (counters, histograms, gauges) for business and infrastructure KPIs
- Building Grafana dashboards with RED (Rate, Errors, Duration) and USE (Utilization, Saturation, Errors) method
- Adding distributed tracing with OpenTelemetry across microservices
- Designing alerting rules with meaningful thresholds and escalation paths
- Debugging observability gaps — "we had an outage but our dashboards showed green"

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Structured Logging | `references/structured-logging.md` | Serilog setup, structured log patterns, enrichers |
| Prometheus Metrics | `references/prometheus-metrics.md` | Counter, Histogram, Gauge, .NET metrics API |
| OpenTelemetry | `references/opentelemetry.md` | Distributed tracing, OTLP, spans, baggage |
| Alerting Rules | `references/alerting-rules.md` | Alert design, thresholds, PagerDuty, OpsGenie |
| Dashboards | `references/dashboards.md` | Grafana, RED/USE method, panel design |

## Core Workflow

### Step 1 — Define Observability Requirements

Understand what needs to be monitored and why.

1. **Identify SLIs (Service Level Indicators)** — Latency, error rate, throughput, saturation for each service.
2. **Define SLOs (Service Level Objectives)** — Target thresholds (e.g., p99 latency < 500ms, error rate < 0.1%).
3. **Map critical paths** — Trace the user journey from UI through API to database for each key workflow.
4. **Inventory existing telemetry** — What logging, metrics, and tracing already exists? What are the gaps?
5. **Determine retention** — How long must logs, metrics, and traces be retained (compliance, debugging needs)?

**✅ Validation checkpoint:** SLIs and SLOs are documented. Critical paths are mapped. Gaps are identified.

### Step 2 — Implement Structured Logging

Set up Serilog with proper enrichment, sinks, and correlation.

1. **Configure Serilog** — Add sinks (Console, Seq, Elasticsearch, or Application Insights) and enrichers (environment, machine, thread).
2. **Establish log levels** — Define what goes at each level: Debug (developer diagnostics), Information (business events), Warning (recoverable issues), Error (failures).
3. **Add correlation IDs** — Ensure every request carries a `CorrelationId` through the full pipeline (HTTP headers → MediatR → EF Core).
4. **Structure log properties** — Use semantic logging: `Log.Information("Escrow {EscrowId} created for {Amount}", id, amount)` — never string interpolation.
5. **Configure log filtering** — Suppress noisy framework logs (e.g., Microsoft.AspNetCore at Warning level).

**✅ Validation checkpoint:** Logs are structured JSON, carry correlation IDs, and flow to the configured sink.

### Step 3 — Add Metrics Instrumentation

Implement application and business metrics using .NET Meters and Prometheus.

1. **Create a custom Meter** — One per bounded context (e.g., `EscrowApp.Escrows`, `EscrowApp.Payments`).
2. **Instrument key operations** — Counter for requests, Histogram for latency, Gauge for active connections.
3. **Add business metrics** — Escrows created per minute, payment processing latency, dispute resolution time.
4. **Expose Prometheus endpoint** — Configure `/metrics` endpoint with `prometheus-net.AspNetCore`.
5. **Validate scraping** — Confirm Prometheus can scrape the endpoint and metrics appear in the TSDB.

**✅ Validation checkpoint:** `/metrics` endpoint returns well-formatted Prometheus exposition format. Business metrics are present.

### Step 4 — Configure Distributed Tracing

Set up OpenTelemetry for cross-service request tracing.

1. **Add OTLP exporter** — Configure OpenTelemetry SDK to export traces to Jaeger, Zipkin, or an OTLP collector.
2. **Instrument HTTP clients** — Add `AddHttpClientInstrumentation()` for outgoing HTTP calls.
3. **Instrument EF Core** — Add `AddEntityFrameworkCoreInstrumentation()` for database query spans.
4. **Add custom spans** — Wrap key business operations in custom `Activity` spans with relevant tags.
5. **Propagate context** — Ensure W3C TraceContext headers propagate across service boundaries.

**✅ Validation checkpoint:** A single user request produces a connected trace across all services it touches.

### Step 5 — Build Dashboards and Alerts

Create actionable dashboards and meaningful alerts.

1. **Build service dashboards** — Use the RED method: Rate (requests/sec), Errors (error rate %), Duration (latency histograms).
2. **Build infrastructure dashboards** — Use the USE method: Utilization, Saturation, Errors for CPU, memory, disk, network.
3. **Design alerts** — Multi-window, multi-burn-rate alerts based on SLO error budgets. Avoid threshold-only alerts.
4. **Configure routing** — Route critical alerts to PagerDuty/OpsGenie; warnings to Slack/Teams.
5. **Add runbook links** — Every alert must link to a runbook describing investigation steps.

**✅ Validation checkpoint:** Dashboards show real data. Alerts fire correctly in staging. Runbooks exist.

## Quick Reference

### Serilog + OpenTelemetry Bootstrap (Program.cs)

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "EscrowApp")
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("EscrowApp"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint();
```

### Custom Business Metric

```csharp
public sealed class EscrowMetrics
{
    private readonly Counter<long> _escrowsCreated;
    private readonly Histogram<double> _escrowProcessingDuration;

    public EscrowMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("EscrowApp.Escrows");
        _escrowsCreated = meter.CreateCounter<long>("escrow.created", "escrows", "Total escrows created");
        _escrowProcessingDuration = meter.CreateHistogram<double>("escrow.processing.duration", "ms",
            "Time to process an escrow from creation to settlement");
    }

    public void RecordCreated() => _escrowsCreated.Add(1);
    public void RecordProcessingDuration(double ms) => _escrowProcessingDuration.Record(ms);
}
```

## Constraints

### MUST DO

- Use structured logging — every log entry must be parseable JSON with typed properties
- Add correlation IDs to all log entries and traces for cross-service request tracking
- Define SLIs and SLOs before building dashboards — dashboards without SLOs are vanity metrics
- Include business metrics alongside infrastructure metrics — technical health alone is insufficient
- Link every alert to a runbook with investigation steps
- Test alerts in staging before enabling in production
- Use semantic conventions for metric and span names (OpenTelemetry naming guidelines)
- Set appropriate log levels — Information for business events, Debug for developer diagnostics

### MUST NOT

- Do not log sensitive data (PII, credentials, tokens, financial amounts in plaintext)
- Do not use string interpolation in log templates — use structured parameters: `Log.Information("User {UserId}", id)`
- Do not create alerts without actionable response — every alert must have a clear "what to do"
- Do not rely on `Console.WriteLine` for production logging
- Do not use high-cardinality labels on Prometheus metrics (user IDs, request IDs as labels)
- Do not set alert thresholds based on gut feeling — use historical data and SLO error budgets
- Do not skip distributed tracing for services that call other services

## Output Template

```markdown
# Observability Configuration

**Service:** {service name}
**Stack:** {.NET 10, PostgreSQL, Redis, etc.}

## SLIs and SLOs

| SLI | Target SLO | Measurement |
|---|---|---|
| Request latency (p99) | < 500ms | Histogram from OTLP traces |
| Error rate | < 0.1% | Counter ratio from metrics |
| Availability | 99.9% | Synthetic + real user monitoring |

## Logging Configuration

- **Sinks:** {Console, Seq, Elasticsearch, etc.}
- **Enrichers:** {CorrelationId, Environment, MachineName}
- **Retention:** {30 days hot, 90 days cold}

## Metrics Catalog

| Metric Name | Type | Labels | Description |
|---|---|---|---|
| `escrow.created` | Counter | `status` | Escrows created |
| `escrow.processing.duration` | Histogram | `type` | Processing time in ms |

## Dashboards

- **Service Health** — RED method for each API endpoint
- **Infrastructure** — USE method for CPU, memory, disk, network
- **Business KPIs** — Escrows, payments, disputes by status and time

## Alert Rules

| Alert | Condition | Severity | Runbook |
|---|---|---|---|
| High Error Rate | error_rate > 1% for 5m | Critical | `runbooks/high-error-rate.md` |
| High Latency | p99 > 1s for 10m | Warning | `runbooks/high-latency.md` |
```

## Integration Notes

### Copilot CLI
Trigger with: `add monitoring`, `configure logging`, `set up tracing`, `create Grafana dashboard`

### Claude
Include this file in project context. Trigger with: "Set up observability for [service]"

### Gemini
Reference via `GEMINI.md` or direct file inclusion. Trigger with: "Add monitoring to [service]"
