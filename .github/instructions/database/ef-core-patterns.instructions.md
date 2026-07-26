---
applyTo: "EscrowApp/Data/**/*.cs, EscrowApp/Migrations/**/*.cs"
---

# Entity Framework Core & PostgreSQL Patterns — NexTruzt.io Escrow Data Layer

## PostgreSQL-Specific Conventions

- Use `Npgsql.EntityFrameworkCore.PostgreSQL` as the database provider.
- Map C# `decimal` to `numeric(18,4)` for monetary values — never use `real` or `double precision`.
- Use `jsonb` columns for semi-structured data (e.g., metadata dictionaries) via `.HasColumnType("jsonb")`.
- Use `uuid` for primary keys — PostgreSQL handles `Guid` natively.
- Use `timestamptz` for all `DateTimeOffset` properties.
- If project conventions dictate **snake_case** column names, configure via `UseSnakeCaseNamingConvention()` from `EFCore.NamingConventions` — do not rename manually in Fluent API.

## EscrowDbContext Configuration

- One `DbContext` class: `EscrowDbContext` — registered as a **scoped** service.
- Apply all entity configurations via `IEntityTypeConfiguration<T>` in separate files, loaded with `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- Define **unique constraints** where the domain demands them:
  - `IdempotencyKey` on `EscrowTransaction` (prevents duplicate payment submissions).
  - Composite unique on (`ActorId`, `TransactionId`) for participant enrollment.
- Define **indexes** on frequently queried columns:
  - `Status` on `EscrowTransaction` (filtered queries by lifecycle state).
  - `CreatedAt` for time-range queries and dashboards.
  - `StripePaymentIntentId` for webhook correlation lookups.
- Configure relationships explicitly — never rely on convention for navigation properties in a DDD model.

```csharp
public sealed class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.IdempotencyKey).IsUnique();
        builder.HasIndex(e => e.Status);

        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnType("numeric(18,4)");
            money.Property(m => m.Currency).HasConversion<string>();
        });

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
```

## Repository Pattern

- Define `IEscrowTransactionRepository` in the **Application** (or Domain) layer — it expresses domain intent, not SQL.
- The implementation (`EscrowTransactionRepository`) lives in **Infrastructure/Data** and depends on `EscrowDbContext`.
- Repository methods return **domain entities**, not DTOs — mapping to DTOs happens in MediatR handlers or projections.
- Provide only the operations the domain needs: `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `ExistsByIdempotencyKeyAsync`.
- Never expose `IQueryable<T>` from the repository — it leaks persistence concerns into the Application layer.

```csharp
public interface IEscrowTransactionRepository
{
    Task<EscrowTransaction?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(EscrowTransaction transaction, CancellationToken ct);
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct);
}
```

## Read-Only Query Patterns

- Use `AsNoTracking()` on **every** read-only query — eliminates change-tracker overhead.
- Prefer **projections** with `Select()` over loading full entities when the consumer needs a subset of fields.
- Use compiled queries (`EF.CompileAsyncQuery`) for hot-path lookups (e.g., transaction status checks).

```csharp
// ✅ Projection — loads only what the query needs
var summary = await _context.EscrowTransactions
    .AsNoTracking()
    .Where(t => t.Id == id)
    .Select(t => new TransactionSummaryDto(t.Id, t.Status, t.Amount.Amount))
    .SingleOrDefaultAsync(ct);

// ❌ Full entity load for a read-only view
var entity = await _context.EscrowTransactions.FindAsync(id);
return new TransactionSummaryDto(entity.Id, entity.Status, entity.Amount.Amount);
```

## Split Queries

- When an `Include()` chain loads **multiple collections**, use `.AsSplitQuery()` to avoid Cartesian explosion.
- Single-collection includes can stay as a single query — split only when needed.

```csharp
var transaction = await _context.EscrowTransactions
    .Include(t => t.Actors)
    .Include(t => t.Milestones)
    .AsSplitQuery()
    .SingleOrDefaultAsync(t => t.Id == id, ct);
```

## Migration Conventions

- Migration names must be **descriptive**: `AddIdempotencyKeyIndex`, `CreateActorsTable` — never `Migration1`.
- Always review the generated SQL (`dotnet ef migrations script`) before applying to any shared environment.
- Keep migrations **additive** — avoid destructive changes (drop column, rename) unless behind a planned migration strategy.
- Never put seed data or business logic in migrations.
- Use `migrationBuilder.Sql(...)` sparingly — only for DDL that EF cannot express.

## Connection String Management

- **Never** hardcode connection strings in code or `appsettings.json` for production.
- Use the **Options pattern**: bind `PostgresOptions` from configuration, inject `IOptions<PostgresOptions>`.
- Development: use `dotnet user-secrets` or `appsettings.Development.json`.
- Production: use environment variables or Azure Key Vault / secret manager.
- Configure connection pooling and timeouts explicitly in the connection string.

## Concurrency Control

- `EscrowTransaction` must use **optimistic concurrency** — a `RowVersion` / `xmin` concurrency token.
- For PostgreSQL, use the `xmin` system column as a concurrency token:

```csharp
builder.UseXminAsConcurrencyToken();
```

- Handle `DbUpdateConcurrencyException` in the Application layer — retry or return a conflict result, never silently overwrite.

## Seeding

- Use `HasData()` **only** for reference/lookup data: `EscrowStatus` enum table, `Currency` codes.
- Never seed transactional business data.
- Seed data must be deterministic and idempotent across migration runs.

## Anti-Patterns to Avoid

| Anti-Pattern | Why It's Harmful | Correct Approach |
|---|---|---|
| `DbContext` in Application/Presentation layers | Bypasses repository abstraction, couples layers | Access data only through `IEscrowTransactionRepository` |
| Lazy loading enabled | Silent N+1 queries, unpredictable performance | Use eager loading with explicit `Include()` |
| Returning `IQueryable` from repository | Leaks persistence concerns, untestable | Return materialized collections or single entities |
| `SaveChanges()` inside repository methods | Breaks unit-of-work boundaries | Call `SaveChangesAsync()` in the handler or via `IUnitOfWork` |
| String interpolation in raw SQL | SQL injection risk | Use `FromSqlInterpolated` or parameterized queries |
| `Find()` / `FindAsync()` for read-only queries | Pollutes change tracker unnecessarily | Use `AsNoTracking().SingleOrDefaultAsync()` |
