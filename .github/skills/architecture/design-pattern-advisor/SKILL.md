---
name: design-pattern-advisor
description: "Suggest and guide design pattern application for code smells and architectural problems — trigger: suggest pattern, which pattern, refactor with pattern"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: architecture
  triggers: suggest pattern, which pattern, refactor with pattern, design pattern, code smell, over-engineering check
  role: specialist
  scope: design
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: architecture-reviewer, dependency-analyzer
---

# Design Pattern Advisor

Recognize problems that benefit from design patterns, evaluate trade-offs, and guide implementation with concrete C#/.NET examples.

## When to Use This Skill

- When you encounter a code smell (large conditionals, tight coupling, duplicated logic)
- When choosing between competing patterns (Strategy vs. State, Factory vs. Builder)
- When introducing CQRS, Repository, or other enterprise patterns
- When reviewing code that uses a pattern incorrectly or unnecessarily
- Before a refactoring effort to decide on the target design

## Core Workflow

1. **Analyze the Problem** — Identify the pain point: duplication, rigidity, fragility, or viscosity. Determine if structural or behavioral.
   - ✅ Checkpoint: Problem clearly articulated with code smell identified

2. **Identify Candidates** — Match problem to 2–3 candidate patterns from the appropriate category → See reference files by category
   - ✅ Checkpoint: At least 2 candidates listed with rationale

3. **Evaluate Trade-Offs** — Assess complexity cost, team familiarity, YAGNI check, testability, and performance
   - ✅ Checkpoint: YAGNI assessment completed (✅ Justified / ⚠️ Borderline / ❌ Over-engineering)

4. **Recommend Best Fit** — Select pattern with least accidental complexity; always include "no pattern" option
   - ✅ Checkpoint: Recommendation justified against alternatives

5. **Provide Implementation** — Show concrete skeleton with DI wiring and integration steps

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Creational Patterns | `references/creational-patterns.md` | Factory, Builder, Singleton decisions |
| Structural Patterns | `references/structural-patterns.md` | Adapter, Decorator, Facade, Proxy |
| Behavioral Patterns | `references/behavioral-patterns.md` | Strategy, Observer, Mediator, Command |
| Enterprise Patterns | `references/enterprise-patterns.md` | Repository, UoW, CQRS, Event Sourcing |

## Quick Reference

```csharp
// Strategy Pattern — replacing payment method conditionals
public interface IPaymentStrategy
{
    Task<PaymentResult> ProcessAsync(Money amount, CancellationToken ct);
}

// DI Registration
services.AddKeyedScoped<IPaymentStrategy, StripePaymentStrategy>("stripe");
services.AddKeyedScoped<IPaymentStrategy, PayPalPaymentStrategy>("paypal");
```

```csharp
// Decorator Pattern — adding retry to any repository
public sealed class RetryEscrowRepository(
    IEscrowRepository inner, ILogger<RetryEscrowRepository> logger)
    : IEscrowRepository
{
    public async Task<EscrowTransaction?> GetByIdAsync(EscrowTransactionId id, CancellationToken ct)
    {
        // Polly retry wrapping the inner call
        return await Policy.Handle<DbException>()
            .RetryAsync(3)
            .ExecuteAsync(() => inner.GetByIdAsync(id, ct));
    }
}
```

## Constraints

### MUST DO
- Evaluate at least 2 candidate patterns before recommending one
- Include a YAGNI assessment — patterns must earn their complexity
- Provide code examples in C#/.NET matching the target project
- Explain the pattern's intent in plain language before showing code
- Show DI wiring when applicable

### MUST NOT
- Recommend a pattern without explaining the problem it solves
- Introduce a pattern for fewer than 3 variations — use simple conditionals instead
- Recommend Visitor, Interpreter, or Abstract Factory without strong justification
- Ignore the "no pattern" alternative
- Combine multiple patterns in one recommendation unless clearly required

## Output Template

```markdown
# Design Pattern Recommendation

**Problem:** {one-sentence} | **Context:** {language, framework}

## Problem Analysis
{Pain point, code smell, what a good solution achieves}

## Candidates
### Option A: {Pattern} — Fit: {why} | Cost: {N new types}
### Option B: {Pattern} — Fit: {why} | Cost: {N new types}
### Option C: No Pattern — {when simplicity wins}

## Recommendation: {Pattern}
**YAGNI:** {✅|⚠️|❌} | **Rationale:** {justification}

## Implementation
{Code skeleton + DI registration + integration steps}
```
