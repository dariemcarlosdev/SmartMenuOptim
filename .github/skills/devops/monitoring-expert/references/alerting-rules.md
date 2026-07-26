# Alerting Rules Reference

> **Load when:** Designing alert rules, thresholds, and escalation paths for PagerDuty or OpsGenie.

## Alert Design Principles

### The Four Golden Signals (Google SRE)

| Signal | What It Measures | Alert Example |
|---|---|---|
| **Latency** | Time to serve a request | p99 > 1s for 5 minutes |
| **Traffic** | Demand on the system | Requests/sec dropped 50% in 5m |
| **Errors** | Rate of failed requests | Error rate > 1% for 5 minutes |
| **Saturation** | Resource utilization | CPU > 80% for 10 minutes |

### Multi-Window, Multi-Burn-Rate Alerts (SLO-Based)

Instead of simple threshold alerts, use burn rate alerts tied to SLOs:

```yaml
# If your SLO is 99.9% availability (error budget: 0.1%)
# A burn rate of 14.4x means you'll exhaust the 30-day budget in 2 hours

# Fast burn — detect severe incidents quickly
- alert: HighErrorBurnRate_2h
  expr: |
    (
      sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1h]))
      / sum(rate(http_server_request_duration_seconds_count[1h]))
    ) > (14.4 * 0.001)  # 14.4x burn rate
  for: 2m
  labels:
    severity: critical
  annotations:
    summary: "High error burn rate — SLO budget exhausting in ~2 hours"
    runbook: "https://wiki.nextruzt.io/runbooks/high-error-rate"

# Slow burn — detect gradual degradation
- alert: HighErrorBurnRate_6h
  expr: |
    (
      sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[6h]))
      / sum(rate(http_server_request_duration_seconds_count[6h]))
    ) > (6 * 0.001)  # 6x burn rate
  for: 15m
  labels:
    severity: warning
  annotations:
    summary: "Elevated error burn rate — SLO budget exhausting in ~5 hours"
```

## Prometheus Alert Rules

### Application Alerts

```yaml
# alerts/escrowapp.rules.yml
groups:
  - name: escrowapp.rules
    rules:
      - alert: HighLatency
        expr: |
          histogram_quantile(0.99,
            rate(http_server_request_duration_seconds_bucket{service="escrow-api"}[5m])
          ) > 1.0
        for: 5m
        labels:
          severity: warning
          service: escrow-api
        annotations:
          summary: "P99 latency > 1s for {{ $labels.service }}"
          description: "P99 latency is {{ $value | humanizeDuration }} (threshold: 1s)"
          runbook: "https://wiki.nextruzt.io/runbooks/high-latency"

      - alert: EscrowCreationFailures
        expr: |
          rate(escrow_failed_total{reason="creation_error"}[5m]) > 0.1
        for: 3m
        labels:
          severity: critical
          service: escrow-api
        annotations:
          summary: "Escrow creation failures exceeding threshold"
          description: "{{ $value | humanize }} failures/sec"
          runbook: "https://wiki.nextruzt.io/runbooks/escrow-creation-failure"

      - alert: PaymentProcessingTimeout
        expr: |
          histogram_quantile(0.95,
            rate(payment_processing_duration_seconds_bucket[5m])
          ) > 5.0
        for: 5m
        labels:
          severity: warning
          service: payment-processor
        annotations:
          summary: "Payment processing p95 > 5s"
```

### Infrastructure Alerts

```yaml
  - name: infrastructure.rules
    rules:
      - alert: HighCPU
        expr: process_cpu_seconds_total > 0.8
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "CPU usage > 80% on {{ $labels.instance }}"

      - alert: HighMemory
        expr: |
          dotnet_gc_heap_size_bytes / (1024 * 1024 * 1024) > 2.0
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "GC heap > 2GB on {{ $labels.instance }}"

      - alert: DatabaseConnectionPoolExhausted
        expr: |
          dotnet_npgsql_idle_connections == 0
          and dotnet_npgsql_busy_connections >= dotnet_npgsql_max_pool_size
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "PostgreSQL connection pool exhausted"

      - alert: BlazorCircuitsHigh
        expr: blazor_circuits_active > 500
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Active Blazor circuits > 500"
```

## Alertmanager Configuration

```yaml
# alertmanager.yml
global:
  resolve_timeout: 5m

route:
  receiver: 'default'
  group_by: ['alertname', 'service']
  group_wait: 30s
  group_interval: 5m
  repeat_interval: 4h
  routes:
    - match:
        severity: critical
      receiver: 'pagerduty-critical'
      repeat_interval: 1h
    - match:
        severity: warning
      receiver: 'slack-warnings'
      repeat_interval: 4h

receivers:
  - name: 'default'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/T00000000/B00000000/XXXX'
        channel: '#alerts-escrowapp'

  - name: 'pagerduty-critical'
    pagerduty_configs:
      - service_key: '{{ .ExternalURL }}'
        severity: 'critical'
        description: '{{ .CommonAnnotations.summary }}'
        details:
          runbook: '{{ .CommonAnnotations.runbook }}'

  - name: 'slack-warnings'
    slack_configs:
      - api_url: 'https://hooks.slack.com/services/T00000000/B00000000/XXXX'
        channel: '#alerts-escrowapp'
        title: '{{ .CommonAnnotations.summary }}'
        text: '{{ .CommonAnnotations.description }}'
```

## Runbook Template

Every alert must link to a runbook:

```markdown
# Runbook: High Error Rate

**Alert:** HighErrorBurnRate_2h
**Severity:** Critical
**Service:** escrow-api

## Symptoms
- Error rate exceeds 1% for sustained period
- Users may see 500 errors on escrow operations

## Triage Steps
1. Check Grafana dashboard: [Escrow Service Health](https://grafana/d/escrow-health)
2. Check recent deployments: `git log --oneline -5`
3. Check database connectivity: `pg_isready -h db-host`
4. Check downstream services: payment gateway, notification service

## Common Causes
| Cause | Diagnostic | Fix |
|---|---|---|
| Database down | `pg_isready` fails | Failover or restart |
| Payment gateway outage | Circuit breaker open | Wait for recovery |
| Bad deployment | Errors correlate with deploy time | Rollback |
| Resource exhaustion | CPU/memory alerts also firing | Scale up |

## Resolution Steps
1. If bad deployment → rollback: `kubectl rollout undo deployment/escrow-api`
2. If database → check RDS/PG status, failover if needed
3. If payment gateway → verify circuit breaker is protecting, notify provider

## Escalation
- L1: On-call engineer (PagerDuty)
- L2: Platform team lead (after 30 min unresolved)
- L3: VP Engineering (after 1 hour, customer impact)
```
