# Debugging Tools Reference

> **Load when:** Setting up debuggers for .NET, JavaScript, or Python projects.

## .NET Debugging Tools

### Visual Studio Debugger

The gold standard for .NET debugging — full IDE integration with breakpoints, watch windows, and diagnostic tools.

**Key Features:**
- **Conditional breakpoints** — Break only when a condition is true: `escrow.Amount > 10000`
- **Hit count breakpoints** — Break after N hits (useful for loop bugs)
- **Tracepoints** — Log messages without stopping: `"Processing escrow {escrowId} at {DateTime.Now}"`
- **Exception settings** — Break on first-chance exceptions by type
- **Data tips** — Hover over variables for instant inspection
- **Parallel Stacks** — Visualize all threads simultaneously

```
// Conditional breakpoint expression for a financial threshold
escrow.Status == EscrowStatus.Pending && escrow.Amount > 50000m
```

### VS Code with C# Dev Kit

Lightweight alternative with full debugging support via the C# extension.

```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/src/EscrowApp/bin/Debug/net10.0/EscrowApp.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/EscrowApp",
            "env": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            },
            "sourceFileMap": {
                "/Views": "${workspaceFolder}/Views"
            }
        },
        {
            "name": "Attach to Process",
            "type": "coreclr",
            "request": "attach",
            "processId": "${command:pickProcess}"
        }
    ]
}
```

### JetBrains Rider

Cross-platform .NET IDE with advanced debugging:
- **Evaluate expression** — Run arbitrary C# during debugging
- **Object graph visualization** — See object relationships
- **Memory view** — Inspect raw memory layout
- **Decompiled code debugging** — Step into framework code

## .NET CLI Diagnostic Tools

Install the full diagnostic toolkit:

```bash
dotnet tool install -g dotnet-dump
dotnet tool install -g dotnet-trace
dotnet tool install -g dotnet-counters
dotnet tool install -g dotnet-gcdump
dotnet tool install -g dotnet-stack
dotnet tool install -g dotnet-sos
```

| Tool | Purpose | When to Use |
|---|---|---|
| `dotnet-dump` | Capture and analyze memory dumps | Crashes, memory leaks, deadlocks |
| `dotnet-trace` | Collect runtime event traces | Performance profiling, event analysis |
| `dotnet-counters` | Real-time performance counters | Quick health check, CPU/memory monitoring |
| `dotnet-gcdump` | GC heap snapshots | Memory leak investigation |
| `dotnet-stack` | Stack traces of running process | Deadlock detection, thread analysis |

## JavaScript Debugging

### Browser DevTools

- **Sources panel** — Set breakpoints, step through code, inspect scope
- **Network panel** — Inspect HTTP requests, timings, payloads
- **Console** — Evaluate expressions, use `console.table()` for structured data
- **Performance panel** — Record and analyze runtime performance

### Node.js Debugging

```bash
# Start with inspector
node --inspect src/server.js

# Start and break on first line
node --inspect-brk src/server.js

# Attach VS Code debugger via launch.json
```

## Database Debugging

### PostgreSQL Query Analysis

```sql
-- Explain a slow query with actual execution stats
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT e.*, p.* FROM escrows e
JOIN payments p ON p.escrow_id = e.id
WHERE e.status = 'pending' AND e.created_at > NOW() - INTERVAL '30 days';

-- Check for lock contention
SELECT pid, usename, query, state, wait_event_type, wait_event
FROM pg_stat_activity
WHERE state = 'active' AND wait_event IS NOT NULL;
```

### EF Core Query Logging

```csharp
// Enable sensitive data logging in development
optionsBuilder
    .UseNpgsql(connectionString)
    .EnableSensitiveDataLogging() // ONLY in Development
    .EnableDetailedErrors()
    .LogTo(Console.WriteLine, LogLevel.Information);
```

## Log Analysis Tools

| Tool | Purpose | Best For |
|---|---|---|
| **Seq** | Structured log server | .NET projects with Serilog |
| **Kibana** | Elasticsearch log visualization | Large-scale log analysis |
| **Grafana Loki** | Log aggregation with labels | Kubernetes/cloud environments |
| **grep / ripgrep** | CLI log searching | Quick local log analysis |

```bash
# Search logs with ripgrep (fast, recursive)
rg "EscrowId.*ERROR" logs/ --glob "*.log" -C 3

# Search with timestamp range
rg "2024-01-1[5-9].*Exception" logs/app.log
```
