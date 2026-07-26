# Experiment Design Reference

> **Load when:** Designing chaos experiment hypotheses, blast radius, and rollback criteria.

## The Chaos Experiment Lifecycle

```
Define Hypothesis → Set Blast Radius → Establish Abort Criteria
       → Prepare Rollback → Execute → Observe → Analyze → Harden
```

## Writing a Good Hypothesis

A chaos hypothesis must be **specific**, **measurable**, and **falsifiable**.

### Template

```
Given: {steady state definition}
When:  {specific failure is injected}
Then:  {expected system behavior}
With:  {acceptable impact bounds}
```

### Examples for NexTruzt Escrow Platform

```markdown
**Hypothesis 1: Payment Gateway Outage**
Given: The escrow service processes ~100 payments/minute with < 500ms p99 latency
When:  The Stripe payment gateway returns HTTP 503 for all requests for 60 seconds
Then:  The circuit breaker opens within 5 seconds, pending payments are queued,
       and the system recovers within 30 seconds after the gateway is restored
With:  Zero data loss, < 2% customer-visible errors, no manual intervention required

**Hypothesis 2: Database Connection Pool Exhaustion**
Given: PostgreSQL connection pool is configured with max 100 connections
When:  50 connections are artificially consumed, reducing available connections to 50
Then:  Request latency increases but stays below 2s, and no requests fail with
       connection timeout errors due to connection pool queuing
With:  No failed escrow operations, < 3x latency increase

**Hypothesis 3: Blazor Server SignalR Reconnection**
Given: 200 active Blazor Server circuits with ongoing user sessions
When:  The SignalR backplane (Redis) is restarted, dropping all connections
Then:  Circuits automatically reconnect within 10 seconds, no user data is lost,
       and the UI shows a reconnection indicator during the outage
With:  < 5% of users need to manually refresh, no duplicate form submissions
```

## Blast Radius Control

### Scope Levels (Start Small, Expand Gradually)

| Level | Scope | Example | Risk |
|---|---|---|---|
| 1 | Single request | Inject latency on 1% of requests | Minimal |
| 2 | Single pod/instance | Kill one container replica | Low |
| 3 | Single service | All instances of one service affected | Medium |
| 4 | Single zone/AZ | Simulate availability zone failure | High |
| 5 | Cross-service | Multiple services impacted simultaneously | Very High |

### Blast Radius Estimation

```markdown
## Blast Radius Assessment: Payment Gateway Outage

**Direct Impact:**
- Payment processing service (3 pods) — cannot process payments
- Estimated affected users: ~100/minute during peak

**Indirect Impact:**
- Escrow creation — delayed (depends on payment hold)
- Dashboard — shows stale payment status
- Notifications — delayed confirmation emails

**Unaffected:**
- User authentication (independent)
- Escrow viewing/read operations (cached)
- Admin panel (no payment dependency)

**Maximum Blast Radius:** 30% of user-facing operations for duration of experiment
```

## Abort Criteria

### Automated Abort Rules

Define measurable conditions that immediately halt the experiment:

```yaml
abort_criteria:
  - name: high_error_rate
    metric: "rate(http_server_request_duration_seconds_count{status=~'5..'}[1m])"
    threshold: "> 0.05"  # 5% error rate
    action: "stop_experiment"

  - name: data_inconsistency
    check: "SELECT COUNT(*) FROM escrows WHERE status = 'inconsistent'"
    threshold: "> 0"
    action: "stop_experiment + alert_oncall"

  - name: high_latency
    metric: "histogram_quantile(0.99, rate(http_server_request_duration_seconds_bucket[1m]))"
    threshold: "> 5.0"  # 5 second p99
    action: "stop_experiment"

  - name: manual_abort
    trigger: "Any team member calls abort"
    action: "stop_experiment"
```

### Abort Procedure

```markdown
1. **STOP** fault injection immediately (kill the chaos tool process)
2. **VERIFY** system is recovering (check dashboards within 60s)
3. **ROLLBACK** if system is not recovering:
   - Restart affected pods: `kubectl rollout restart deployment/<name>`
   - Clear circuit breakers: restart application instances
   - Drain queues if needed
4. **NOTIFY** team: post in #incidents channel with summary
5. **DOCUMENT** what happened and why abort was triggered
```

## Rollback Procedures

### Template

```markdown
## Rollback: {Experiment Name}

**Time to Rollback:** < 60 seconds

### Steps

1. Stop chaos injection:
   ```bash
   kubectl delete chaosengine <name> -n escrow
   # OR
   curl -X POST http://chaos-controller/api/experiments/<id>/stop
   ```

2. Verify recovery (within 60s):
   ```bash
   # Check error rate returns to baseline
   curl -s http://prometheus:9090/api/v1/query?query=rate(http_errors_total[1m])

   # Check all pods are healthy
   kubectl get pods -n escrow
   ```

3. If not recovering automatically:
   ```bash
   # Force restart affected service
   kubectl rollout restart deployment/escrow-api -n escrow

   # Wait for rollout
   kubectl rollout status deployment/escrow-api -n escrow --timeout=120s
   ```

4. Verify data integrity:
   ```sql
   -- Check for orphaned or inconsistent records
   SELECT COUNT(*) FROM escrows WHERE status NOT IN ('pending','active','completed','cancelled');
   SELECT COUNT(*) FROM payments WHERE escrow_id NOT IN (SELECT id FROM escrows);
   ```
```

## Experiment Progression Framework

Gradually increase experiment complexity as confidence grows:

```markdown
## Level 1: Baseline (Week 1-2)
- [ ] Latency injection: 200ms added to payment API (5% of requests)
- [ ] Single pod termination: Kill one escrow-api replica
- [ ] DNS delay: 100ms added to internal DNS resolution

## Level 2: Component (Week 3-4)
- [ ] Database failover: Promote read replica to primary
- [ ] Cache flush: Clear entire Redis cache
- [ ] Circuit breaker validation: Force-open payment circuit breaker

## Level 3: Service (Week 5-6)
- [ ] Payment gateway outage: Block all traffic to Stripe for 60s
- [ ] Message broker restart: Restart RabbitMQ/SQS
- [ ] Authentication service delay: 5s latency on token validation

## Level 4: Infrastructure (Week 7-8)
- [ ] Availability zone loss: Drain all pods in one AZ
- [ ] Network partition: Block traffic between services
- [ ] Clock skew: Inject NTP drift on service instances
```

## Steady State Definition

Before any experiment, document what "normal" looks like:

```markdown
## Steady State: EscrowApp Production

| Metric | Normal Range | Measurement |
|---|---|---|
| Request rate | 50-200 req/s | Prometheus: rate(http_requests_total[5m]) |
| Error rate | < 0.1% | Prometheus: error ratio |
| P99 latency | < 500ms | Prometheus: histogram_quantile(0.99, ...) |
| Active circuits | 100-300 | Prometheus: blazor_circuits_active |
| DB connections (busy) | 10-40 | Prometheus: npgsql_busy_connections |
| Payment success rate | > 99.5% | Prometheus: payment_success_ratio |
| CPU utilization | 20-60% | Prometheus: process_cpu_seconds_total |
| Memory (heap) | 500MB-1.5GB | Prometheus: dotnet_gc_heap_size_bytes |
```
