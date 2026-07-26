# CLAUDE.md

Guidance for Claude Code / AI agents working in this repo. Keep changes surgical; match existing patterns.

> Last synced with codebase: 2026-07-12

## What this is

SmartMenuOptim — restaurant menu optimization platform. Clean Architecture + DDD + CQRS (MediatR). Blazor Server UI + separate REST API, PostgreSQL via EF Core.

## Solution layout

`SmartMenuOptim.sln` — 7 projects:

| Project | Type | TFM | Role |
|---------|------|-----|------|
| `SmartMenuOptim.Server` | Blazor Server (`Sdk.Web`, MudBlazor, Polly) | net9.0 | Interactive UI host |
| `SmartMenuOptim.API` | REST API (`Sdk.Web`, Swagger, EF Core, Sentry) | net8.0 | Backend endpoints |
| `SmartMenuOptim.Application` | class lib | - | Use cases, CQRS handlers, DTOs, contracts |
| `SmartMenuOptim.Domain` | class lib | - | Entities, aggregates, value objects, domain events |
| `SmartMenuOptim.Infrastructure` | class lib | - | EF Core, repositories, persistence, jobs |
| `SmartMenuOptim.Shared` | class lib | - | Shared contracts (API-referenced) |
| `SmartMenuOptim.Tests` | xUnit | net9.0 | Tests (FluentAssertions, Moq, EF InMemory) |

Two independent web roots (Server + API) — NOT a Blazor WASM hosted pair. TFM mismatch (net9 Server vs net8 API) is intentional/current state.

## Build / run / test

```bash
dotnet build SmartMenuOptim.sln
dotnet test SmartMenuOptim.Tests/SmartMenuOptim.Tests.csproj

# Run API (Swagger at /swagger)
dotnet run --project SmartMenuOptim.API --launch-profile https.Development     # https://localhost:7119

# Run Blazor UI
dotnet run --project SmartMenuOptim.Server --launch-profile https.Development   # https://localhost:7060
```

**Visual Studio F5:** multi-project launch profile `Blazor/Api` (in `SmartMenuOptim.slnLaunch.user`) starts both. `DebugTarget` values MUST match a profile key in each project's `launchSettings.json` — a bad target causes `Ensure that correct project is set as startup project`.

## Architecture rules (dependencies point inward)

```
Presentation (Server UI) → Application → Domain
                            Infrastructure → Application/Domain
```

- Domain: zero framework deps. Entities own invariants. `record` for value objects + domain events.
- Application: inject interfaces only, never `DbContext`. Return DTOs, never domain entities to UI. FluentValidation next to each command.
- Infrastructure: implements repository interfaces; EF configs in `Data/Configurations/`; never leak `DbContext` outward.
- Presentation: no repository/`DbContext` injection. Go through `IMediator.Send()`. Code-behind mandatory (`.razor` + `.razor.cs` + `.razor.css`).

## Conventions

- CQRS via MediatR commands/queries + handlers in Application.
- Repository pattern per aggregate (no generic `IRepository<T>`).
- `[Authorize]` default-deny on endpoints/pages; policy-based, not inline roles.
- Parameterized EF queries only; secrets via user-secrets (dev) / env vars (prod) — never `appsettings.json`.
- Config loading: API `Program.cs` switches by env (user-secrets local, env vars Azure, `/app/secrets.json` Docker).
- Resilience: Polly on Server HTTP clients.

## Docs map (progressive disclosure — load only what task needs)

- `docs/README.md` — solution doc index (8 folders, feature/arch/security).
- `docs/02-Architecture/` — Clean/DDD, multitenant, ADRs (ADR-004, ADR-005).
- `docs/10-ISSUES-QUICK-FIX/` — bug fix log (BUG-001…005, order management).
- Layer docs live in each project's `docs/` (Domain/Application/Infrastructure).
- Deeper AI coding rules: `.claude/rules/*.md` (clean-architecture, cqrs-mediatr, ddd-domain, ef-core, owasp-security, polly-resilience, testing-standards, blazor-components, mvp-first, memory-optimization).

## Testing

xUnit + FluentAssertions (AAA pattern), Moq for mocks. Handlers using `BeginTransactionAsync()` need SQLite in-memory, NOT EF Core InMemory. One happy-path test per feature minimum.

## Notes

- `SmartMenuOptim.API/Program.cs:20` — Sentry DSN hardcoded. Prefer config/env var.
- Multi-tenancy middleware present but commented out (`TenantResolverMiddleware`) — not enabled yet.
