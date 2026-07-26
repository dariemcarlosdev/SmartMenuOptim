# Index Strategies — PostgreSQL

Reference guide for designing effective indexes on the NexTruzt escrow platform's PostgreSQL database.

## Composite Index Column Order

The golden rule: **Equality → Range → Sort**

```sql
-- Query pattern:
SELECT id, amount FROM escrow_transactions
WHERE status = 'pending'           -- equality
  AND created_at > '2024-01-01'    -- range
ORDER BY created_at DESC;          -- sort

-- ✅ Optimal index: equality columns first, then range/sort
CREATE INDEX CONCURRENTLY ix_escrow_tx_status_created
ON escrow_transactions (status, created_at DESC);

-- ❌ Wrong order: range column first breaks equality filtering
CREATE INDEX ix_bad ON escrow_transactions (created_at DESC, status);
```

**Why order matters:** PostgreSQL traverses the B-tree left to right. Equality predicates narrow the search to a contiguous range of leaf pages. Putting range/sort columns after equality columns allows the planner to read a minimal contiguous slice.

## Covering Indexes (INCLUDE)

Index-only scans avoid heap fetches entirely. Use `INCLUDE` to add columns the query selects but doesn't filter on.

```sql
-- Query needs: id, amount, status (filter on status + created_at)
CREATE INDEX CONCURRENTLY ix_escrow_tx_covering
ON escrow_transactions (status, created_at DESC)
INCLUDE (id, amount);
```

**When to use INCLUDE vs. adding to key columns:**
- `INCLUDE` columns are stored in leaf pages only — not used for tree navigation
- Use `INCLUDE` for columns in SELECT but not in WHERE/ORDER BY
- Keeps the index narrower and more cache-friendly than adding all columns to the key

**Trade-off:** Wider indexes consume more disk and slow down writes. Only add INCLUDE columns for proven hot-path queries.

## Partial Indexes

Filter the index to include only the rows you actually query. Dramatically reduces index size.

```sql
-- Only 5% of transactions are pending, but 90% of queries filter for them
CREATE INDEX CONCURRENTLY ix_escrow_tx_pending
ON escrow_transactions (created_at DESC)
WHERE status = 'pending';

-- Soft-delete pattern: only index active records
CREATE INDEX CONCURRENTLY ix_users_active_email
ON users (email)
WHERE is_deleted = false;
```

**Benefits:**
- Index is a fraction of the full table size
- Faster to scan, update, and vacuum
- Less bloat from write-heavy columns

**Requirement:** The query's WHERE clause must match or be a superset of the partial index predicate for PostgreSQL to use it.

## Expression Indexes

For queries that filter on computed values or function results.

```sql
-- Query uses LOWER() for case-insensitive search
CREATE INDEX CONCURRENTLY ix_users_lower_email
ON users (LOWER(email));

-- Query extracts year from a timestamp
-- ❌ Don't do this — rewrite the query to use a range predicate instead
CREATE INDEX ix_bad ON escrow_transactions (EXTRACT(YEAR FROM created_at));

-- ✅ Better: use a range predicate in the query and a plain B-tree index on created_at
```

**Rule of thumb:** Prefer rewriting the query to be SARGable over creating expression indexes. Expression indexes are a last resort when you can't change the query.

## Multi-Column vs. Single-Column Decision Matrix

| Scenario | Strategy |
|---|---|
| Single equality predicate (`WHERE status = 'x'`) | Single-column index |
| Equality + range on different columns | Composite: equality first, range second |
| Two equality columns always queried together | Composite index on both |
| Two columns queried independently | Two single-column indexes (let bitmap AND combine) |
| High-cardinality column + low-cardinality column | High-cardinality column first in composite |
| JOIN column | Single-column index on the FK column |
| ORDER BY multiple columns | Composite index matching the sort order and direction |

## PostgreSQL Index Types

| Type | Use Case | Example |
|---|---|---|
| **B-tree** (default) | Equality, range, sorting, LIKE 'prefix%' | Most columns |
| **Hash** | Equality only, large values | Long text equality (rarely needed since PG 10+) |
| **GIN** | JSONB containment, array overlap, full-text search | `jsonb_path_ops`, `tsvector` columns |
| **GiST** | Range types, geometric, nearest-neighbor | `tsrange`, `inet`, PostGIS |
| **BRIN** | Physically ordered data (timestamps on append-only tables) | `created_at` on large, insert-only audit logs |

```sql
-- GIN for JSONB queries on escrow metadata
CREATE INDEX CONCURRENTLY ix_escrow_tx_metadata
ON escrow_transactions USING gin (metadata jsonb_path_ops);

-- BRIN for append-only audit log (very small index, effective on sorted data)
CREATE INDEX CONCURRENTLY ix_audit_log_created
ON audit_logs USING brin (created_at);
```

## Anti-Patterns

### Redundant Indexes

```sql
-- ix_a covers all queries that ix_b would serve
CREATE INDEX ix_a ON escrow_transactions (status, created_at);
CREATE INDEX ix_b ON escrow_transactions (status);  -- ❌ Redundant

-- Detection query:
SELECT indexrelid::regclass, indkey
FROM pg_index
WHERE indrelid = 'escrow_transactions'::regclass
ORDER BY indkey;
```

### Over-Indexing Write-Heavy Tables

Every index adds overhead to INSERT, UPDATE, and DELETE operations. For the escrow platform:

| Table | Read/Write Ratio | Index Strategy |
|---|---|---|
| `escrow_transactions` | Read-heavy (dashboards, reports) | More indexes acceptable |
| `audit_logs` | Write-heavy (every action logged) | Minimal indexes; prefer BRIN on created_at |
| `notifications` | Write-heavy, read-once | Single index on (user_id, is_read) at most |

**Rule:** If a table has >5 indexes, audit each one. Check `pg_stat_user_indexes` for unused indexes:

```sql
SELECT schemaname, relname, indexrelname, idx_scan
FROM pg_stat_user_indexes
WHERE idx_scan = 0 AND schemaname = 'public'
ORDER BY pg_relation_size(indexrelid) DESC;
```

### Missing FK Indexes

PostgreSQL does NOT automatically create indexes on foreign key columns. Always add them:

```sql
-- FK: escrow_transactions.buyer_id → users.id
CREATE INDEX CONCURRENTLY ix_escrow_tx_buyer_id
ON escrow_transactions (buyer_id);
```

## Index Maintenance

```sql
-- Check index bloat (estimate)
SELECT relname, pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
       idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 20;

-- Rebuild a bloated index (non-blocking)
REINDEX INDEX CONCURRENTLY ix_escrow_tx_status_created;
```

> **Always use `CONCURRENTLY`** for CREATE INDEX and REINDEX in production to avoid locking the table.
