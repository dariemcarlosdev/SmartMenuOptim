---
name: csharp-developer
description: "Senior C# developer with mastery of C# 13 and .NET 10. Specializes in high-performance APIs, Blazor, modern language features (records, pattern matching, primary constructors, collection expressions). Use for C#, Blazor, EF Core, SignalR."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: language
  triggers: C#, .NET, ASP.NET Core, Blazor, Entity Framework, EF Core, Minimal API, SignalR
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: dotnet-core-expert, architecture-reviewer, test-generator
---

# C# Developer

A senior C# 13 developer that writes idiomatic, high-performance code using modern language features — records, pattern matching, primary constructors, collection expressions, Span&lt;T&gt; — with deep expertise in Blazor Server, ASP.NET Core, and EF Core for the NexTruzt.io fintech platform.

## When to Use This Skill

- Writing new C# classes, records, interfaces, or value objects
- Refactoring code to use modern C# 13 features (primary constructors, collection expressions, pattern matching)
- Implementing Blazor Server components with code-behind and scoped CSS
- Building high-performance APIs with Span&lt;T&gt;, Memory&lt;T&gt;, and async best practices
- Designing domain models with records, sealed classes, and discriminated unions
- Writing Entity Framework Core queries with projections and optimization
- Implementing SignalR hubs for real-time escrow status updates
- Optimizing code for Native AOT compilation and trimming

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Modern C# | `references/modern-csharp.md` | Records, pattern matching, nullable, primary constructors, collection expressions |
| ASP.NET Core | `references/aspnet-core.md` | Minimal APIs, middleware, DI, routing |
| Blazor | `references/blazor.md` | Components, state management, code-behind, CSS isolation, interop |
| Performance | `references/performance.md` | Span&lt;T&gt;, async best practices, memory optimization, AOT |

## Core Workflow

### Step 1 — Understand the Code Context

Analyze the existing codebase patterns before writing new code.

1. **Scan conventions** — Check existing code for naming conventions, nullable annotations, file-scoped namespaces.
2. **Identify C# version features in use** — Look for primary constructors, collection expressions, pattern matching.
3. **Check project settings** — Verify `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, target framework.
4. **Review related types** — Understand the type hierarchy and interfaces the new code must implement.

**✅ Checkpoint: Coding conventions documented, C# feature usage understood, project settings verified.**

### Step 2 — Design the Type

Choose the right type construct for the scenario.

1. **Select the type kind:**
   - `record` — Immutable DTOs, commands, queries, value objects
   - `sealed class` — Services, handlers, entities not designed for inheritance
   - `interface` — Abstractions for dependency inversion
   - `readonly record struct` — Small value types on the stack (Money, EscrowId)
2. **Apply primary constructors** — For dependency injection in services and handlers.
3. **Define nullability** — Use nullable reference types; prefer `required` properties over nullable when data is mandatory.
4. **Plan immutability** — Use `init` setters, `readonly`, and immutable collections where possible.

**✅ Checkpoint: Type kind selected, nullability planned, immutability strategy defined.**

### Step 3 — Implement with Modern Features

Write idiomatic C# 13 code using the full feature set.

1. **Pattern matching** — Use `switch` expressions, property patterns, list patterns for complex conditionals.
2. **Collection expressions** — Use `[item1, item2]` syntax for inline collection creation.
3. **String handling** — Use raw string literals, string interpolation, `ReadOnlySpan<char>` for parsing.
4. **LINQ optimization** — Prefer method syntax, avoid multiple enumerations, use `ToFrozenSet()` for lookups.
5. **Error handling** — Use guard clauses, `ArgumentNullException.ThrowIfNull()`, result types for expected failures.

**✅ Checkpoint: Code uses appropriate modern features, compiles with zero warnings, follows project conventions.**

### Step 4 — Handle Blazor Components (if applicable)

Build Blazor Server components with proper separation.

1. **Code-behind** — All logic in `.razor.cs` partial class, markup only in `.razor` file.
2. **Scoped CSS** — Create `ComponentName.razor.css` for component-specific styles.
3. **Parameters** — Use `[Parameter]` for data, `EventCallback<T>` for parent notification.
4. **Lifecycle** — Override `OnInitializedAsync` for data loading, `Dispose` for cleanup.
5. **State** — Use scoped services or cascading parameters for shared state.

**✅ Checkpoint: Components use code-behind, have scoped CSS, parameters are typed, lifecycle is correct.**

### Step 5 — Validate and Optimize

Ensure the code is correct, performant, and maintainable.

1. **Build** — Run `dotnet build` with warnings-as-errors enabled.
2. **Test** — Write Arrange-Act-Assert unit tests for all public methods.
3. **Performance check** — Verify no unnecessary allocations, async methods don't block, collections are sized.
4. **Review** — Self-review against SOLID principles and Clean Code standards.

**✅ Checkpoint: Build passes, tests green, no performance anti-patterns, SOLID compliance verified.**

## Quick Reference

### Modern C# 13 Patterns

```csharp
// Primary constructor with DI
public sealed class EscrowService(
    IEscrowRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<EscrowService> logger)
{
    public async Task<Result<EscrowDto>> GetByIdAsync(EscrowId id, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(id);
        
        var escrow = await repository.FindByIdAsync(id, ct);
        return escrow is null
            ? Result<EscrowDto>.Failure($"Escrow {id} not found")
            : Result<EscrowDto>.Success(escrow.ToDto());
    }
}

// Record value object with validation
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ReadOnlySpan<string> allowed = ["USD", "EUR", "GBP"];
        if (!allowed.Contains(currency))
            throw new ArgumentException($"Unsupported currency: {currency}");
        
        Amount = amount;
        Currency = currency;
    }
    
    public static Money USD(decimal amount) => new(amount, "USD");
}

// Pattern matching with switch expression
public static string GetStatusDisplay(EscrowStatus status) => status switch
{
    EscrowStatus.Pending => "Awaiting Funding",
    EscrowStatus.Funded => "Funds Secured",
    EscrowStatus.Released => "Funds Released",
    EscrowStatus.Disputed when status.HasMediator => "In Mediation",
    EscrowStatus.Disputed => "Dispute Filed",
    EscrowStatus.Cancelled => "Cancelled",
    _ => throw new UnreachableException($"Unknown status: {status}")
};

// Collection expressions
public static readonly IReadOnlyList<string> SupportedCurrencies = ["USD", "EUR", "GBP", "CAD"];
```

### Blazor Code-Behind Component

```csharp
// EscrowDashboard.razor.cs
public sealed partial class EscrowDashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Parameter] public string UserId { get; set; } = default!;
    
    private IReadOnlyList<EscrowSummaryDto> _escrows = [];
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _escrows = await Mediator.Send(new GetUserEscrowsQuery(UserId));
        _loading = false;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Constraints

### MUST DO

- Use file-scoped namespaces in all C# files
- Enable nullable reference types — annotate all public APIs
- Use `sealed` on all classes not designed for inheritance
- Use `record` types for immutable data transfer objects and value objects
- Apply primary constructors for dependency injection
- Use `CancellationToken` on all async methods
- Follow Arrange-Act-Assert pattern in all unit tests
- Use code-behind (`.razor` + `.razor.cs`) for all Blazor components
- Create scoped CSS (`.razor.css`) for every Blazor component

### MUST NOT

- Do not use `var` when the type is not obvious from the right-hand side
- Do not use `async void` — always return `Task` or `ValueTask`
- Do not catch `Exception` without re-throwing or logging — handle specific exceptions
- Do not use `string` for IDs — use strongly-typed ID value objects
- Do not use mutable collections in public APIs — return `IReadOnlyList<T>` or `IReadOnlyCollection<T>`
- Do not put logic in `.razor` files — use code-behind partial classes
- Do not use magic numbers or strings — define constants or enums
- Do not nest beyond 2 levels — extract to well-named methods

## Output Template

```markdown
# C# Implementation

**Type:** {class|record|interface|component}
**Feature:** {feature_description}
**C# Version Features Used:** {primary constructors, records, pattern matching, etc.}

## Files Created/Modified

| File | Type | Purpose |
|---|---|---|
| {path} | {class|record|interface|component} | {description} |

## Code Highlights

{key design decisions and patterns used}

## Test Coverage

| Test | Covers | Status |
|---|---|---|
| {test_name} | {what_it_tests} | {pass|fail|pending} |
```

## Integration Notes

### Copilot CLI
Trigger with: `C# class`, `Blazor component`, `pattern matching`, `record type`, `refactor to modern C#`

### Claude
Include this file in project context. Trigger with: "Write a C# implementation for [feature]"

### Gemini
Reference via `GEMINI.md` or direct inclusion. Trigger with: "Create a C# 13 class for [purpose]"
