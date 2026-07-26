# Review Checklist

Comprehensive checklist to follow when starting any code review. Work through each section sequentially.

## Phase 1 — Context Gathering

- [ ] Identify review scope: single file, PR diff, module, or full codebase
- [ ] Determine language, framework, and architecture style
- [ ] Read `.editorconfig`, linter configs, `AGENTS.md`/`CLAUDE.md` for project conventions
- [ ] Understand the feature/change intent — what problem is the code solving?

## Phase 2 — Architecture & Layer Compliance

- [ ] Dependencies point inward: Presentation → Application → Domain
- [ ] No infrastructure concerns in domain or application layers
- [ ] No direct database access from presentation layer
- [ ] Controllers/pages only orchestrate — no business logic
- [ ] Services don't depend on HTTP context or UI concerns
- [ ] Domain entities don't reference infrastructure types
- [ ] DI lifetimes correct (Scoped, Transient, Singleton)
- [ ] Abstractions injected, not concrete types

## Phase 3 — SOLID Principles

### SRP (Single Responsibility)
- [ ] Each class has one reason to change
- [ ] Each method does one thing
- [ ] No mixed concerns (validation + persistence + notification in one method)

### OCP (Open/Closed)
- [ ] Behavior extensible without modifying existing code
- [ ] Switch/if-else chains evaluated for polymorphism/strategy replacement

### LSP (Liskov Substitution)
- [ ] Derived types substitute base types without breaking behavior
- [ ] No type checks (`is`, `as`, `typeof`) that violate LSP

### ISP (Interface Segregation)
- [ ] Interfaces focused and cohesive
- [ ] No empty or throwing implementations for unused members

### DIP (Dependency Inversion)
- [ ] High-level code depends on abstractions
- [ ] Constructor injection, not service locator pattern

## Phase 4 — Clean Code Metrics

| Metric | Threshold | Severity |
|--------|-----------|----------|
| Method length | ≤ 20 lines preferred, ≤ 30 acceptable | Medium |
| Nesting depth | ≤ 2 levels | Medium |
| Parameter count | ≤ 3 preferred, ≤ 5 acceptable | Low |
| Class length | ≤ 200 lines preferred | Low |
| Cyclomatic complexity | ≤ 10 per method | Medium |

## Phase 5 — Security Scan (OWASP-Aware)

- [ ] Injection: No string concatenation in SQL/commands
- [ ] Access Control: Every endpoint has `[Authorize]` or justified `[AllowAnonymous]`
- [ ] Data Exposure: No plaintext secrets, no PII in logs
- [ ] XSS: No `@Html.Raw()` with user data
- [ ] CSRF: Antiforgery tokens on state-changing operations
- [ ] Mass Assignment: DTOs used, not direct entity binding
- [ ] Deserialization: No `TypeNameHandling.All` or `BinaryFormatter`

## Phase 6 — Performance

- [ ] No N+1 queries (use `Include()` or batch loading)
- [ ] No blocking async (`.Result`, `.Wait()`)
- [ ] `CancellationToken` propagated through async chains
- [ ] Queries bounded with pagination or `Take()`
- [ ] `AsNoTracking()` on read-only EF Core queries
- [ ] `Any()` instead of `Count() > 0`

## Phase 7 — Error Handling & Testability

- [ ] Exceptions caught at appropriate layer
- [ ] Error messages don't leak internal details
- [ ] Nullable reference types used properly
- [ ] External calls wrapped with Polly (retry/circuit breaker)
- [ ] `IDisposable`/`IAsyncDisposable` properly disposed
- [ ] Classes testable in isolation (injectable dependencies)
- [ ] No hidden dependencies (`DateTime.Now`, static methods, `new` on services)
