# Strangler Fig Pattern Reference

> **Load when:** Designing incremental replacement strategies with facade layers.

## Pattern Overview

The Strangler Fig pattern incrementally replaces a legacy system by routing traffic through a facade that gradually redirects from old to new implementations — like a fig tree that grows around and eventually replaces its host.

```
┌───────────────┐
│   Clients     │
└───────┬───────┘
        │
┌───────▼───────┐
│   Facade /    │  ← Routes requests to old or new
│  API Gateway  │
├───────┬───────┤
│  New  │ Legacy│  ← Coexist during migration
│ Code  │ Code  │
└───────┴───────┘
```

## Implementation with YARP (Yet Another Reverse Proxy)

YARP is the .NET reverse proxy that enables strangler fig routing in ASP.NET Core.

### Basic YARP Configuration

```csharp
// Program.cs — Strangler Fig facade
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// New endpoints handled by this service
app.MapGet("/api/v2/escrows/{id}", async (string id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetEscrowQuery(id));
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

// Everything else proxied to legacy
app.MapReverseProxy();
app.Run();
```

```json
// appsettings.json — YARP route configuration
{
  "ReverseProxy": {
    "Routes": {
      "legacy-catchall": {
        "ClusterId": "legacy",
        "Match": {
          "Path": "{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "legacy": {
        "Destinations": {
          "legacy-app": {
            "Address": "https://legacy.internal.nextruzt.io"
          }
        }
      }
    }
  }
}
```

### Progressive Route Migration

Migrate routes one at a time — each migrated route gets handled by the new service:

```
Phase 1: /api/escrows/*     → New service (first migration target)
         /api/*              → Legacy (everything else)

Phase 2: /api/escrows/*     → New service
         /api/payments/*    → New service (second migration)
         /api/*              → Legacy (remaining)

Phase 3: /api/escrows/*     → New service
         /api/payments/*    → New service
         /api/users/*       → New service (third migration)
         /api/*              → Legacy (shrinking)

Phase N: All routes          → New service (legacy decommissioned)
```

## Anti-Corruption Layer (ACL)

The ACL translates between the legacy domain model and the new domain model, preventing legacy concepts from contaminating the new system.

```csharp
// Anti-corruption layer translates legacy DTOs to new domain models
public sealed class LegacyEscrowAdapter : IEscrowRepository
{
    private readonly ILegacyEscrowClient _legacyClient;
    private readonly ILogger<LegacyEscrowAdapter> _logger;

    public LegacyEscrowAdapter(ILegacyEscrowClient legacyClient, ILogger<LegacyEscrowAdapter> logger)
    {
        _legacyClient = legacyClient;
        _logger = logger;
    }

    public async Task<Escrow?> GetByIdAsync(EscrowId id, CancellationToken ct)
    {
        var legacyDto = await _legacyClient.GetTransactionAsync(id.Value, ct);
        if (legacyDto is null) return null;

        // Translate legacy model to new domain model
        return new Escrow(
            id: new EscrowId(legacyDto.TransactionNumber),
            buyer: new PartyInfo(legacyDto.BuyerCode, legacyDto.BuyerName),
            seller: new PartyInfo(legacyDto.VendorCode, legacyDto.VendorName),
            amount: Money.FromLegacy(legacyDto.AmountInCents, legacyDto.CurrencyCode),
            status: MapLegacyStatus(legacyDto.StatusFlag)
        );
    }

    private static EscrowStatus MapLegacyStatus(string flag) => flag switch
    {
        "A" => EscrowStatus.Active,
        "P" => EscrowStatus.Pending,
        "C" => EscrowStatus.Completed,
        "X" => EscrowStatus.Cancelled,
        _ => throw new InvalidOperationException($"Unknown legacy status: {flag}")
    };
}
```

## Feature Flag Integration

Use feature flags to control the cutover between legacy and new implementations:

```csharp
// Feature flag controlled routing middleware
public sealed class StranglerFigMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFeatureManager _features;

    public StranglerFigMiddleware(RequestDelegate next, IFeatureManager features)
    {
        _next = next;
        _features = features;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (path?.StartsWith("/api/escrows") == true
            && await _features.IsEnabledAsync("NewEscrowService"))
        {
            // Route to new service (handled by this app's controllers)
            await _next(context);
        }
        else
        {
            // Proxy to legacy
            await ProxyToLegacyAsync(context);
        }
    }
}
```

## Dual-Write Pattern for Data Migration

During migration, write to both old and new databases to maintain consistency:

```csharp
public sealed class DualWriteEscrowRepository : IEscrowRepository
{
    private readonly NewEscrowDbContext _newDb;
    private readonly ILegacyEscrowClient _legacyClient;
    private readonly ILogger<DualWriteEscrowRepository> _logger;

    public async Task<EscrowId> CreateAsync(Escrow escrow, CancellationToken ct)
    {
        // Write to new database (source of truth)
        _newDb.Escrows.Add(escrow);
        await _newDb.SaveChangesAsync(ct);

        // Write to legacy (best-effort during transition)
        try
        {
            await _legacyClient.CreateTransactionAsync(MapToLegacy(escrow), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Dual-write to legacy failed for escrow {EscrowId}. New DB is source of truth.",
                escrow.Id);
            // Queue for retry via outbox pattern
        }

        return escrow.Id;
    }
}
```

## Migration Progress Tracking

Track which routes and data have been migrated:

```markdown
| Endpoint | Legacy | New | Status | Cutover Date |
|---|---|---|---|---|
| GET /api/escrows/{id} | ✅ | ✅ | Parallel run | 2024-02-15 |
| POST /api/escrows | ✅ | ✅ | New primary | 2024-03-01 |
| GET /api/payments | ✅ | 🔨 | In progress | — |
| POST /api/users | ✅ | ❌ | Not started | — |
```
