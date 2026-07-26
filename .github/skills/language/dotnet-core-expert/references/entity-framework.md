# Entity Framework Core Reference

> **Load when:** Configuring DbContext, creating migrations, defining relationships, or optimizing queries.

## DbContext Configuration

```csharp
public sealed class EscrowDbContext(
    DbContextOptions<EscrowDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Escrow> Escrows => Set<Escrow>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Dispute> Disputes => Set<Dispute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EscrowDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Audit timestamps
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(ct);
    }
}
```

## Entity Configuration (Fluent API)

```csharp
public sealed class EscrowConfiguration : IEntityTypeConfiguration<Escrow>
{
    public void Configure(EntityTypeBuilder<Escrow> builder)
    {
        builder.ToTable("Escrows");
        builder.HasKey(e => e.Id);

        // Value object conversion
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EscrowId(value))
            .HasColumnName("Id");

        // Owned value object (Money)
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Value).HasColumnName("Amount").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
        });

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.BuyerId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.SellerId).IsRequired().HasMaxLength(50);

        // Relationships
        builder.HasMany(e => e.Payments)
            .WithOne()
            .HasForeignKey(p => p.EscrowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Dispute)
            .WithOne()
            .HasForeignKey<Dispute>(d => d.EscrowId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.BuyerId);
        builder.HasIndex(e => e.SellerId);
        builder.HasIndex(e => e.CreatedAt);
    }
}
```

## Migrations

```bash
# Create a migration
dotnet ef migrations add AddEscrowDisputeRelationship -p src/Infrastructure -s src/Presentation

# Apply migrations
dotnet ef database update -p src/Infrastructure -s src/Presentation

# Generate SQL script (for production)
dotnet ef migrations script -p src/Infrastructure -s src/Presentation --idempotent -o migrations.sql
```

## Query Optimization

### Read-Only Queries (AsNoTracking)

```csharp
// Always use AsNoTracking for read-only queries
var escrows = await context.Escrows
    .AsNoTracking()
    .Where(e => e.Status == EscrowStatus.Funded)
    .OrderByDescending(e => e.CreatedAt)
    .Take(20)
    .ToListAsync(ct);
```

### Projections (Select)

```csharp
// Project to DTOs instead of loading full entities
var summaries = await context.Escrows
    .AsNoTracking()
    .Where(e => e.BuyerId == buyerId)
    .Select(e => new EscrowSummaryDto(
        e.Id.Value,
        e.Amount.Value,
        e.Amount.Currency,
        e.Status.ToString(),
        e.CreatedAt))
    .ToListAsync(ct);
```

### Avoiding N+1 Queries

```csharp
// BAD — N+1: loads escrow, then lazy-loads each payment
var escrow = await context.Escrows.FindAsync(id);
foreach (var payment in escrow.Payments) { ... } // N queries!

// GOOD — eager load with Include
var escrow = await context.Escrows
    .Include(e => e.Payments)
    .FirstOrDefaultAsync(e => e.Id == id, ct);

// BETTER — split query for large includes
var escrow = await context.Escrows
    .Include(e => e.Payments)
    .Include(e => e.Dispute)
    .AsSplitQuery()
    .FirstOrDefaultAsync(e => e.Id == id, ct);
```

### Pagination

```csharp
public async Task<PaginatedList<T>> GetPagedAsync<T>(
    IQueryable<T> query, int page, int pageSize, CancellationToken ct)
{
    var totalCount = await query.CountAsync(ct);
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);
    return new PaginatedList<T>(items, totalCount, page, pageSize);
}
```

## Connection Resilience

```csharp
services.AddDbContext<EscrowDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sql.CommandTimeout(30);
        sql.MigrationsAssembly("Infrastructure");
    }));
```

## Global Query Filters

```csharp
// Soft delete filter — automatically excludes deleted entities
builder.HasQueryFilter(e => !e.IsDeleted);

// Tenant isolation — automatically scopes to current tenant
builder.HasQueryFilter(e => e.TenantId == _currentTenantId);
```
