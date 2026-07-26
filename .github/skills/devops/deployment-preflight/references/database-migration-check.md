# Database Migration Check — EF Core Migration Safety for PostgreSQL

Database migrations are the highest-risk deployment component. This covers pending migration detection, backward compatibility, and PostgreSQL-specific safety.

## Detecting Pending Migrations + Startup Check

```csharp
// Program.cs — fail startup in Production if migrations are pending
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EscrowDbContext>();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

    if (pending.Count > 0)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Pending migrations: {Migrations}", string.Join(", ", pending));

        if (app.Environment.IsProduction())
            throw new InvalidOperationException(
                $"Cannot start with {pending.Count} pending migrations.");
    }
}
```

## Backward Compatibility Rules

| Operation | Safety | Mitigation |
|---|---|---|
| Add nullable column | ✅ Safe | None needed |
| Add table / index | ✅ Safe | Use `CONCURRENTLY` for indexes |
| Add non-nullable column w/ default | ⚠️ Caution | Set `HasDefaultValue()` |
| Rename column | 🚫 Unsafe | Two-phase: add → migrate → drop |
| Change column type | 🚫 Unsafe | Two-phase with data migration |
| Drop column / table | 🚫 Unsafe | Remove code refs first, drop next release |

## Zero-Downtime Checklist

1. Review SQL: `dotnet ef migrations script --idempotent -o migration.sql`
2. Test on staging clone: `dotnet ef database update --connection "Host=staging-clone;..."`
3. Verify old app version works with new schema
4. Test rollback: `dotnet ef database update {PreviousMigration}`
5. Estimate runtime on production-scale data
6. Check for table-level locks on high-traffic tables

## Rollback Verification

Every migration must have a tested `Down()`:

```csharp
public partial class AddEscrowStatusColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status", table: "EscrowTransactions",
            type: "varchar(50)", nullable: true, defaultValue: "Pending");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Status", table: "EscrowTransactions");
    }
}
```

## PostgreSQL-Specific Considerations

**Table locks:** `ALTER TABLE` acquires `ACCESS EXCLUSIVE` locks. Set `lock_timeout = '5s'` and schedule DDL during low-traffic windows.

**Concurrent indexes:** Use `CONCURRENTLY` to avoid blocking writes:

```csharp
migrationBuilder.Sql(
    "CREATE INDEX CONCURRENTLY IF NOT EXISTS " +
    "\"IX_EscrowTransactions_Status\" ON \"EscrowTransactions\" (\"Status\")",
    suppressTransaction: true); // CONCURRENTLY cannot run inside a transaction
```

**Large tables:** PostgreSQL 11+ adds columns with defaults as metadata-only (fast). Adding `NOT NULL` constraints on existing columns requires a full table scan.

## Preflight Checklist

- [ ] Migration SQL reviewed — no unsafe operations without compat period
- [ ] `Down()` rollback tested on staging clone
- [ ] No `ACCESS EXCLUSIVE` locks on high-traffic tables without maintenance window
- [ ] Indexes use `CONCURRENTLY`; runtime fits deployment window
- [ ] Idempotent script generated for emergency manual application
