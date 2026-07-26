# Normalization Reference

Normal form assessment and denormalization guidance for the NexTruzt.io escrow platform.

## Normal Forms Quick Reference

| Normal Form | Rule | Check For | Violation Example |
|---|---|---|---|
| **1NF** | Atomic values, no repeating groups | CSV-in-column, arrays stored as text, missing PK | `tags = "escrow,payment,hold"` |
| **2NF** | No partial dependencies | Non-key column depends on *part* of a composite PK | `(order_id, product_id) → product_name` |
| **3NF** | No transitive dependencies | Non-key column depends on another non-key column | `buyer_id → buyer_name → buyer_email` |

## Common Fintech Violations

### 1NF — CSV-in-Column

```sql
-- ❌ Violation: multi-valued column
CREATE TABLE escrow_transaction (
    id uuid PRIMARY KEY,
    participant_ids text  -- "uuid1,uuid2,uuid3"
);

-- ✅ Fix: junction table
CREATE TABLE escrow_transaction (
    id uuid PRIMARY KEY
);

CREATE TABLE escrow_transaction_participant (
    escrow_transaction_id uuid REFERENCES escrow_transaction(id),
    user_id uuid REFERENCES app_user(id),
    role varchar(20) NOT NULL,  -- 'buyer', 'seller', 'arbiter'
    PRIMARY KEY (escrow_transaction_id, user_id)
);
```

### 1NF — Repeating Groups in Columns

```sql
-- ❌ Violation: repeating groups
CREATE TABLE payment (
    id uuid PRIMARY KEY,
    fee_1_type varchar(50), fee_1_amount numeric(19,4),
    fee_2_type varchar(50), fee_2_amount numeric(19,4),
    fee_3_type varchar(50), fee_3_amount numeric(19,4)
);

-- ✅ Fix: separate fee table
CREATE TABLE payment_fee (
    id uuid PRIMARY KEY,
    payment_id uuid REFERENCES payment(id),
    fee_type varchar(50) NOT NULL,
    amount numeric(19,4) NOT NULL,
    CONSTRAINT ck_payment_fee_amount_positive CHECK (amount > 0)
);
```

### 3NF — Transitive Dependency

```sql
-- ❌ Violation: buyer_email depends on buyer_id, not on PK
CREATE TABLE escrow_transaction (
    id uuid PRIMARY KEY,
    buyer_id uuid,
    buyer_name varchar(200),   -- depends on buyer_id, not on id
    buyer_email varchar(320),  -- depends on buyer_id, not on id
    amount numeric(19,4)
);

-- ✅ Fix: reference the user table
CREATE TABLE escrow_transaction (
    id uuid PRIMARY KEY,
    buyer_id uuid REFERENCES app_user(id),
    amount numeric(19,4)
);
-- buyer_name and buyer_email live in app_user
```

## When Denormalization Is Justified

Denormalization is acceptable **only** when:

| Scenario | Justification | Pattern |
|---|---|---|
| **CQRS read models** | Query side needs flattened data for fast reads | Separate read-model table populated by domain events |
| **Materialized views** | Expensive joins needed for dashboards/reports | `CREATE MATERIALIZED VIEW` with scheduled refresh |
| **Audit snapshots** | Point-in-time state must be preserved | Store snapshot of related data at event time |
| **Caching columns** | Computed values queried frequently | Denormalized column with trigger/event-driven update |
| **Reporting tables** | Analytics queries span many joins | Star schema in a reporting schema |

> **Rule**: Every denormalized column/table MUST have a code comment or migration comment explaining *why* it exists and *how* it stays in sync.

## PostgreSQL JSONB vs Normalization

| Use JSONB When | Normalize When |
|---|---|
| Schema varies per row (plugin metadata, external API payloads) | Structure is known and consistent |
| Data is read-mostly, rarely queried by inner fields | Fields are used in `WHERE`, `JOIN`, or `ORDER BY` |
| Storing audit trail event payloads | Data has referential integrity needs |
| Semi-structured extension data (custom fields) | Financial amounts, dates, or identity data |

```sql
-- Appropriate JSONB: webhook payload varies by provider
CREATE TABLE webhook_event (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    provider varchar(50) NOT NULL,
    event_type varchar(100) NOT NULL,
    payload jsonb NOT NULL,         -- semi-structured, provider-specific
    received_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_webhook_event_payload_type
    ON webhook_event USING gin (payload jsonb_path_ops);
```

## EF Core Owned Entities for Value Objects

Use owned entities to model DDD value objects without creating separate tables:

```csharp
// Value object
public sealed record Money(decimal Amount, string CurrencyCode);

// Entity
public sealed class EscrowTransaction
{
    public Guid Id { get; init; }
    public Money HoldAmount { get; private set; } = null!;
    public Money FeeAmount { get; private set; } = null!;
}

// Configuration — maps to columns in the same table
public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.ToTable("escrow_transaction");

        builder.OwnsOne(e => e.HoldAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("hold_amount")
                .HasColumnType("numeric(19,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("hold_currency_code")
                .HasMaxLength(3);
        });

        builder.OwnsOne(e => e.FeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("fee_amount")
                .HasColumnType("numeric(19,4)");
            money.Property(m => m.CurrencyCode)
                .HasColumnName("fee_currency_code")
                .HasMaxLength(3);
        });
    }
}
```

> Owned entities preserve 3NF — the value object columns belong to the same entity and depend on the PK.

## Normalization Checklist

- [ ] Every table has a primary key
- [ ] No column stores comma-separated or delimited values
- [ ] No repeating column groups (`fee_1`, `fee_2`, `fee_3`)
- [ ] No partial dependencies on composite keys
- [ ] No transitive dependencies (non-key → non-key)
- [ ] All denormalization is documented with justification
- [ ] JSONB columns are justified (semi-structured or schema-varies)
- [ ] Value objects use owned entities, not separate tables
