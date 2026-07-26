# Naming Conventions Reference

PostgreSQL and EF Core naming conventions for the NexTruzt.io escrow platform.

## PostgreSQL Naming Rules

| Element | Convention | Example |
|---|---|---|
| Tables | `snake_case`, singular | `escrow_transaction` |
| Columns | `snake_case` | `created_at`, `fee_amount` |
| Primary keys | `id` | `id` (per table) |
| Foreign keys (column) | `{referenced_table}_id` | `buyer_id`, `escrow_transaction_id` |
| FK constraints | `fk_{child}_{parent}_{column}` | `fk_escrow_transaction_buyer_buyer_id` |
| Indexes | `ix_{table}_{columns}` | `ix_escrow_transaction_status` |
| Unique constraints | `uq_{table}_{columns}` | `uq_user_email` |
| Check constraints | `ck_{table}_{rule}` | `ck_escrow_transaction_amount_positive` |
| Sequences | `sq_{table}_{column}` | `sq_ledger_entry_id` |

## Key Rules

1. **Always `snake_case`** — PostgreSQL folds unquoted identifiers to lowercase; `snake_case` avoids quoting issues
2. **Singular table names** — `escrow_transaction`, not `escrow_transactions` (entity represents one row)
3. **No abbreviations** — `transaction_reference` not `txn_ref`; exception: well-known acronyms (`id`, `url`, `ip`)
4. **No reserved words** — avoid `user`, `order`, `group`, `table` as bare names; use `app_user`, `escrow_order`
5. **Boolean columns** — prefix with `is_` or `has_`: `is_deleted`, `is_active`, `has_2fa_enabled`
6. **Timestamp columns** — suffix with `_at`: `created_at`, `updated_at`, `completed_at`

## EF Core Entity-to-Table Mapping

Map PascalCase C# entities to snake_case PostgreSQL using explicit configuration:

```csharp
public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        // Table
        builder.ToTable("escrow_transaction");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Columns — explicit snake_case mapping
        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)");

        builder.Property(e => e.BuyerId)
            .HasColumnName("buyer_id");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        // Foreign key constraint naming
        builder.HasOne(e => e.Buyer)
            .WithMany(b => b.EscrowTransactions)
            .HasForeignKey(e => e.BuyerId)
            .HasConstraintName("fk_escrow_transaction_user_buyer_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Index naming
        builder.HasIndex(e => e.BuyerId)
            .HasDatabaseName("ix_escrow_transaction_buyer_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_escrow_transaction_status")
            .HasFilter("is_deleted = false");

        // Unique constraint naming
        builder.HasIndex(e => e.TransactionReference)
            .IsUnique()
            .HasDatabaseName("uq_escrow_transaction_transaction_reference");

        // Concurrency
        builder.UseXminAsConcurrencyToken();
    }
}
```

> **Tip**: Use the `EFCore.NamingConventions` NuGet package to auto-convert to `snake_case` globally, then override only where needed.

```csharp
// In DbContext OnConfiguring or Startup
options.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention();
```

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|---|---|---|
| `PascalCase` table/column names | Requires quoting in all raw SQL; inconsistent with PostgreSQL ecosystem | Use `snake_case` |
| Mixed casing (`userId`, `user_Id`) | Confusing; breaks conventions | Pick `snake_case` and enforce |
| Abbreviations (`txn`, `amt`, `desc`) | Ambiguous; hard to discover | Spell out: `transaction`, `amount`, `description` |
| Reserved words (`user`, `order`) | Requires quoting; error-prone in queries | Prefix: `app_user`, `escrow_order` |
| Plural table names (`transactions`) | Inconsistent when joining; entity-row mismatch | Singular: `transaction` |
| Inconsistent PK naming (`Id` vs `TransactionId`) | Confusion in joins and FK references | Use `id` in every table |
| No constraint names (EF auto-generated) | Unreadable migration diffs; hard to reference in scripts | Always set explicit names |
| `tbl_` or `sp_` prefixes | Redundant; adds noise | Drop prefixes entirely |

## Naming Checklist

- [ ] All tables use `snake_case` singular
- [ ] All columns use `snake_case`
- [ ] Boolean columns start with `is_` or `has_`
- [ ] Timestamp columns end with `_at`
- [ ] FK columns follow `{referenced_table}_id`
- [ ] All constraints/indexes have explicit names matching conventions
- [ ] No reserved words used as bare identifiers
- [ ] No abbreviations except well-known acronyms
