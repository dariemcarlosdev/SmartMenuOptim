---
name: schema-reviewer
description: "Review database schema design for normalization, indexing, naming, and constraints. Triggers: schema review, database design, table design, normalization check"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: database
  triggers: schema review, database design, normalization check, index review, table design
  role: database-architect
  scope: review
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: query-optimizer, migration-safety
---

# Schema Reviewer

Review database schema design for normalization, indexing strategy, naming consistency, constraint completeness, and data type correctness — targeting PostgreSQL with EF Core on the NexTruzt.io fintech escrow platform.

## When to Use

- New EF Core entity models or `IEntityTypeConfiguration<T>` files need structural review
- Database migrations are being added and need safety/correctness validation
- Performance issues suggest schema-level problems (missing indexes, poor data types)
- Pre-release gate for schema changes touching financial data
- Tech-debt reduction targeting the data layer
- CQRS read-model schema needs denormalization review

## Core Workflow

### 1. Discover Schema Sources

Scan the codebase for schema definitions:

```bash
# Find EF Core configurations and entities
find . -name "*Configuration.cs" -o -name "*DbContext.cs" -o -name "*.cs" | xargs grep -l "IEntityTypeConfiguration\|DbSet<\|modelBuilder"
# Find migrations
find . -path "*/Migrations/*.cs" | head -20
```

Produce a table inventory: table name, columns, types, nullability, keys, indexes.

✅ **Checkpoint**: Table inventory complete — every table accounted for.

### 2. Validate Naming Conventions

Load **[Naming Conventions](references/naming-conventions.md)** and check:
- Tables: `snake_case`, singular (`escrow_transaction`, not `EscrowTransactions`)
- Columns: `snake_case` (`created_at`, not `CreatedAt`)
- PKs: `id` per table, FKs: `fk_{child}_{parent}_{column}`
- Indexes: `ix_{table}_{columns}`, constraints: `ck_`/`uq_` prefixes

### 3. Assess Normalization (1NF → 3NF)

Load **[Normalization](references/normalization.md)** and validate each table:
- **1NF**: No CSV-in-column, no repeating groups, atomic values, PK present
- **2NF**: No partial dependencies on composite keys
- **3NF**: No transitive dependencies between non-key columns

Flag intentional denormalization (CQRS read models, materialized views) — verify it is documented.

✅ **Checkpoint**: All tables assessed against 3NF — violations documented with severity.

### 4. Review Index Strategy

Load **[Index Design](references/index-design.md)** and evaluate:
- FK columns without indexes → 🔴 Critical
- Missing composite indexes for frequent query patterns
- Over-indexing (more indexes than columns, low-cardinality indexes)
- Partial indexes for soft-delete patterns (`WHERE is_deleted = false`)
- Covering indexes with `INCLUDE` for high-frequency queries

### 5. Check Constraints and Referential Integrity

- FK constraints present for every relationship with explicit `ON DELETE` behavior
- `CHECK` constraints for bounded values (amounts > 0, valid status enums)
- `NOT NULL` enforced where business rules require a value
- `DEFAULT` values for timestamps, status fields, boolean flags
- Concurrency tokens (`xmin` / row version) on tables with concurrent writes

✅ **Checkpoint**: All constraints validated — no implicit cascade surprises.

### 6. Assess Data Types (Fintech Focus)

- Money: `numeric(19,4)` — never `float`/`double`/`real`
- Dates: `timestamptz` for all timestamps — never `timestamp` without timezone
- UUIDs: `uuid` type with `gen_random_uuid()` default — sequential strategy for clustered PK
- Enums: PostgreSQL `CREATE TYPE` or `int` with check constraint — never magic strings
- Text: Bounded `varchar(n)` — no unbounded `text` without justification

### 7. Validate Migration Safety

Load **[Migration Safety](references/migration-safety.md)** for any pending migrations:
- Zero-downtime compatibility check
- Backward-compatible column changes
- Rollback strategy present

✅ **Checkpoint**: All migrations reviewed — safe for zero-downtime deployment.

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [Naming Conventions](references/naming-conventions.md) | Table/column/index naming | PostgreSQL conventions, EF Core mapping, consistency rules |
| [Normalization](references/normalization.md) | Normal forms, denormalization decisions | 1NF→3NF checks, justified denormalization, read models |
| [Migration Safety](references/migration-safety.md) | Safe migration patterns (EF Core) | Zero-downtime migrations, backward compat, rollback |
| [Index Design](references/index-design.md) | Index strategies, anti-patterns | B-tree, GIN, partial indexes, covering indexes |

## Quick Reference

### Financial Column Pattern (PostgreSQL/EF Core)

```csharp
// Entity
public sealed class EscrowTransaction
{
    public Guid Id { get; init; }
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public uint RowVersion { get; private set; } // xmin concurrency
}

// Configuration
public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.ToTable("escrow_transaction");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(19,4)");
        builder.Property(e => e.FeeAmount).HasColumnName("fee_amount").HasColumnType("numeric(19,4)");
        builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.UseXminAsConcurrencyToken();
    }
}
```

### Partial Index for Soft Deletes

```sql
CREATE INDEX ix_escrow_transaction_active_status
    ON escrow_transaction (status)
    WHERE is_deleted = false;
```

### Audit Column Check Query

```sql
SELECT table_name
FROM information_schema.tables t
WHERE t.table_schema = 'public'
  AND NOT EXISTS (
    SELECT 1 FROM information_schema.columns c
    WHERE c.table_name = t.table_name
      AND c.column_name IN ('created_at', 'updated_at')
  );
```

## Constraints

### MUST DO

- Review every table — do not skip any entity
- Assess normalization to at least 3NF; document justified denormalization
- Flag `float`/`double` on monetary columns as 🔴 Critical
- Check FK columns have indexes; check composite index column order
- Provide severity (🔴 Critical, 🟡 Warning, 🔵 Info) for every finding
- Validate migration safety for zero-downtime deployment

### MUST NOT

- Recommend denormalization without read-performance justification
- Suggest indexes without noting write-performance trade-offs
- Skip concurrency token check on tables with concurrent writes
- Report naming style preferences as critical findings
- Assume ORM — work with raw DDL, EF Core configs, or Dapper equally
- Ignore PostgreSQL-specific behaviors (partial indexes, `xmin`, `JSONB`)

## Output Template

**Project**: `{project-name}` | **Engine**: PostgreSQL | **Tables**: {count} | **Date**: {date}

| Severity | Count |
|---|---|
| 🔴 Critical | {n} |
| 🟡 Warning | {n} |
| 🔵 Info | {n} |

| # | Issue | Table.Column | Severity | Category | Fix |
|---|---|---|---|---|---|
| 1 | {description} | {table.column} | 🔴 | {Normalization/Indexing/Constraint/Naming/DataType} | {actionable fix} |

**Normalization**: 1NF {✅/❌} | 2NF {✅/❌} | 3NF {✅/❌}

**Priority Actions**:
1. 🔴 {action} — {reason}
2. 🟡 {action} — {reason}
3. 🔵 {action} — {reason}
