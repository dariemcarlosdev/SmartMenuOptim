# Analysis Checklist

Comprehensive checklist to ensure thorough codebase analysis.

## Pre-Analysis Setup

```
- [ ] Identify the solution file (.sln) and all projects
- [ ] Verify you can build the solution (dotnet build)
- [ ] Identify the target framework version (.NET 10, etc.)
- [ ] Locate the entry point (Program.cs)
- [ ] Read README.md and any ARCHITECTURE.md files
```

## Domain Layer Analysis

```
- [ ] List all entities (classes inheriting BaseEntity/Entity/AggregateRoot)
- [ ] List all value objects (records or ValueObject base class)
- [ ] List all enumerations (especially Status/State enums)
- [ ] List all domain interfaces (IRepository, IService patterns)
- [ ] List all domain events (IDomainEvent/INotification)
- [ ] Identify aggregate roots and their boundaries
- [ ] Document entity invariants (constructor guards, property setters)
- [ ] Map entity relationships (1:1, 1:N, N:N)
- [ ] Extract state machines from status enums and transition methods
- [ ] Check for anemic vs. rich domain models
```

## Application Layer Analysis

```
- [ ] List all MediatR commands (write operations)
- [ ] List all MediatR queries (read operations)
- [ ] List all handlers and map to commands/queries
- [ ] List all FluentValidation validators
- [ ] Extract validation rules as business constraints
- [ ] List all pipeline behaviors (logging, validation, transaction)
- [ ] List all DTOs and response models
- [ ] Map command → handler → repository → entity flow
- [ ] Identify cross-cutting concerns (caching, auth checks in handlers)
- [ ] Check for notification handlers (domain event reactions)
```

## Infrastructure Layer Analysis

```
- [ ] Identify DbContext class(es) and database provider
- [ ] List all EF Core entity configurations
- [ ] List all repository implementations
- [ ] Extract relationship configurations (HasOne, HasMany, etc.)
- [ ] List all migrations (chronological schema evolution)
- [ ] Identify external service clients (HTTP, message queue, etc.)
- [ ] List all DI registrations (services, repositories, behaviors)
- [ ] Check for background services (IHostedService, BackgroundService)
- [ ] Identify caching implementations
- [ ] Check for resilience patterns (Polly policies)
```

## Presentation Layer Analysis

```
- [ ] List all API endpoints (controllers or minimal API)
- [ ] List all Blazor pages and components
- [ ] Map endpoints to MediatR commands/queries
- [ ] Check authentication configuration (JWT, cookies, Entra ID)
- [ ] Check authorization policies and role definitions
- [ ] Identify middleware pipeline order
- [ ] Check CORS configuration
- [ ] Check rate limiting configuration
- [ ] List all error handling middleware
```

## Security Analysis

```
- [ ] Identify authentication provider (Entra ID, Identity, IdentityServer)
- [ ] List all [Authorize] attributes and policies
- [ ] Check for endpoints missing authorization
- [ ] Verify input validation on all command handlers
- [ ] Check for SQL injection risks (raw SQL, string concatenation)
- [ ] Verify HTTPS enforcement
- [ ] Check for secret management (user-secrets, Key Vault, env vars)
- [ ] Verify CSRF protection on state-changing endpoints
- [ ] Check data protection configuration (encryption, hashing)
```

## Test Coverage Analysis

```
- [ ] Identify test projects and frameworks (xUnit, NUnit, MSTest)
- [ ] List tested handlers/use cases
- [ ] Identify untested handlers (coverage gaps)
- [ ] Check for integration tests (WebApplicationFactory)
- [ ] Check for domain model tests
- [ ] Check for validator tests
- [ ] Verify test data patterns (builders, factories, fixtures)
- [ ] Calculate approximate test-to-handler ratio
```

## Cross-Cutting Analysis

```
- [ ] Check logging configuration and coverage
- [ ] Check health check endpoints
- [ ] Identify configuration options (IOptions<T> bindings)
- [ ] Check for feature flags
- [ ] Identify telemetry/metrics (OpenTelemetry, Application Insights)
- [ ] Check for API versioning
- [ ] Identify documentation generation (Swagger/OpenAPI)
```

## Final Validation

```
- [ ] Every entity has been cataloged
- [ ] Every handler has been mapped to a requirement
- [ ] Every validator rule has been extracted
- [ ] State machines are fully documented
- [ ] Integration points are identified
- [ ] Gaps and unknowns are explicitly listed
- [ ] Confidence level is assessed for each requirement
- [ ] Specification document is complete and internally consistent
```

## Time Budget Guide

| Phase | Typical Time | Output |
|-------|-------------|--------|
| Reconnaissance | 30 min | Tech stack, project structure |
| Domain Model | 1 hour | Entity/VO/enum catalog |
| Business Logic | 1 hour | Requirements from handlers + validators |
| Integration | 30 min | External dependency map |
| Security | 30 min | Auth/authz audit |
| Testing | 30 min | Coverage assessment |
| Assembly | 1 hour | Final specification document |
| **Total** | **~5 hours** | **Complete discovered specification** |
