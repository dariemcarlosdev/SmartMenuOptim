# Migration Safety Reference

Safe migration patterns for PostgreSQL with EF Core on the NexTruzt.io escrow platform. All migrations must support zero-downtime deployment.

## Safe vs Unsafe Operations

| Operation | Safety | Notes |
|---|---|---|
| Add nullable column | ✅ Safe | No lock escalation, no data rewrite |
| Add column with `DEFAULT` (PG 11+) | ✅ Safe | PostgreSQL stores default in catalog, no table rewrite |
| Add index `CONCURRENTLY` | ✅ Safe | Non-blocking; requires outside transaction |
| Create new table | ✅ Safe | No impact on existing queries |
| Add check constraint `NOT VALID` | ✅ Safe | Validates new rows only; validate later |
| Drop unused index | ✅ Safe | Brief lock, fast operation |
| Add non-nullable column (no default) | 🚫 Unsafe | Fails on existing rows; requires backfill pattern |
| Drop column | ⚠️ Caution | Ensure no code references; may need phased rollout |
| Rename column | ⚠️ Caution | Breaks all existing queries referencing old name |
| Rename table | ⚠️ Caution | Breaks all existing queries; use view as alias |
| Change column type | 🚫 Unsafe | Full table rewrite; `ACCESS EXCLUSIVE` lock |
| Add index (non-concurrent) | ⚠️ Caution | Blocks writes for duration of build |
| Drop table | 🚫 Unsafe | Data loss; ensure no FK references remain |
| Add NOT NULL to existing column | 🚫 Unsafe | Full table scan; use `NOT VALID` + `VALIDATE` pattern |

## Zero-Downtime Patterns

### Column Rename (3-phase)

Never rename directly — deploy in phases:

```
Phase 1: Add new column → copy data → deploy app reading both
Phase 2: Update app to write new column → stop writing old
Phase 3: Drop old column (next release)
```

```csharp
// Phase 1 migration
public partial class RenameTransactionRefToReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new column
        migrationBuilder.AddColumn<string>(
            name: "transaction_reference",
            table: "escrow_transaction",
            type: "varchar(50)",
            nullable: true);

        // Copy data
        migrationBuilder.Sql(
            "UPDATE escrow_transaction SET transaction_reference = txn_ref");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "transaction_reference",
            table: "escrow_transaction");
    }
}

// Phase 3 migration (next release)
public partial class DropOldTxnRef : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "txn_ref",
            table: "escrow_transaction");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "txn_ref",
            table: "escrow_transaction",
            type: "varchar(50)",
            nullable: true);
    }
}
```

### Adding Non-Nullable Column Safely

```csharp
public partial class AddCurrencyCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Step 1: Add as nullable
        migrationBuilder.AddColumn<string>(
            name: "currency_code",
            table: "escrow_transaction",
            type: "varchar(3)",
            nullable: true);

        // Step 2: Backfill existing rows
        migrationBuilder.Sql(
            "UPDATE escrow_transaction SET currency_code = 'USD' WHERE currency_code IS NULL");

        // Step 3: Set NOT NULL
        migrationBuilder.AlterColumn<string>(
            name: "currency_code",
            table: "escrow_transaction",
            type: "varchar(3)",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "currency_code",
            table: "escrow_transaction");
    }
}
```

### Concurrent Index Creation

EF Core doesn't natively support `CONCURRENTLY` — use raw SQL:

```csharp
public partial class AddIndexOnStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CONCURRENTLY cannot run inside a transaction
        migrationBuilder.Sql(
            "CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_escrow_transaction_status " +
            "ON escrow_transaction (status) WHERE is_deleted = false",
            suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_escrow_transaction_status",
            suppressTransaction: true);
    }
}
```

### Adding CHECK Constraint Safely

```sql
-- Step 1: Add NOT VALID (instant, no table scan)
ALTER TABLE escrow_transaction
    ADD CONSTRAINT ck_escrow_transaction_amount_positive
    CHECK (amount > 0) NOT VALID;

-- Step 2: Validate in background (ShareUpdateExclusiveLock, doesn't block writes)
ALTER TABLE escrow_transaction
    VALIDATE CONSTRAINT ck_escrow_transaction_amount_positive;
```

## EF Core Migration Best Practices

1. **Idempotent migrations** — use `IF NOT EXISTS` / `IF EXISTS` in raw SQL
2. **Always implement `Down()`** — rollback must be possible for every migration
3. **One concern per migration** — don't mix schema changes with data changes
4. **Name migrations descriptively** — `AddCurrencyCodeToEscrowTransaction`, not `Migration_20240115`
5. **Review generated SQL** — run `dotnet ef migrations script` before applying
6. **Test on a copy** — apply migrations against a staging database first
7. **Never edit applied migrations** — create a new migration to fix issues

```bash
# Review generated SQL before applying
dotnet ef migrations script --idempotent -o review.sql

# Apply with verbose logging
dotnet ef database update --verbose
```

## Rollback Strategies

| Strategy | When | How |
|---|---|---|
| **EF Core `Down()` method** | Single migration rollback | `dotnet ef database update {PreviousMigration}` |
| **Point-in-time restore** | Catastrophic failure | Restore from backup to timestamp before migration |
| **Forward-fix migration** | `Down()` is too complex | Create new migration that fixes the issue |
| **Feature flag + phased rollout** | High-risk schema change | Deploy behind flag; roll back by disabling flag |

## Migration Safety Checklist

- [ ] Migration supports zero-downtime deployment
- [ ] No `ACCESS EXCLUSIVE` locks on high-traffic tables
- [ ] Indexes created with `CONCURRENTLY` where possible
- [ ] Non-nullable columns added via nullable → backfill → alter pattern
- [ ] `Down()` method implemented and tested
- [ ] Raw SQL uses `IF NOT EXISTS` / `IF EXISTS` for idempotency
- [ ] Generated SQL reviewed (`dotnet ef migrations script`)
- [ ] CHECK constraints added with `NOT VALID` + `VALIDATE` pattern
- [ ] No column renames — use add/copy/drop pattern
- [ ] Data backfill handles large tables in batches (avoid long transactions)
