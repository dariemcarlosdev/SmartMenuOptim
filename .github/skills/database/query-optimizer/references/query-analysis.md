# Query Analysis — PostgreSQL EXPLAIN ANALYZE

Reference guide for reading and interpreting PostgreSQL query execution plans in the NexTruzt escrow platform.

## Running EXPLAIN ANALYZE

### From psql or pgAdmin

```sql
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) 
SELECT id, amount, status, created_at 
FROM escrow_transactions 
WHERE status = 'pending' AND created_at > '2024-01-01'
ORDER BY created_at DESC 
LIMIT 50;
```

**Key flags:**
- `ANALYZE` — actually executes the query (use with caution on writes)
- `BUFFERS` — shows shared/local buffer hits and reads (I/O detail)
- `FORMAT TEXT` — human-readable output (use `JSON` for programmatic parsing)

### From EF Core (.NET)

```csharp
// Log the generated SQL, then run EXPLAIN manually
var sql = _context.EscrowTransactions
    .Where(t => t.Status == "pending" && t.CreatedAt > cutoff)
    .OrderByDescending(t => t.CreatedAt)
    .Take(50)
    .ToQueryString();

// Or use Npgsql's built-in EXPLAIN support
await using var cmd = _context.Database.GetDbConnection().CreateCommand();
cmd.CommandText = $"EXPLAIN (ANALYZE, BUFFERS) {sql}";
await _context.Database.OpenConnectionAsync(ct);
await using var reader = await cmd.ExecuteReaderAsync(ct);
while (await reader.ReadAsync(ct))
    Console.WriteLine(reader.GetString(0));
```

## Key Metrics in EXPLAIN Output

| Metric | What It Means | Red Flag |
|---|---|---|
| **actual time** | Wall-clock time in ms (startup..total) | Total > 100ms for OLTP queries |
| **rows** | Actual rows produced by node | Huge mismatch vs. `rows` estimate = stale statistics |
| **loops** | Times this node was executed | loops > 1 on expensive nodes = potential N+1 at DB level |
| **Buffers: shared hit** | Pages read from PostgreSQL cache | Low ratio to shared read = cold cache or working set > memory |
| **Buffers: shared read** | Pages read from disk | High reads = missing index or insufficient shared_buffers |
| **Sort Method: external merge** | Sort spilled to disk | Increase `work_mem` or reduce result set size |

## Common Plan Node Types

### Scan Nodes (leaf nodes — read data)

| Node | Description | When It's a Problem |
|---|---|---|
| **Seq Scan** | Full table scan, reads every row | On tables > 10K rows when a predicate is selective |
| **Index Scan** | B-tree lookup + heap fetch | Usually good; watch for high `rows` if selectivity is low |
| **Index Only Scan** | Reads entirely from the index | Best case — means you have a covering index |
| **Bitmap Index Scan** | Builds a bitmap of matching TIDs | OK for medium-selectivity; watch for `lossy` recheck |
| **Bitmap Heap Scan** | Fetches heap pages from bitmap | Follows Bitmap Index Scan; `Recheck Cond` means pages were lossy |

### Join Nodes

| Node | Best For | Watch Out |
|---|---|---|
| **Nested Loop** | Small outer set, indexed inner | Disastrous if outer set is large — O(N×M) |
| **Hash Join** | Large unsorted sets, equality joins | Hash spills to disk if `work_mem` too low |
| **Merge Join** | Pre-sorted input, large equi-joins | Requires sorted input; Sort node adds overhead if not pre-sorted |

### Other Important Nodes

| Node | Description |
|---|---|
| **Sort** | Explicit sort (ORDER BY). Check for `external merge Disk` = spill. |
| **Aggregate** | GROUP BY or aggregate functions. Watch for HashAggregate overflow. |
| **Limit** | Stops reading after N rows. Efficient only if underlying plan supports early termination. |
| **Materialize** | Caches sub-plan results in memory. Triggered on repeated reads of a subquery. |

## Cost Estimation Basics

```
Seq Scan on escrow_transactions  (cost=0.00..1523.00 rows=50000 width=64)
                                  ^^^^^    ^^^^^^^
                                  startup  total cost
```

- **cost** is in arbitrary units (sequential page reads). Not milliseconds.
- **startup cost** — time before the first row is returned (sorting, hashing)
- **total cost** — estimated time to return all rows
- **rows** — planner's estimate of rows returned. Compare to `actual rows`.
- **width** — average row size in bytes

> **Rule of thumb:** If `actual rows` is >10× different from estimated `rows`, run `ANALYZE` on the table to update statistics.

## Red Flags Checklist

| Red Flag | What to Do |
|---|---|
| Seq Scan on a table with >10K rows | Add an index on the filtered/joined column |
| `loops=1000` on a Nested Loop inner | Rewrite as a Hash Join or add an index to eliminate the loop |
| `actual rows` ≫ `estimated rows` | Run `ANALYZE tablename;` to refresh statistics |
| `Sort Method: external merge Disk` | Increase `work_mem` for the session or reduce the sort set |
| `Buffers: shared read` ≫ `shared hit` | Working set exceeds `shared_buffers`; consider increasing it |
| `Filter: (removes 95% of rows)` after Seq Scan | The filter belongs in an index WHERE clause (partial index) |
| `Recheck Cond` with `lossy=true` | `work_mem` too low for bitmap; increase it or use a more selective index |

## Example: Reading a Plan

```
Sort  (cost=2145.30..2145.55 rows=100 width=48) (actual time=12.456..12.501 rows=100 loops=1)
  Sort Key: created_at DESC
  Sort Method: top-N heapsort  Memory: 32kB
  ->  Seq Scan on escrow_transactions  (cost=0.00..2142.00 rows=5000 width=48) (actual time=0.021..11.234 rows=5000 loops=1)
        Filter: (status = 'pending'::text)
        Rows Removed by Filter: 45000
        Buffers: shared hit=892
Planning Time: 0.185 ms
Execution Time: 12.589 ms
```

**Analysis:**
1. **Seq Scan** on 50K rows, filtering down to 5K — index on `(status, created_at DESC)` would eliminate the scan
2. **Sort** uses top-N heapsort (efficient for LIMIT), but the underlying scan is wasteful
3. **Buffers: shared hit=892** — all from cache, but that's 892 pages read unnecessarily

**Fix:** Create a composite index:
```sql
CREATE INDEX CONCURRENTLY ix_escrow_transactions_status_created 
ON escrow_transactions (status, created_at DESC);
```

**Expected result:** Seq Scan → Index Scan, reading ~5-10 pages instead of 892.
