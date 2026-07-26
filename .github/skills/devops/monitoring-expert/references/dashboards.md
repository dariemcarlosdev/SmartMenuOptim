# Dashboards Reference

> **Load when:** Building Grafana dashboards using the RED or USE method.

## RED Method — Request-Driven Services

For every service that handles requests, dashboard the three key signals:

| Signal | Metric | Prometheus Query |
|---|---|---|
| **Rate** | Requests per second | `rate(http_server_request_duration_seconds_count[5m])` |
| **Errors** | Error rate % | `100 * rate(...{status=~"5.."}[5m]) / rate(...[5m])` |
| **Duration** | Latency percentiles | `histogram_quantile(0.99, rate(..._bucket[5m]))` |

### Grafana Dashboard JSON (Service Health)

```json
{
  "dashboard": {
    "title": "EscrowApp — Service Health (RED)",
    "panels": [
      {
        "title": "Request Rate",
        "type": "timeseries",
        "targets": [{
          "expr": "sum(rate(http_server_request_duration_seconds_count{service=\"escrow-api\"}[5m])) by (http_route)",
          "legendFormat": "{{http_route}}"
        }],
        "fieldConfig": {
          "defaults": { "unit": "reqps" }
        }
      },
      {
        "title": "Error Rate (%)",
        "type": "timeseries",
        "targets": [{
          "expr": "100 * sum(rate(http_server_request_duration_seconds_count{service=\"escrow-api\",http_response_status_code=~\"5..\"}[5m])) / sum(rate(http_server_request_duration_seconds_count{service=\"escrow-api\"}[5m]))",
          "legendFormat": "Error %"
        }],
        "fieldConfig": {
          "defaults": { "unit": "percent", "thresholds": { "steps": [
            { "color": "green", "value": null },
            { "color": "yellow", "value": 0.5 },
            { "color": "red", "value": 1.0 }
          ]}}
        }
      },
      {
        "title": "Latency Percentiles",
        "type": "timeseries",
        "targets": [
          {
            "expr": "histogram_quantile(0.50, sum(rate(http_server_request_duration_seconds_bucket{service=\"escrow-api\"}[5m])) by (le))",
            "legendFormat": "p50"
          },
          {
            "expr": "histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket{service=\"escrow-api\"}[5m])) by (le))",
            "legendFormat": "p95"
          },
          {
            "expr": "histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket{service=\"escrow-api\"}[5m])) by (le))",
            "legendFormat": "p99"
          }
        ],
        "fieldConfig": {
          "defaults": { "unit": "s" }
        }
      }
    ]
  }
}
```

## USE Method — Resource-Oriented Monitoring

For every infrastructure resource, dashboard:

| Signal | What It Means | Example |
|---|---|---|
| **Utilization** | % of resource capacity in use | CPU 75%, Memory 60% |
| **Saturation** | Work queued because resource is full | ThreadPool queue > 0 |
| **Errors** | Resource-level errors | Disk I/O errors, OOM kills |

### Key .NET Runtime Metrics

```promql
# CPU Utilization
process_cpu_seconds_total

# Memory Utilization
dotnet_gc_heap_size_bytes / (1024 * 1024)  # Heap in MB
process_working_set_bytes / (1024 * 1024)  # Working set in MB

# GC Pressure (Saturation indicator)
rate(dotnet_gc_collections_total{generation="2"}[5m])  # Gen 2 collections

# ThreadPool Saturation
dotnet_threadpool_queue_length          # Items waiting for threads
dotnet_threadpool_threads_count         # Active threads

# Connection Pool
dotnet_npgsql_idle_connections
dotnet_npgsql_busy_connections
```

## Business KPI Dashboard

Beyond technical metrics — track what matters to the business:

```promql
# Escrows created per hour
rate(escrow_created_total[1h]) * 3600

# Average escrow amount
rate(escrow_amount_sum[1h]) / rate(escrow_amount_count[1h])

# Payment success rate
100 * rate(payment_completed_total[5m])
/ (rate(payment_completed_total[5m]) + rate(payment_failed_total[5m]))

# Time to settlement (from creation to release)
histogram_quantile(0.50, rate(escrow_settlement_duration_seconds_bucket[1h]))
```

### Business Dashboard Panels

| Panel | Type | Metric | Notes |
|---|---|---|---|
| Escrows Created (24h) | Stat | `increase(escrow_created_total[24h])` | Show big number |
| Active Escrows | Gauge | `escrow_active_count` | Current count |
| Total Value in Escrow | Stat | `escrow_total_value_usd` | Financial KPI |
| Settlement Time (p50) | Stat | `histogram_quantile(0.50, ...)` | Time to close |
| Dispute Rate | Stat | `rate(escrow_disputed_total[7d]) / rate(escrow_created_total[7d]) * 100` | Business risk |
| Payment Failures | Time Series | `rate(payment_failed_total[5m])` | Broken down by reason |

## Dashboard Design Best Practices

### Layout Principles

```
Row 1: Overview (SLI/SLO status indicators — green/yellow/red)
Row 2: Rate, Errors, Duration (RED method)
Row 3: Resource utilization (USE method)
Row 4: Business KPIs
Row 5: Dependencies (downstream service health)
```

### Variable Templates

Use Grafana template variables for reusable dashboards:

```
Variable: service
Query: label_values(http_server_request_duration_seconds_count, service)
Usage: {service="$service"}

Variable: environment
Query: label_values(http_server_request_duration_seconds_count, environment)
Usage: {environment="$environment"}
```

### SLO Status Panel

Display SLO compliance as a simple traffic light:

```promql
# Remaining error budget (percentage)
100 * (
  1 - (
    sum(increase(http_server_request_duration_seconds_count{status=~"5.."}[30d]))
    / sum(increase(http_server_request_duration_seconds_count[30d]))
  )
) / 0.999  # SLO target

# Green: > 50% budget remaining
# Yellow: 10-50% budget remaining
# Red: < 10% budget remaining
```

## Grafana Provisioning

Automate dashboard deployment with provisioning:

```yaml
# provisioning/dashboards/dashboards.yml
apiVersion: 1
providers:
  - name: 'EscrowApp'
    orgId: 1
    folder: 'EscrowApp'
    type: file
    disableDeletion: true
    editable: false
    options:
      path: /var/lib/grafana/dashboards/escrowapp
      foldersFromFilesStructure: true
```

### Docker Compose for Local Monitoring Stack

```yaml
services:
  prometheus:
    image: prom/prometheus:v2.50.1
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - ./monitoring/alerts:/etc/prometheus/alerts
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana:10.4.1
    volumes:
      - ./monitoring/grafana/provisioning:/etc/grafana/provisioning
      - ./monitoring/grafana/dashboards:/var/lib/grafana/dashboards
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin

  jaeger:
    image: jaegertracing/all-in-one:1.55
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
```
