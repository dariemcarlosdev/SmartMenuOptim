---
applyTo: "EscrowApp/Services/**/*.cs, EscrowApp/Infrastructure/**/*.cs"
---

# Polly Resilience Patterns — NexTruzt.io Escrow Platform

## Retry Policies — Stripe API Calls

- Use **exponential backoff with jitter** for transient failures from the Stripe API.
- Retry on HTTP status codes: `429 Too Many Requests`, `500`, `502`, `503`.
- Retry on `HttpRequestException` and `TimeoutRejectedException`.
- Start with **3 retries**, base delay of 1 second, exponential multiplier of 2, plus random jitter to avoid thundering herd.
- Never retry on `4xx` client errors other than `429` — these indicate invalid requests that will never succeed.

```csharp
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))
            + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
        onRetry: (outcome, delay, attempt, context) =>
        {
            Log.Warning("Stripe retry {Attempt} after {Delay}ms — {StatusCode}",
                attempt, delay.TotalMilliseconds, outcome.Result?.StatusCode);
        });
```

## Circuit Breaker — Stripe Availability

- Break the circuit after **5 consecutive failures** within a **30-second sampling window**.
- Stay in **open** state for **60 seconds** before transitioning to **half-open**.
- In half-open state, allow **one probe request** — if it succeeds, close the circuit; if it fails, re-open.
- When the circuit is open, fail fast with a descriptive `BrokenCircuitException` — do not queue requests.
- Log every state transition (Closed → Open, Open → HalfOpen, HalfOpen → Closed) for operational visibility.

```csharp
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .AdvancedCircuitBreakerAsync(
        failureThreshold: 0.5,
        samplingDuration: TimeSpan.FromSeconds(30),
        minimumThroughput: 5,
        durationOfBreak: TimeSpan.FromSeconds(60),
        onBreak: (outcome, breakDelay) =>
            Log.Error("Stripe circuit OPEN for {BreakDuration}s", breakDelay.TotalSeconds),
        onReset: () => Log.Information("Stripe circuit CLOSED"),
        onHalfOpen: () => Log.Information("Stripe circuit HALF-OPEN"));
```

## Timeout Policies

- **Always** pass and honor `CancellationToken` on every async method in the call chain.
- Apply an **optimistic timeout** of **15 seconds** per Stripe API call — cancels the underlying `HttpClient` request.
- Apply a **pessimistic timeout** of **30 seconds** as an outer policy for the entire payment operation (hold + capture workflow).
- Handle `TimeoutRejectedException` explicitly — return a timeout-specific error result to the caller.

```csharp
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(15),
    TimeoutStrategy.Optimistic,
    onTimeoutAsync: (context, timeout, task) =>
    {
        Log.Warning("Stripe call timed out after {Timeout}s", timeout.TotalSeconds);
        return Task.CompletedTask;
    });
```

## Bulkhead Isolation

- Limit **concurrent payment operations** to prevent a surge of Stripe calls from cascading into resource exhaustion.
- Configure a bulkhead of **10 concurrent executions** with a **queue depth of 5** for burst absorption.
- When the bulkhead rejects a request, return `503 Service Unavailable` with a `Retry-After` header.
- Use separate bulkheads for payment-critical operations vs. non-critical queries (e.g., balance lookups).

```csharp
var bulkheadPolicy = Policy.BulkheadAsync<HttpResponseMessage>(
    maxParallelization: 10,
    maxQueuingActions: 5,
    onBulkheadRejectedAsync: context =>
    {
        Log.Warning("Stripe bulkhead rejected — max concurrency reached");
        return Task.CompletedTask;
    });
```

## IHttpClientFactory + Polly Integration

- Register a **named or typed `HttpClient`** for Stripe via `IHttpClientFactory` — never instantiate `HttpClient` manually.
- Attach Polly policies using `.AddPolicyHandler()` in the registration chain.
- Compose policies using `Policy.WrapAsync()` — order matters: **Bulkhead → Circuit Breaker → Retry → Timeout** (outermost → innermost).

```csharp
services.AddHttpClient("Stripe", client =>
    {
        client.BaseAddress = new Uri("https://api.stripe.com/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {stripeApiKey}");
        client.Timeout = Timeout.InfiniteTimeSpan; // Polly controls timeout
    })
    .AddPolicyHandler(bulkheadPolicy)
    .AddPolicyHandler(circuitBreakerPolicy)
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(timeoutPolicy);
```

## Idempotency Keys — Safe Retries

- **Every** payment mutation (hold, capture, refund) must include an `Idempotency-Key` header.
- Generate the idempotency key **deterministically** from the domain operation: `{TransactionId}:{Operation}:{Attempt}`.
- Store the idempotency key on the `EscrowTransaction` aggregate — check for duplicates before initiating a new operation.
- Stripe honors idempotency keys for 24 hours — retries within that window return the original response, preventing double charges.

```csharp
var idempotencyKey = $"{transaction.Id}:hold:{Guid.NewGuid():N}";
request.Headers.Add("Idempotency-Key", idempotencyKey);
```

## Fallback Policy

- Define a fallback for every policy chain — **never let an unhandled exception propagate silently**.
- On failure after all retries are exhausted, return a structured error result with enough context for the caller to take action.
- Log the final failure at `Error` level with full exception details, correlation ID, and operation context.
- Never swallow exceptions — the fallback must either re-throw a domain-specific exception or return a typed failure result.

```csharp
var fallbackPolicy = Policy<HttpResponseMessage>
    .Handle<BrokenCircuitException>()
    .Or<TimeoutRejectedException>()
    .Or<BulkheadRejectedException>()
    .FallbackAsync(
        fallbackAction: (context, ct) =>
        {
            Log.Error("Stripe call failed after all resilience policies — returning service unavailable");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
```

## Health Checks

- Expose circuit breaker state as an ASP.NET Core `IHealthCheck`.
- Report `Degraded` when the circuit is half-open, `Unhealthy` when open, `Healthy` when closed.
- Register the health check at `/health/stripe` for infrastructure monitoring and alerting.
- Include the circuit breaker state in structured logs for correlation with incident timelines.

```csharp
public sealed class StripeCircuitBreakerHealthCheck(CircuitBreakerStateProvider stateProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        return Task.FromResult(stateProvider.CircuitState switch
        {
            CircuitState.Closed => HealthCheckResult.Healthy("Stripe circuit closed"),
            CircuitState.HalfOpen => HealthCheckResult.Degraded("Stripe circuit half-open"),
            _ => HealthCheckResult.Unhealthy("Stripe circuit open")
        });
    }
}
```

## Configuration via Options Pattern

- **Never hardcode** policy values (retry count, timeout duration, concurrency limits).
- Bind resilience settings from configuration using `IOptions<StripeResilienceOptions>`.
- Allow environment-specific overrides (e.g., shorter timeouts in tests, higher retry counts in production).

```csharp
public sealed class StripeResilienceOptions
{
    public const string SectionName = "Stripe:Resilience";

    public int RetryCount { get; init; } = 3;
    public int RetryBaseDelaySeconds { get; init; } = 1;
    public int TimeoutSeconds { get; init; } = 15;
    public int CircuitBreakerFailureThreshold { get; init; } = 5;
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 60;
    public int BulkheadMaxParallelization { get; init; } = 10;
    public int BulkheadMaxQueuingActions { get; init; } = 5;
}
```

```jsonc
// appsettings.json
{
  "Stripe": {
    "Resilience": {
      "RetryCount": 3,
      "TimeoutSeconds": 15,
      "CircuitBreakerBreakDurationSeconds": 60,
      "BulkheadMaxParallelization": 10
    }
  }
}
```

## General Rules

- Compose policies in a **PolicyWrap** — do not apply policies ad-hoc in individual service methods.
- Use `Context` to pass correlation IDs and operation metadata through the policy chain for structured logging.
- Test resilience behavior: use Simmy (Polly's chaos engineering library) to inject faults in integration tests.
- Review Polly policy telemetry in production — alert on elevated retry rates or frequent circuit breaks.
