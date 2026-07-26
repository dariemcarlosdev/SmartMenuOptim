# Index Design Reference

PostgreSQL index strategies and EF Core configuration for the NexTruzt.io escrow platform.

## PostgreSQL Index Types

| Type | Best For | Example Use Case |
|---|---|---|
| **B-tree** (default) | Equality, range, sorting, `LIKE 'prefix%'` | Most columns: `status`, `created_at`, FKs |
| **Hash** | Equality-only lookups | Exact match on long text (rare — B-tree usually better) |
| **GIN** | JSONB fields, full-text search, arrays | `payload jsonb`, `tsvector`, `tags text[]` |
| **GiST** | Geometric, range types, nearest-neighbor | `daterange`, `tstzrange`, PostGIS spatial |
| **BRIN** | Large tables with naturally ordered data | Append-only `created_at` on billions of rows |

### When to Use Each

```
Filtering by status, date range, FK? → B-tree
Searching inside JSONB? → GIN
Full-text search? → GIN on tsvector
Range overlap queries (date ranges)? → GiST
Huge append-only table, ordered column? → BRIN
Everything else? → B-tree
```

## Composite Index Column Order

**Selectivity-first rule**: Place the most selective column (most distinct values) leftmost.

```sql
-- Query: WHERE status = 'active' AND buyer_id = '{uuid}'
-- buyer_id is more selective (many distinct values) than status (few values)

-- ✅ Correct: high selectivity first
CREATE INDEX ix_escrow_transaction_buyer_status
    ON escrow_transaction (buyer_id, status);

-- ❌ Wrong: low selectivity first — index scan reads too many rows
CREATE INDEX ix_escrow_transaction_status_buyer
    ON escrow_transaction (status, buyer_id);
```

**Leftmost-prefix rule**: A composite index on `(a, b, c)` supports:
- Queries filtering on `a`
- Queries filtering on `a, b`
- Queries filtering on `a, b, c`
- But NOT queries filtering only on `b` or `c`

## Partial Indexes

Filter the index to include only relevant rows — smaller index, faster scans:

```sql
-- Soft-delete pattern: only index active rows
CREATE INDEX ix_escrow_transaction_status_active
    ON escrow_transaction (status)
    WHERE is_deleted = false;

-- Only index pending transactions (hot data)
CREATE INDEX ix_escrow_transaction_pending
    ON escrow_transaction (created_at)
    WHERE status = 'pending';

-- EF Core configuration
builder.HasIndex(e => e.Status)
    .HasDatabaseName("ix_escrow_transaction_status_active")
    .HasFilter("is_deleted = false");
```

## Covering Indexes with INCLUDE

Add non-key columns to avoid table lookups (index-only scans):

```sql
-- Query: SELECT id, amount, status FROM escrow_transaction WHERE buyer_id = ?
-- Without INCLUDE: index finds rows → heap fetch for amount, status
-- With INCLUDE: index has all columns → index-only scan

CREATE INDEX ix_escrow_transaction_buyer_covering
    ON escrow_transaction (buyer_id)
    INCLUDE (amount, status, created_at);
```

```csharp
// EF Core (PostgreSQL provider)
builder.HasIndex(e => e.BuyerId)
    .HasDatabaseName("ix_escrow_transaction_buyer_covering")
    .IncludeProperties(e => new { e.Amount, e.Status, e.CreatedAt });
```

> **Trade-off**: Covering indexes are larger and slower to maintain on writes. Use only for high-frequency read queries.

## Unique Indexes for Business Rules

Enforce uniqueness at the database level — never trust application code alone:

```sql
-- One active escrow per buyer-seller pair
CREATE UNIQUE INDEX uq_escrow_transaction_active_pair
    ON escrow_transaction (buyer_id, seller_id)
    WHERE status IN ('pending', 'active') AND is_deleted = false;
```

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|---|---|---|
| **Missing FK indexes** | Slow joins and cascade deletes; full table scans | Add B-tree index on every FK column |
| **Over-indexing** | Slow writes; wasted storage; maintenance overhead | Audit: drop indexes with < 10 scans/month |
| **Low-cardinality index** | Index on `boolean` or `status` with 3 values scans most rows | Use partial index or remove |
| **Duplicate indexes** | `ix_a` on `(buyer_id)` + `ix_b` on `(buyer_id, status)` — `ix_a` is redundant | Drop the prefix-duplicate |
| **Indexing every column** | "Just in case" indexes hurt write performance | Index based on actual query patterns |
| **Wrong column order** | Composite `(status, buyer_id)` when queries filter by `buyer_id` first | Reorder: selectivity-first |
| **Non-concurrent index creation** | `CREATE INDEX` blocks writes on production tables | Use `CREATE INDEX CONCURRENTLY` |
| **Unused indexes** | Indexes that are never scanned waste space and slow writes | Query `pg_stat_user_indexes` to find |

## Detecting Index Issues

```sql
-- Find unused indexes (low scan count)
SELECT schemaname, relname AS table_name, indexrelname AS index_name,
       idx_scan, idx_tup_read, pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE idx_scan < 10
ORDER BY pg_relation_size(indexrelid) DESC;

-- Find missing indexes (sequential scans on large tables)
SELECT relname AS table_name, seq_scan, seq_tup_read,
       idx_scan, pg_size_pretty(pg_relation_size(relid)) AS table_size
FROM pg_stat_user_tables
WHERE seq_scan > 1000 AND pg_relation_size(relid) > 10485760  -- > 10 MB
ORDER BY seq_scan DESC;

-- Find duplicate indexes
SELECT indrelid::regclass AS table_name,
       array_agg(indexrelid::regclass) AS duplicate_indexes
FROM pg_index
GROUP BY indrelid, indkey
HAVING COUNT(*) > 1;
```

## EF Core Index Configuration

```csharp
public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        // Simple B-tree index
        builder.HasIndex(e => e.BuyerId)
            .HasDatabaseName("ix_escrow_transaction_buyer_id");

        // Composite index (selectivity-first)
        builder.HasIndex(e => new { e.BuyerId, e.Status })
            .HasDatabaseName("ix_escrow_transaction_buyer_status");

        // Partial index (soft deletes)
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_escrow_transaction_status_active")
            .HasFilter("is_deleted = false");

        // Covering index
        builder.HasIndex(e => e.BuyerId)
            .HasDatabaseName("ix_escrow_transaction_buyer_covering")
            .IncludeProperties(e => new { e.Amount, e.Status, e.CreatedAt });

        // Unique business rule
        builder.HasIndex(e => e.TransactionReference)
            .IsUnique()
            .HasDatabaseName("uq_escrow_transaction_transaction_reference");
    }
}
```

## Index Maintenance

```sql
-- Check index bloat (estimated)
SELECT nspname, relname,
       round(100 * pg_relation_size(indexrelid) / pg_relation_size(indrelid)) AS index_ratio_pct,
       pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_index
JOIN pg_class ON pg_class.oid = pg_index.indexrelid
JOIN pg_namespace ON pg_namespace.oid = pg_class.relnamespace
WHERE pg_relation_size(indrelid) > 0
ORDER BY pg_relation_size(indexrelid) DESC
LIMIT 20;

-- Rebuild bloated indexes (non-blocking)
REINDEX INDEX CONCURRENTLY ix_escrow_transaction_status_active;

-- Rebuild all indexes on a table (non-blocking, PG 14+)
REINDEX TABLE CONCURRENTLY escrow_transaction;
```

## Index Design Checklist

- [ ] Every FK column has a B-tree index
- [ ] Composite indexes follow selectivity-first column order
- [ ] Soft-delete tables use partial indexes (`WHERE is_deleted = false`)
- [ ] High-frequency read queries have covering indexes
- [ ] No duplicate/overlapping indexes
- [ ] No indexes on low-cardinality columns without partial filter
- [ ] Indexes created with `CONCURRENTLY` in migrations
- [ ] JSONB columns queried by path have GIN indexes
- [ ] Business uniqueness enforced via unique indexes, not app code
- [ ] Unused indexes identified and scheduled for removal
