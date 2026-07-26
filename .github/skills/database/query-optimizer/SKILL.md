---
name: query-optimizer
description: "Optimize database queries by detecting anti-patterns in EF Core LINQ, raw SQL, and Dapper. Triggers: slow query, N+1, query optimization, execution plan"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: database
  triggers: slow query, N+1, query optimization, performance, AsNoTracking, execution plan
  role: performance-engineer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: schema-reviewer, index-design
---

# Query Optimizer — NexTruzt Escrow Platform

Detect anti-patterns and optimize EF Core LINQ, raw SQL, and Dapper queries for the NexTruzt.io PostgreSQL-backed escrow platform.

## When to Use

- A MediatR handler or repository method produces slow queries (>100ms for OLTP)
- N+1 patterns suspected in navigation property access across escrow transactions
- `EXPLAIN ANALYZE` reveals sequential scans on large tables (escrow_transactions, audit_logs)
- EF Core change tracker overhead is high on read-heavy dashboards
- Preparing for load testing or post-incident performance review
- Code review flags missing `AsNoTracking()`, unbounded queries, or `SELECT *`

## Core Workflow

1. **Identify** — Locate the slow query from EF Core LINQ, raw SQL, Dapper, or `pg_stat_statements` output. Capture the full calling context: method signature, parameters, Include chains, Where/OrderBy clauses, expected result size, and execution frequency.

2. **Analyze** — Run or interpret `EXPLAIN (ANALYZE, BUFFERS)` output. Look for sequential scans on large tables, high loop counts in nested loops, sort/hash spills to disk, and implicit type conversions.
   ✅ *Checkpoint: Can you identify the dominant cost node in the plan?*

3. **Detect Anti-Patterns** — Scan for known issues:
   - **EF Core**: N+1, missing AsNoTracking, full entity loads, client-side evaluation, cartesian explosions, premature materialization, unbounded result sets
   - **Raw SQL**: SELECT *, correlated subqueries, non-SARGable predicates, missing parameterization, functions on indexed columns
   ✅ *Checkpoint: Each finding has a named anti-pattern and file:line location*

4. **Optimize** — For each finding, provide before/after code with generated SQL diff and quantitative impact estimate. Load the appropriate reference for deep guidance:
   - Query plan issues → [Query Analysis](references/query-analysis.md)
   - Index recommendations → [Index Strategies](references/index-strategies.md)
   - PostgreSQL config → [PostgreSQL Tuning](references/postgresql-tuning.md)
   - EF Core patterns → [EF Core Optimization](references/ef-core-optimization.md)
   ✅ *Checkpoint: Every optimization has a trade-off documented*

5. **Validate** — Confirm optimized query produces identical results. Re-run EXPLAIN ANALYZE to verify improvement. Check that new indexes don't degrade write paths.
   ✅ *Checkpoint: Before/after plan comparison shows measurable improvement*

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [Query Analysis](references/query-analysis.md) | EXPLAIN ANALYZE, query plans | Plan reading, seq scans, index scans, cost estimation |
| [Index Strategies](references/index-strategies.md) | Covering, partial, composite indexes | Index selection, column order, PostgreSQL index types |
| [PostgreSQL Tuning](references/postgresql-tuning.md) | PG-specific optimization | work_mem, shared_buffers, connection pooling, vacuuming |
| [EF Core Optimization](references/ef-core-optimization.md) | EF Core query patterns, AsNoTracking | N+1 fixes, projections, split queries, compiled queries |

## Quick Reference

### N+1 → Eager Loading

```csharp
// ❌ Before: N+1 — each iteration triggers a lazy-load query
var transactions = await _context.EscrowTransactions.ToListAsync(ct);
foreach (var t in transactions)
    Console.WriteLine(t.Buyer.Name); // SELECT per iteration

// ✅ After: Single query with Include
var transactions = await _context.EscrowTransactions
    .Include(t => t.Buyer)
    .AsNoTracking()
    .ToListAsync(ct);
```

### Full Entity → Projection

```csharp
// ❌ Before: Loads all 30+ columns
var list = await _context.EscrowTransactions.ToListAsync(ct);

// ✅ After: Only the 4 columns the UI needs
var list = await _context.EscrowTransactions
    .AsNoTracking()
    .Select(t => new TransactionSummaryDto
    {
        Id = t.Id,
        Amount = t.Amount,
        Status = t.Status,
        CreatedAt = t.CreatedAt
    })
    .ToListAsync(ct);
```

### Non-SARGable → SARGable Predicate

```sql
-- ❌ Before: Function on column prevents index usage
SELECT * FROM escrow_transactions WHERE EXTRACT(YEAR FROM created_at) = 2024;

-- ✅ After: Range predicate enables index scan
SELECT id, amount, status, created_at FROM escrow_transactions
WHERE created_at >= '2024-01-01' AND created_at < '2025-01-01';
```

## Constraints

### MUST DO

- Name each anti-pattern explicitly (N+1, client evaluation, cartesian explosion, etc.)
- Provide compilable before/after C# or SQL — not pseudocode
- Show the generated SQL for EF Core changes
- Document trade-offs for every optimization
- Propagate `CancellationToken` on all async query paths
- Flag SQL injection risks in raw SQL and Dapper queries
- Consider write-path impact before recommending new indexes

### MUST NOT

- Suggest optimizations without explaining *why* they help
- Recommend raw SQL over EF Core unless LINQ translation is fundamentally limited
- Add indexes without considering the read/write ratio
- Apply micro-optimizations that add complexity for negligible gain
- Assume database engine — detect from DbContext configuration or ask

## Output Template

```markdown
## Query Optimization Report

**Project**: `NexTruzt.Escrow`
**Source**: {EF Core LINQ | Raw SQL | Dapper}
**Database**: PostgreSQL
**Files**: `{file-path(s)}`

### Findings

| # | Anti-Pattern | Location | Severity | Impact |
|---|---|---|---|---|
| 1 | {name} | `{file:line}` | 🔴 Critical | {e.g., "N queries → 1"} |
| 2 | {name} | `{file:line}` | 🟡 Warning | {e.g., "~30% memory reduction"} |

### Finding 1: {Anti-Pattern Name}

**Location**: `{file}:{line}` | **Severity**: 🔴 Critical

**Before**:
\```csharp
{original code}
\```

**After**:
\```csharp
{optimized code}
\```

**SQL Diff**: {key difference in generated SQL}
**Improvement**: {quantitative estimate}
**Trade-off**: {any downside}

### Index Recommendations

\```sql
CREATE INDEX CONCURRENTLY IX_{table}_{cols} ON {table} ({cols}) INCLUDE ({cols});
\```

### Priority Actions

1. 🔴 {action} — {impact}
2. 🟡 {action} — {impact}
3. 🔵 {action} — {impact}
```
