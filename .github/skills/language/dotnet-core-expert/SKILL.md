---
name: dotnet-core-expert
description: "Deep .NET 10 expertise for building high-performance applications with minimal APIs, Clean Architecture, EF Core, CQRS/MediatR, JWT auth, AOT compilation. Use for .NET Core, ASP.NET Core, minimal API, microservices, Entity Framework."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: backend
  triggers: .NET Core, .NET 10, ASP.NET Core, C# 13, minimal API, Entity Framework Core, microservices .NET, CQRS, MediatR
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: csharp-developer, architecture-reviewer, test-generator, database-optimizer
---

# .NET Core Expert

A .NET 10 specialist that designs and implements high-performance applications using Clean Architecture, CQRS/MediatR, minimal APIs, Entity Framework Core, JWT/Entra ID authentication, and cloud-native patterns — optimized for the NexTruzt.io fintech escrow platform.

## When to Use This Skill

- Creating new ASP.NET Core minimal API endpoints for the escrow platform
- Implementing CQRS command/query handlers with MediatR pipeline behaviors
- Designing Clean Architecture layers (Domain, Application, Infrastructure, Presentation)
- Configuring Entity Framework Core with migrations, relationships, and query optimization
- Setting up JWT or Entra ID authentication with policy-based authorization
- Building cloud-native services with health checks, configuration, and .NET Aspire
- Implementing microservice communication patterns (gRPC, message queues, HTTP clients)
- Optimizing for AOT compilation and trimming in containerized deployments

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Minimal APIs | `references/minimal-apis.md` | Creating endpoints, routing, middleware |
| Clean Architecture | `references/clean-architecture.md` | CQRS, MediatR, layers, DI patterns |
| Entity Framework | `references/entity-framework.md` | DbContext, migrations, relationships, query optimization |
| Authentication | `references/authentication.md` | JWT, Entra ID, Identity, authorization policies |
| Cloud-Native | `references/cloud-native.md` | Docker, health checks, configuration, Aspire |

## Core Workflow

### Step 1 — Analyze Requirements

Determine the feature scope and which architectural layers are affected.

1. **Identify the domain concept** — Map the requirement to domain entities, value objects, and aggregates.
2. **Determine the slice** — Is this a new vertical slice (command + handler + validator + endpoint) or a cross-cutting change?
3. **Check existing patterns** — Scan the codebase for similar features to maintain consistency.
4. **Plan the layers** — Identify which layers need changes: Domain, Application, Infrastructure, Presentation.

**✅ Checkpoint: Domain concepts identified, slice type determined, affected layers mapped before writing code.**

### Step 2 — Implement Domain and Application Layers

Build the core business logic following DDD and CQRS patterns.

1. **Domain entities** — Create or update entities with rich behavior, value objects, and domain events.
2. **Command/Query records** — Define `IRequest<TResponse>` records with descriptive names.
3. **Handlers** — Implement `IRequestHandler<TRequest, TResponse>` with single-responsibility logic.
4. **Validators** — Create FluentValidation validators for all commands.
5. **Pipeline behaviors** — Leverage existing `ValidationBehavior<,>`, `LoggingBehavior<,>` via MediatR pipeline.

**✅ Checkpoint: Domain model compiles, handler logic is testable in isolation, validators cover all inputs.**

### Step 3 — Implement Infrastructure Layer

Wire up data access, external services, and cross-cutting concerns.

1. **EF Core configuration** — Add entity configurations with `IEntityTypeConfiguration<T>`.
2. **Repository implementation** — Implement repository interfaces defined in the Application layer.
3. **Migrations** — Create and test EF Core migrations for schema changes.
4. **External services** — Implement adapters for third-party APIs with Polly resilience policies.

**✅ Checkpoint: Migrations apply cleanly, repository queries are efficient (check with SQL logging), external calls have retry policies.**

### Step 4 — Implement Presentation Layer

Expose the feature through minimal API endpoints.

1. **Define endpoints** — Create typed endpoint classes using `IEndpointRouteBuilder` extensions.
2. **Map routes** — Use `app.MapGet/Post/Put/Delete` with route groups for versioning.
3. **Add authorization** — Apply `[Authorize(Policy = "...")]` or `.RequireAuthorization()` on endpoints.
4. **Configure serialization** — Use `TypedResults` for compile-time response type checking.
5. **Add OpenAPI metadata** — Use `.WithName()`, `.Produces<T>()`, `.WithTags()` for Swagger docs.

**✅ Checkpoint: Endpoints respond correctly, authorization enforced, Swagger shows correct schemas.**

### Step 5 — Test and Validate

Verify correctness across all layers.

1. **Unit tests** — Test handlers, validators, and domain logic in isolation with mocked dependencies.
2. **Integration tests** — Use `WebApplicationFactory<T>` with test database for end-to-end endpoint testing.
3. **Build verification** — Run `dotnet build` with `TreatWarningsAsErrors` and `dotnet test` with coverage.
4. **AOT compatibility** — Verify trimming warnings are resolved if targeting Native AOT.

**✅ Checkpoint: All tests pass, no build warnings, coverage meets team threshold.**

## Quick Reference

### Vertical Slice — Command + Handler + Endpoint

```csharp
// Application/Features/Escrows/CreateEscrow/CreateEscrowCommand.cs
public sealed record CreateEscrowCommand(
    string BuyerId,
    string SellerId,
    decimal Amount,
    string Currency) : IRequest<CreateEscrowResult>;

public sealed record CreateEscrowResult(Guid EscrowId, string Status);

// Application/Features/Escrows/CreateEscrow/CreateEscrowValidator.cs
public sealed class CreateEscrowValidator : AbstractValidator<CreateEscrowCommand>
{
    public CreateEscrowValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SellerId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000);
        RuleFor(x => x.Currency).Must(c => new[] { "USD", "EUR", "GBP" }.Contains(c));
    }
}

// Application/Features/Escrows/CreateEscrow/CreateEscrowHandler.cs
public sealed class CreateEscrowHandler(
    IEscrowRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateEscrowCommand, CreateEscrowResult>
{
    public async Task<CreateEscrowResult> Handle(
        CreateEscrowCommand request, CancellationToken ct)
    {
        var escrow = Escrow.Create(
            request.BuyerId, request.SellerId, 
            Money.From(request.Amount, request.Currency));
        
        await repository.AddAsync(escrow, ct);
        await unitOfWork.SaveChangesAsync(ct);
        
        return new CreateEscrowResult(escrow.Id, escrow.Status.ToString());
    }
}

// Presentation/Endpoints/EscrowEndpoints.cs
public static class EscrowEndpoints
{
    public static void MapEscrowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/escrows")
            .WithTags("Escrows")
            .RequireAuthorization("EscrowManager");

        group.MapPost("/", async (CreateEscrowCommand command, IMediator mediator) =>
            TypedResults.Created($"/api/v1/escrows/{(await mediator.Send(command)).EscrowId}",
                await mediator.Send(command)))
            .WithName("CreateEscrow")
            .Produces<CreateEscrowResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
```

### DI Registration Pattern

```csharp
// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IEscrowRepository, EscrowRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EscrowDbContext>());

        return services;
    }
}
```

## Constraints

### MUST DO

- Follow Clean Architecture — dependencies point inward, Domain has zero external references
- Use MediatR for all command/query dispatch — no direct service calls from endpoints
- Validate all commands with FluentValidation before handler execution
- Apply `[Authorize]` or `.RequireAuthorization()` on every endpoint — default deny
- Use `CancellationToken` on all async methods and propagate through the call chain
- Configure EF Core with explicit `IEntityTypeConfiguration<T>` — no data annotations on entities
- Use `AsNoTracking()` for all read-only queries
- Apply Polly resilience policies on all external HTTP calls

### MUST NOT

- Do not inject `IConfiguration` directly into services — use `IOptions<T>` pattern
- Do not put business logic in controllers or endpoints — delegate to MediatR handlers
- Do not use `DbContext` directly in handlers — access through repository interfaces
- Do not hardcode connection strings or secrets — use `dotnet user-secrets` or Key Vault
- Do not skip migrations — every schema change must have a corresponding migration
- Do not use `async void` — always return `Task` or `ValueTask`
- Do not expose domain entities in API responses — use DTOs or records

## Output Template

```markdown
# Feature Implementation

**Feature:** {feature_name}
**Layers Modified:** {Domain|Application|Infrastructure|Presentation}
**Pattern:** {CQRS Vertical Slice|Cross-cutting|Infrastructure Only}

## Files Created/Modified

| File | Layer | Change |
|---|---|---|
| {path} | {layer} | {created|modified|deleted} |

## Domain Changes
{entity/value object/aggregate changes}

## Application Changes
{command/query/handler/validator changes}

## Infrastructure Changes
{EF configuration/repository/migration changes}

## Endpoint Changes
{route/authorization/serialization changes}

## Test Coverage
- [ ] Unit tests for handler logic
- [ ] Validation tests for all rules
- [ ] Integration tests for endpoints
- [ ] Migration tested against dev database
```

## Integration Notes

### Copilot CLI
Trigger with: `.NET Core`, `minimal API`, `CQRS handler`, `EF Core migration`, `add endpoint`

### Claude
Include this file in project context. Trigger with: "Implement a .NET feature for [requirement]"

### Gemini
Reference via `GEMINI.md` or direct inclusion. Trigger with: "Build a .NET 10 service for [feature]"
