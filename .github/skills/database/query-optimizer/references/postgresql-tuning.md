# PostgreSQL Tuning

Reference guide for PostgreSQL server configuration, connection pooling, and maintenance relevant to the NexTruzt escrow platform.

## Key Configuration Parameters

### Memory Settings

| Parameter | Default | Recommended | Purpose |
|---|---|---|---|
| `shared_buffers` | 128MB | 25% of system RAM (e.g., 4GB on 16GB) | PostgreSQL's main page cache |
| `effective_cache_size` | 4GB | 50-75% of system RAM | Planner hint for OS cache availability |
| `work_mem` | 4MB | 32-128MB (depends on concurrency) | Per-operation memory for sorts, hashes, bitmap ops |
| `maintenance_work_mem` | 64MB | 512MB-1GB | Memory for VACUUM, CREATE INDEX, REINDEX |
| `wal_buffers` | -1 (auto) | 64MB | Write-ahead log buffer size |

**work_mem caution:** This is per-sort/hash *per query*. A complex query with 5 sort nodes at `work_mem=128MB` could use 640MB. Formula: `available_RAM / (max_connections × avg_sorts_per_query)`.

### Planner Settings

| Parameter | Default | When to Adjust |
|---|---|---|
| `random_page_cost` | 4.0 | Lower to 1.1-1.5 on SSD storage (makes index scans more attractive) |
| `effective_io_concurrency` | 1 | Raise to 200 on SSD (allows parallel prefetch) |
| `default_statistics_target` | 100 | Raise to 500-1000 for columns with skewed distributions |
| `jit` | on | Disable (`off`) if short OLTP queries dominate — JIT overhead hurts latency |

```sql
-- Check current settings
SHOW shared_buffers;
SHOW work_mem;

-- Set per-session for testing (no restart needed)
SET work_mem = '64MB';
SET random_page_cost = 1.1;
```

## Connection Pooling

### Why Pooling Matters

PostgreSQL forks a process per connection (~10MB RAM each). At 200 concurrent users, that's 2GB of RAM just for connections. The escrow platform should target **20-50 actual PG connections** regardless of application concurrency.

### Npgsql Built-In Pooling (.NET)

```json
{
  "ConnectionStrings": {
    "EscrowDb": "Host=pg-server;Database=escrow;Username=app_user;Password=***;Minimum Pool Size=5;Maximum Pool Size=30;Connection Idle Lifetime=60;Connection Pruning Interval=10;Timeout=15;Command Timeout=30;"
  }
}
```

| Parameter | Recommended | Purpose |
|---|---|---|
| `Minimum Pool Size` | 5 | Keep warm connections ready |
| `Maximum Pool Size` | 20-50 | Cap total connections to PostgreSQL |
| `Connection Idle Lifetime` | 60 | Close idle connections after 60s |
| `Connection Pruning Interval` | 10 | How often to prune idle connections |
| `Timeout` | 15 | Connection acquisition timeout (seconds) |
| `Command Timeout` | 30 | Query execution timeout (seconds) |
| `Multiplexing` | true | Npgsql 7+ — share connections across commands |

### PgBouncer (External Pooler)

Use PgBouncer when multiple services connect to the same PostgreSQL instance:

| Setting | Value | Notes |
|---|---|---|
| `pool_mode` | `transaction` | Releases connection after each transaction (best for web apps) |
| `default_pool_size` | 20 | Connections per user/database pair |
| `max_client_conn` | 200 | Total client connections PgBouncer accepts |
| `server_idle_timeout` | 600 | Close idle server connections after 10min |

> **Warning:** `transaction` mode doesn't support prepared statements by default. Use `DEALLOCATE ALL` or disable prepared statements in Npgsql with `No Reset On Close=true`.

## VACUUM and ANALYZE

### Why VACUUM Matters

PostgreSQL uses MVCC — UPDATEs create new row versions, DELETEs mark rows as dead. VACUUM reclaims dead tuple space. Without it, tables bloat and performance degrades.

### Autovacuum Tuning

```sql
-- Check autovacuum activity
SELECT relname, n_dead_tup, last_autovacuum, last_autoanalyze
FROM pg_stat_user_tables
WHERE schemaname = 'public'
ORDER BY n_dead_tup DESC;
```

For high-write tables like `audit_logs` and `escrow_transactions`:

```sql
ALTER TABLE audit_logs SET (
    autovacuum_vacuum_scale_factor = 0.02,    -- vacuum after 2% dead tuples (default 20%)
    autovacuum_analyze_scale_factor = 0.01,   -- analyze after 1% changes (default 10%)
    autovacuum_vacuum_cost_delay = 2          -- less delay between vacuum I/O operations
);
```

### Manual ANALYZE

Run `ANALYZE` after bulk data loads or schema changes to refresh planner statistics:

```sql
ANALYZE escrow_transactions;
ANALYZE VERBOSE escrow_transactions;  -- shows per-column stats
```

## Table Statistics and pg_stat_statements

### pg_stat_statements — Find Slow Queries

```sql
-- Enable the extension (once)
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Top 10 queries by total time
SELECT query, calls, total_exec_time / 1000 AS total_seconds,
       mean_exec_time AS avg_ms, rows,
       shared_blks_hit, shared_blks_read
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;

-- Top queries by I/O (shared blocks read from disk)
SELECT query, calls, shared_blks_read, shared_blks_hit,
       round(shared_blks_hit::numeric / NULLIF(shared_blks_hit + shared_blks_read, 0), 3) AS cache_hit_ratio
FROM pg_stat_statements
ORDER BY shared_blks_read DESC
LIMIT 10;
```

### Cache Hit Ratio

```sql
-- Overall cache hit ratio (should be > 99% for OLTP)
SELECT sum(heap_blks_hit) / NULLIF(sum(heap_blks_hit) + sum(heap_blks_read), 0) AS ratio
FROM pg_statio_user_tables;
```

**Target:** >99% cache hit ratio for the escrow platform. If below 95%, increase `shared_buffers` or investigate queries that read too many pages.

## Partitioning Strategies

For tables exceeding ~10M rows or where queries consistently filter on a known column.

### Range Partitioning (Time-Based)

Best for `escrow_transactions` and `audit_logs` where queries filter by date range:

```sql
CREATE TABLE escrow_transactions (
    id uuid NOT NULL,
    amount numeric(18,2) NOT NULL,
    status text NOT NULL,
    created_at timestamptz NOT NULL
) PARTITION BY RANGE (created_at);

CREATE TABLE escrow_transactions_2024_q1 PARTITION OF escrow_transactions
    FOR VALUES FROM ('2024-01-01') TO ('2024-04-01');
CREATE TABLE escrow_transactions_2024_q2 PARTITION OF escrow_transactions
    FOR VALUES FROM ('2024-04-01') TO ('2024-07-01');
```

### List Partitioning (Status-Based)

Useful when queries almost always filter on a status enum:

```sql
CREATE TABLE escrow_transactions (
    id uuid NOT NULL,
    status text NOT NULL,
    -- ...
) PARTITION BY LIST (status);

CREATE TABLE escrow_tx_pending PARTITION OF escrow_transactions
    FOR VALUES IN ('pending', 'in_review');
CREATE TABLE escrow_tx_completed PARTITION OF escrow_transactions
    FOR VALUES IN ('completed', 'released');
CREATE TABLE escrow_tx_archived PARTITION OF escrow_transactions
    FOR VALUES IN ('cancelled', 'expired', 'disputed');
```

### Hash Partitioning

For even distribution when there's no natural range or list key:

```sql
CREATE TABLE audit_logs (
    id uuid NOT NULL,
    entity_id uuid NOT NULL,
    -- ...
) PARTITION BY HASH (entity_id);

CREATE TABLE audit_logs_p0 PARTITION OF audit_logs FOR VALUES WITH (MODULUS 4, REMAINDER 0);
CREATE TABLE audit_logs_p1 PARTITION OF audit_logs FOR VALUES WITH (MODULUS 4, REMAINDER 1);
CREATE TABLE audit_logs_p2 PARTITION OF audit_logs FOR VALUES WITH (MODULUS 4, REMAINDER 2);
CREATE TABLE audit_logs_p3 PARTITION OF audit_logs FOR VALUES WITH (MODULUS 4, REMAINDER 3);
```

**Partitioning trade-offs:**
- ✅ Partition pruning eliminates scanning irrelevant data
- ✅ Maintenance (VACUUM, REINDEX) can target individual partitions
- ❌ Cross-partition queries may be slower without partition key in WHERE
- ❌ Unique constraints must include the partition key
- ❌ Foreign keys referencing partitioned tables require PostgreSQL 12+
