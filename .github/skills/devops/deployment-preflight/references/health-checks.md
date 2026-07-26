# Health Checks — ASP.NET Core Health Check Patterns

## Probe Types

| Probe | Purpose | Failure Action |
|---|---|---|
| **Liveness** (`/health/live`) | Process alive, not deadlocked | Restart container |
| **Readiness** (`/health/ready`) | Dependencies up, can serve traffic | Remove from LB |
| **Startup** (`/health/startup`) | Initialization complete | Delay other probes |

## Middleware Setup + Custom PostgreSQL Check

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready", "db"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "cache"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Custom PostgreSQL health check
public sealed class PostgresHealthCheck(
    EscrowDbContext dbContext,
    ILogger<PostgresHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(ct))
                return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
            return HealthCheckResult.Healthy("PostgreSQL is responsive.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostgreSQL health check failed");
            return HealthCheckResult.Unhealthy("PostgreSQL check failed.", exception: ex);
        }
    }
}
```

## Kubernetes Probe Mapping

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 8080 }
  initialDelaySeconds: 10
  periodSeconds: 10
  failureThreshold: 3
readinessProbe:
  httpGet: { path: /health/ready, port: 8080 }
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
startupProbe:
  httpGet: { path: /health/startup, port: 8080 }
  periodSeconds: 3
  failureThreshold: 30  # 90s max startup
```

## Health Check UI (dev/staging only)

```csharp
builder.Services.AddHealthChecksUI(o =>
{
    o.SetEvaluationTimeInSeconds(10);
    o.AddHealthCheckEndpoint("NexTruzt API", "/health/ready");
}).AddInMemoryStorage();

app.MapHealthChecksUI(o => o.UIPath = "/health-ui");
```

## Preflight Checklist

- [ ] `/health/live` returns 200; `/health/ready` returns 200; `/health/startup` returns 200
- [ ] Health check timeout < probe `periodSeconds`
- [ ] Unhealthy returns `503` with structured details
- [ ] Health endpoints excluded from auth middleware
- [ ] Health checks are read-only (no destructive operations)
