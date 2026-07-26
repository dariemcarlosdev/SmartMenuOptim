# Chaos Tools Reference

> **Load when:** Selecting or configuring chaos engineering tools — Chaos Monkey, Gremlin, toxiproxy, Simmy.

## Tool Selection Matrix

| Tool | Scope | Ease of Use | Best For | Cost |
|---|---|---|---|---|
| **Simmy (Polly)** | Application | Easy | .NET apps, unit testing resilience | Free |
| **toxiproxy** | Network | Easy | TCP proxy-level chaos, local dev | Free |
| **Litmus** | Kubernetes | Medium | K8s-native experiments | Free |
| **Chaos Monkey** | Cloud | Medium | Random instance termination | Free |
| **Gremlin** | Any | Easy | Enterprise chaos platform | Paid |
| **Chaos Mesh** | Kubernetes | Medium | K8s experiments with dashboard | Free |
| **AWS FIS** | AWS | Easy | AWS-native fault injection | Pay-per-use |

## Simmy (Polly v8 Chaos Strategies)

The recommended chaos tool for .NET applications — integrates directly with Polly resilience pipelines.

### Setup

```xml
<PackageReference Include="Polly" Version="8.*" />
<PackageReference Include="Polly.Extensions.Http" Version="8.*" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="8.*" />
```

### Fault Injection

```csharp
// Inject exceptions on a percentage of calls
builder.Services.AddResiliencePipeline("chaos-payment", (pipelineBuilder, context) =>
{
    pipelineBuilder.AddChaosFault(new ChaosFaultStrategyOptions
    {
        InjectionRate = 0.05,  // 5% of calls
        Enabled = true,
        FaultGenerator = static args =>
        {
            var exception = new HttpRequestException("Simulated payment gateway failure");
            return ValueTask.FromResult<Exception?>(exception);
        }
    });
});
```

### Latency Injection

```csharp
// Add artificial delays
pipelineBuilder.AddChaosLatency(new ChaosLatencyStrategyOptions
{
    InjectionRate = 0.10,  // 10% of calls
    Enabled = true,
    Latency = TimeSpan.FromSeconds(3)
});
```

### Outcome Injection

```csharp
// Return specific HTTP responses
pipelineBuilder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
{
    InjectionRate = 0.05,
    Enabled = true,
    OutcomeGenerator = static args =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
        return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
    }
});
```

### Runtime Control via Feature Flags

```csharp
// Toggle chaos at runtime without redeployment
services.AddHttpClient("PaymentGateway")
    .AddResilienceHandler("payment-chaos", (builder, context) =>
    {
        var featureManager = context.ServiceProvider.GetRequiredService<IFeatureManager>();

        builder.AddChaosFault(new ChaosFaultStrategyOptions
        {
            EnabledGenerator = async args =>
                await featureManager.IsEnabledAsync("Chaos.PaymentFault"),
            InjectionRateGenerator = async args =>
            {
                var config = await featureManager.GetFeatureFlagValueAsync<double>("Chaos.PaymentFaultRate");
                return config;
            },
            FaultGenerator = static args =>
                ValueTask.FromResult<Exception?>(new TimeoutException("Chaos: payment timeout"))
        });
    });
```

## toxiproxy

TCP proxy for simulating network conditions. Works at the connection level — language-agnostic.

### Setup with Docker Compose

```yaml
services:
  toxiproxy:
    image: ghcr.io/shopify/toxiproxy:2.9.0
    ports:
      - "8474:8474"    # API port
      - "15432:15432"  # PostgreSQL proxy
      - "16379:16379"  # Redis proxy

  escrowapp:
    environment:
      # Point app at toxiproxy instead of real services
      - ConnectionStrings__Default=Host=toxiproxy;Port=15432;Database=escrow
      - Redis__ConnectionString=toxiproxy:16379
```

### Configure Proxies

```bash
# Create proxies
toxiproxy-cli create postgres -l 0.0.0.0:15432 -u postgres:5432
toxiproxy-cli create redis -l 0.0.0.0:16379 -u redis:6379

# Add toxics (chaos effects)
toxiproxy-cli toxic add postgres -t latency -a latency=500 -a jitter=100
toxiproxy-cli toxic add redis -t timeout -a timeout=3000

# List active toxics
toxiproxy-cli inspect postgres

# Remove toxics
toxiproxy-cli toxic remove postgres -n latency_downstream

# Disable proxy entirely (simulate outage)
toxiproxy-cli toggle postgres
```

### Available Toxics

| Toxic | Effect | Key Attributes |
|---|---|---|
| `latency` | Add delay | `latency` (ms), `jitter` (ms) |
| `bandwidth` | Limit throughput | `rate` (KB/s) |
| `slow_close` | Delay connection close | `delay` (ms) |
| `timeout` | Stop forwarding data | `timeout` (ms) |
| `slicer` | Slice data into small bits | `average_size`, `size_variation`, `delay` |
| `limit_data` | Close connection after N bytes | `bytes` |

## Chaos Mesh (Kubernetes)

### Install

```bash
helm repo add chaos-mesh https://charts.chaos-mesh.org
helm install chaos-mesh chaos-mesh/chaos-mesh -n chaos-mesh --create-namespace
```

### Network Partition Experiment

```yaml
apiVersion: chaos-mesh.org/v1alpha1
kind: NetworkChaos
metadata:
  name: escrow-network-partition
  namespace: escrow
spec:
  action: partition
  mode: all
  selector:
    namespaces: [escrow]
    labelSelectors:
      app: escrow-api
  direction: both
  target:
    selector:
      namespaces: [escrow]
      labelSelectors:
        app: postgres
    mode: all
  duration: "60s"
```

### Time Skew Experiment

```yaml
apiVersion: chaos-mesh.org/v1alpha1
kind: TimeChaos
metadata:
  name: escrow-time-skew
  namespace: escrow
spec:
  mode: one
  selector:
    namespaces: [escrow]
    labelSelectors:
      app: escrow-api
  timeOffset: "-5m"  # Clock 5 minutes behind
  duration: "120s"
```

## Choosing the Right Tool

```markdown
## Decision Tree

Q: Is this a .NET application?
├─ Yes → Start with Simmy for application-level chaos
│        Add toxiproxy for network-level chaos
│
Q: Running on Kubernetes?
├─ Yes → Use Litmus or Chaos Mesh for infrastructure chaos
│        Combine with Simmy for application-level
│
Q: Running on AWS?
├─ Yes → Consider AWS FIS for cloud-native experiments
│        Combine with Simmy for application-level
│
Q: Need enterprise features (RBAC, audit, scheduling)?
├─ Yes → Evaluate Gremlin (paid)
│
Q: Just want to test resilience locally?
└─ Use toxiproxy + Simmy (both free, easy to set up)
```
