# Migration Strategies Reference

> **Load when:** Planning database, API, or framework migrations.

## Database Migration Strategies

### Expand-Contract Pattern

The safest approach for schema changes in production systems.

```
Phase 1: EXPAND    — Add new columns/tables alongside old ones
Phase 2: MIGRATE   — Backfill data, update application code
Phase 3: VERIFY    — Run parallel reads, compare results
Phase 4: CONTRACT  — Remove old columns/tables
```

### SQL Server to PostgreSQL Migration

A common migration path for .NET applications moving to open-source infrastructure.

**Key Differences to Address:**

| SQL Server | PostgreSQL | Migration Action |
|---|---|---|
| `IDENTITY` columns | `GENERATED ALWAYS AS IDENTITY` | Update DDL |
| `NVARCHAR(MAX)` | `TEXT` | Simplify types |
| `DATETIME2` | `TIMESTAMPTZ` | Use timezone-aware |
| `BIT` | `BOOLEAN` | Direct mapping |
| `UNIQUEIDENTIFIER` | `UUID` | Direct mapping |
| Stored procedures (T-SQL) | Functions (PL/pgSQL) | Rewrite or remove |
| `@@IDENTITY` / `SCOPE_IDENTITY` | `RETURNING id` | Use EF Core |

**EF Core Provider Switch:**

```csharp
// Before: SQL Server
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// After: PostgreSQL with Npgsql
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MigrationsHistoryTable("__ef_migrations", "public");
        npgsql.EnableRetryOnFailure(3);
    }));
```

**Data Migration Script Pattern:**

```bash
# Step 1: Export from SQL Server
bcp "SELECT * FROM escrows" queryout escrows.csv -S sqlserver -d EscrowDB -T -c -t ","

# Step 2: Import to PostgreSQL
psql -h localhost -d escrow -c "\COPY escrows FROM 'escrows.csv' WITH CSV HEADER"

# Step 3: Verify row counts match
# Step 4: Run application integration tests against PostgreSQL
```

### Zero-Downtime Database Migration

For systems requiring continuous availability during migration:

```csharp
// Dual-read pattern: Read from new, fall back to old during migration
public sealed class MigrationAwareEscrowRepository : IEscrowRepository
{
    private readonly NewDbContext _newDb;
    private readonly LegacyDbContext _legacyDb;
    private readonly IFeatureManager _features;

    public async Task<Escrow?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (await _features.IsEnabledAsync("ReadFromNewDb"))
        {
            var result = await _newDb.Escrows.FindAsync([id], ct);
            if (result is not null) return result;

            // Fallback to legacy if not yet migrated
            return await ReadFromLegacyAsync(id, ct);
        }

        return await ReadFromLegacyAsync(id, ct);
    }
}
```

## API Migration Strategies

### API Versioning with Asp.Versioning

```csharp
// Support both old and new API versions simultaneously
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
});

// V1 controller (legacy behavior)
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/escrows")]
public sealed class EscrowsV1Controller : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<LegacyEscrowDto> Get(int id) { /* old format */ }
}

// V2 controller (new behavior)
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/escrows")]
public sealed class EscrowsV2Controller : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<EscrowDto> Get(string id) { /* new format with GUID IDs */ }
}
```

### API Contract Migration Checklist

```markdown
1. [ ] Document all current API consumers and their usage patterns
2. [ ] Design the new API contract (OpenAPI spec)
3. [ ] Implement the new version alongside the old one
4. [ ] Notify consumers of the new version with migration guide
5. [ ] Set a deprecation date for the old version
6. [ ] Monitor old version usage — track which consumers have migrated
7. [ ] Send reminders to remaining consumers
8. [ ] Disable the old version after the deprecation date
```

## Framework Migration Strategies

### .NET Framework → .NET 10

**Incremental Migration Path:**

```
Step 1: Upgrade to .NET Framework 4.8 (latest)
Step 2: Replace System.Web dependencies with OWIN/Katana
Step 3: Move shared libraries to .NET Standard 2.0
Step 4: Create new .NET 10 host project
Step 5: Migrate controllers/pages one at a time using strangler fig
Step 6: Migrate data access (EF6 → EF Core)
Step 7: Migrate authentication (OWIN → ASP.NET Core Identity/Entra)
Step 8: Decommission .NET Framework host
```

**Portability Analysis:**

```bash
# Analyze .NET Framework project for migration compatibility
dotnet tool install -g upgrade-assistant
upgrade-assistant analyze <solution.sln>

# Generate migration report
upgrade-assistant upgrade <solution.sln> --non-interactive --target-tfm net10.0
```

### Web Forms → Blazor Server

Migration path for legacy ASP.NET Web Forms applications:

| Web Forms Concept | Blazor Equivalent | Migration Notes |
|---|---|---|
| `.aspx` pages | `.razor` components | Rewrite markup, keep logic |
| Code-behind (`.aspx.cs`) | Code-behind (`.razor.cs`) | Similar pattern, new lifecycle |
| `ViewState` | Component state / cascading params | Explicit state management |
| `UpdatePanel` | `StateHasChanged()` | Automatic with events |
| `Session` | Scoped services | DI-based state |
| Master pages | Layouts (`MainLayout.razor`) | Simpler composition |
| User controls | Components | Better reusability |
| `GridView` | `QuickGrid` or custom table | More flexible |

```csharp
// Web Forms code-behind
public partial class EscrowList : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            GridView1.DataSource = GetEscrows();
            GridView1.DataBind();
        }
    }
}

// Blazor equivalent (code-behind)
public partial class EscrowList : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    private IReadOnlyList<EscrowDto> _escrows = [];

    protected override async Task OnInitializedAsync()
    {
        _escrows = await Mediator.Send(new GetEscrowsQuery());
    }
}
```
