---
applyTo: "**/*"
---

# MVP-First Development Rules

> Ship a working product fast. Iterate from there. These rules override perfectionism.

## Core Principle

**Working software > Perfect architecture.** Every decision should be filtered through:
_"Does this get us closer to a usable product, or is it premature optimization?"_

## 1. The MVP Decision Filter

Before implementing anything, ask:

| Question | If YES | If NO |
|----------|--------|-------|
| Does the user see or interact with this? | Build it | Defer it |
| Does the app crash without this? | Build it | Defer it |
| Is this a security requirement? | Build it | Defer it |
| Is this "nice to have" for v1? | Defer it | — |
| Are we building for 10K users when we have 10? | Stop | — |
| Are we abstracting something used in only one place? | Stop | — |

## 2. What MVP Means (and Doesn't)

### MVP IS
- The **smallest thing that delivers user value** and validates assumptions
- A working vertical slice: one feature, end-to-end (UI → API → DB)
- Hardcoded config instead of admin panels
- Direct service calls instead of message queues
- One database instead of microservices
- Manual processes instead of automation (if rare)

### MVP IS NOT
- A buggy mess with no error handling
- Skipping authentication or input validation
- Technical debt you can't pay back (no tests at all, no separation of concerns)
- A throwaway prototype (MVP code should be improvable, not disposable)

## 3. Build Order for Any Feature

```
1. Domain model (entity + value objects)     — 30 min, not 3 hours
2. Simplest data access (EF Core, direct)    — Repository interface + implementation
3. One happy-path API endpoint               — MediatR command/query
4. Basic UI that calls it                    — Blazor page with form
5. Basic validation                          — FluentValidation on the command
6. Basic error handling                      — Try-catch at handler level
7. One integration test                      — WebApplicationFactory happy path
═══════════════════════════════════════════
   ✅ SHIP IT — everything below is v1.1+
═══════════════════════════════════════════
8. Edge case handling
9. Comprehensive test coverage
10. Performance optimization
11. Advanced UI polish
12. Caching layer
13. Background jobs / queues
14. Admin dashboards
```

## 4. Anti-Over-Engineering Rules

### MUST NOT in MVP Phase

- ❌ **Generic repositories** — Use specific repositories per aggregate. Don't build `IRepository<T>` until you have 5+ entities with identical patterns.
- ❌ **CQRS read models** — Use the same EF model for reads and writes until query performance proves otherwise.
- ❌ **Event sourcing** — Use simple database updates. Event sourcing is v2+ complexity.
- ❌ **Microservices** — Start as a modular monolith. Extract services only when you have a proven scaling bottleneck.
- ❌ **Message queues** — Use direct method calls. Add MediatR notifications for in-process events. Queues come when you need cross-service communication.
- ❌ **Custom middleware** — Use built-in ASP.NET middleware. Write custom only when built-in can't solve the problem.
- ❌ **Abstract factories** — Inject services directly. Factory pattern when you have 3+ implementations to choose from at runtime.
- ❌ **Specification pattern** — Use LINQ Where clauses. Specifications when you have 5+ reusable query filters.
- ❌ **Custom result types** — Use `IActionResult` or simple exceptions. Result<T> when error handling becomes a pattern.
- ❌ **GraphQL** — Use REST. GraphQL when you have 10+ clients with different data needs.

### MUST DO in MVP Phase

- ✅ **Clean Architecture layers** — Separation of concerns is free and prevents rewrites.
- ✅ **Interfaces for external services** — `IEscrowPaymentService` so you can swap Stripe for PayPal later.
- ✅ **Input validation** — FluentValidation on every command. Non-negotiable.
- ✅ **Authentication & authorization** — `[Authorize]` on every endpoint. Default deny.
- ✅ **One happy-path test per feature** — Minimum viable test coverage.
- ✅ **Code-behind pattern** — `.razor` + `.razor.cs` from day one. Costs nothing, prevents technical debt.
- ✅ **Parameterized queries** — Never concatenate SQL. Ever.
- ✅ **Structured logging** — `ILogger<T>` with structured parameters. Costs nothing, saves debugging time.
- ✅ **Dependency injection** — Always. No `new SomeService()` in business logic.

## 5. The "Rule of Three" for Abstraction

> Don't abstract until you've written the same pattern three times.

- **1st time:** Write it inline. Ship it.
- **2nd time:** Note the duplication. Ship it.
- **3rd time:** Now extract a shared abstraction. You have 3 real examples to design from.

This prevents building abstractions for hypothetical futures that never arrive.

## 6. Time-Boxing Decisions

| Decision | Max Time | Default If Stuck |
|----------|----------|------------------|
| Database choice | 15 min | PostgreSQL |
| Auth provider | 15 min | ASP.NET Identity (upgrade to Entra ID later) |
| CSS framework | 10 min | Bootstrap (enterprise) or Tailwind (consumer) |
| Architecture pattern | 10 min | Clean Architecture + MediatR |
| ORM | 5 min | EF Core |
| Testing framework | 5 min | xUnit + FluentAssertions |
| State management | 10 min | Scoped services (Blazor Server) |
| API style | 5 min | Minimal APIs |
| Logging | 5 min | Serilog + structured logging |
| Caching | Skip | Add when you measure a performance problem |

## 7. Definition of "Done" for MVP Features

A feature is MVP-done when:

1. ✅ Happy path works end-to-end (UI → API → DB → response)
2. ✅ Input validation prevents obviously bad data
3. ✅ Authentication required (no anonymous access to business features)
4. ✅ Basic error handling (user sees a friendly message, not a stack trace)
5. ✅ One integration test covers the happy path
6. ✅ No hardcoded secrets or connection strings
7. ✅ Code compiles with zero warnings

A feature is NOT MVP-done if:
- It only works in Swagger but has no UI
- It handles the happy path but crashes on empty input
- It works but bypasses authentication

## 8. Iteration Cadence

```
Sprint 0:  Project scaffold, auth, first entity, CI pipeline
Sprint 1:  Core feature #1 end-to-end (e.g., Create Escrow Hold)
Sprint 2:  Core feature #2 end-to-end (e.g., Release Funds)
Sprint 3:  Core feature #3 + user feedback integration
Sprint 4:  Polish, edge cases, error handling improvements
Sprint 5:  Performance baseline, monitoring, production readiness
═══════════════════════════════════════════════════════════════
           ✅ MVP RELEASE
═══════════════════════════════════════════════════════════════
Sprint 6+: Iterate based on real user feedback, not assumptions
```

## 9. When to Break These Rules

These MVP rules have intentional escape hatches:

- **Compliance requirements** — If regulations mandate it (PCI-DSS, SOC2), build it regardless of MVP scope.
- **Data integrity** — If getting it wrong means data loss or corruption, invest the time.
- **Security** — Never cut corners on auth, input validation, or secret management.
- **Irreversible decisions** — Database schema choices that are painful to change deserve more thought.

## 10. Red Flags You're Over-Engineering

Stop and reassess if you catch yourself:

- Building an admin panel before you have users
- Writing a "plugin system" for a feature with one implementation
- Debating architectural patterns for more than 30 minutes
- Creating more interfaces than concrete classes
- Writing unit tests for trivial getters/setters
- Building a caching layer without measuring response times first
- Designing for "what if we need to scale to millions" on day one
- Spending more time on infrastructure than features
- Creating a NuGet package for code used in one project
