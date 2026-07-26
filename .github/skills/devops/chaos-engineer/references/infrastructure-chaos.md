# Infrastructure Chaos Reference

> **Load when:** Injecting server, network, or availability zone failures.

## Network Chaos

### Latency Injection

Add artificial latency to simulate slow network or distant dependencies.

**With toxiproxy (application-level):**

```bash
# Create a proxy in front of PostgreSQL
toxiproxy-cli create postgres_proxy -l 0.0.0.0:15432 -u db-host:5432

# Add 200ms latency to all database traffic
toxiproxy-cli toxic add postgres_proxy -t latency -a latency=200 -a jitter=50

# Add latency to only 10% of connections
toxiproxy-cli toxic add postgres_proxy -t latency -a latency=500 -a jitter=100 --toxicity 0.1

# Remove the toxic
toxiproxy-cli toxic remove postgres_proxy -n latency_downstream
```

**With tc (Linux network level):**

```bash
# Add 100ms latency to all traffic on eth0
tc qdisc add dev eth0 root netem delay 100ms 20ms distribution normal

# Add 5% packet loss
tc qdisc add dev eth0 root netem loss 5%

# Combine latency and loss
tc qdisc add dev eth0 root netem delay 100ms 20ms loss 5%

# Remove network chaos
tc qdisc del dev eth0 root
```

### Connection Failures

```bash
# Block all traffic to payment gateway (iptables)
iptables -A OUTPUT -d payments.stripe.com -j DROP

# Block specific port (PostgreSQL)
iptables -A OUTPUT -p tcp --dport 5432 -j DROP

# Remove rules
iptables -D OUTPUT -d payments.stripe.com -j DROP
iptables -D OUTPUT -p tcp --dport 5432 -j DROP
```

### DNS Failures

```bash
# Simulate DNS resolution failure
# Add to /etc/hosts to override DNS
echo "127.0.0.1 payments.stripe.com" >> /etc/hosts

# Or use toxiproxy to proxy DNS
toxiproxy-cli create dns_proxy -l 0.0.0.0:5353 -u 8.8.8.8:53
toxiproxy-cli toxic add dns_proxy -t timeout -a timeout=5000
```

## Server/Process Chaos

### Process Termination

```bash
# Graceful shutdown (SIGTERM)
kill -TERM <PID>

# Forceful kill (SIGKILL) — no cleanup
kill -KILL <PID>

# For .NET processes — find and kill
dotnet_pid=$(pgrep -f "EscrowApp.dll")
kill -TERM $dotnet_pid

# Simulate OOM kill
# Allocate memory until the OOM killer activates the target process
stress-ng --vm 1 --vm-bytes 95% --timeout 30s
```

### CPU Stress

```bash
# Consume all CPU cores for 30 seconds
stress-ng --cpu $(nproc) --timeout 30s

# Consume specific percentage of CPU
stress-ng --cpu 1 --cpu-load 80 --timeout 60s

# .NET-specific: Trigger aggressive GC under CPU pressure
# This tests how the app behaves when GC pauses are frequent
stress-ng --cpu $(nproc) --cpu-load 90 --timeout 30s &
```

### Disk I/O Chaos

```bash
# Fill disk to 95% capacity
fallocate -l $(df --output=avail / | tail -1 | awk '{print int($1*0.90)}')K /disk-filler

# Slow disk I/O
tc qdisc add dev sda root delay 100ms

# Remove disk filler
rm /disk-filler
```

## Database Chaos

### PostgreSQL Chaos Scenarios

```sql
-- Scenario 1: Lock contention — hold a lock on a critical table
BEGIN;
LOCK TABLE escrows IN EXCLUSIVE MODE;
-- Hold lock for experiment duration
SELECT pg_sleep(30);
ROLLBACK;

-- Scenario 2: Connection exhaustion — consume all connections
-- Create connections up to max_connections - 5
SELECT * FROM generate_series(1, 95) AS i, pg_sleep(30);

-- Scenario 3: Slow queries — inject CPU-intensive query
SELECT * FROM escrows e1
CROSS JOIN escrows e2
WHERE e1.id != e2.id
LIMIT 1000000;
```

### Redis Chaos

```bash
# Simulate Redis down
redis-cli SHUTDOWN NOSAVE

# Simulate Redis slow (add latency)
redis-cli CONFIG SET slowlog-log-slower-than 0
redis-cli DEBUG SLEEP 5  # Block Redis for 5 seconds

# Flush all data (cache invalidation chaos)
redis-cli FLUSHALL

# Simulate high memory (force eviction)
redis-cli CONFIG SET maxmemory 1mb
redis-cli CONFIG SET maxmemory-policy allkeys-lru
```

## Application-Level Chaos with Simmy (.NET)

Inject faults directly in the .NET application using Polly's Simmy extension:

```csharp
// Register chaos policies for specific environments
builder.Services.AddHttpClient("PaymentGateway")
    .AddResilienceHandler("chaos-pipeline", (builder, context) =>
    {
        var env = context.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!env.IsProduction())
        {
            // Inject HTTP 503 on 5% of requests
            builder.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
            {
                InjectionRate = 0.05,
                Enabled = true,
                OutcomeGenerator = static args =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                    return ValueTask.FromResult<Outcome<HttpResponseMessage>?>(Outcome.FromResult(response));
                }
            });

            // Inject 2s latency on 10% of requests
            builder.AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                InjectionRate = 0.10,
                Enabled = true,
                Latency = TimeSpan.FromSeconds(2)
            });
        }
    });
```

### Feature Flag Controlled Chaos

```csharp
// Control chaos injection via feature flags for safe experimentation
public sealed class FeatureFlagChaosController
{
    private readonly IFeatureManager _features;

    public async Task<bool> IsChaosEnabledAsync(string experimentName)
    {
        return await _features.IsEnabledAsync($"Chaos_{experimentName}");
    }

    public async Task<double> GetInjectionRateAsync(string experimentName)
    {
        var config = await _features.GetFeatureFlagValueAsync<ChaosConfig>($"Chaos_{experimentName}");
        return config?.InjectionRate ?? 0.0;
    }
}
```

## Safety Checklist Before Infrastructure Chaos

```markdown
- [ ] Blast radius is confined to experiment scope
- [ ] Monitoring dashboards are open and visible
- [ ] On-call engineer is aware and available
- [ ] Rollback commands are prepared and tested
- [ ] Abort criteria are defined and automated
- [ ] Experiment will NOT run during peak traffic
- [ ] Data backup verified (if database chaos)
- [ ] Customer notification prepared (if production)
```
