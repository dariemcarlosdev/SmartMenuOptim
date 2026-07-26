---
name: tdd-coach
description: "Guide the Red-Green-Refactor TDD cycle with iterative test-first development — trigger: TDD, red green refactor, test first, test driven"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: testing
  triggers: TDD, red green refactor, test first, test driven, write test first, failing test
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: test-generator, test-coverage-analyzer
---

# TDD Coach

Guide developers through the Red-Green-Refactor cycle with disciplined test-first development, enforcing the TDD rhythm for .NET/C# projects.

## When to Use This Skill

- When implementing a new feature from scratch and want to drive design from tests
- When fixing a bug — write a failing test that reproduces it before writing the fix
- When refactoring — ensure test coverage exists before changing code
- When implementing business logic with complex rules
- When learning TDD — use as a coach to maintain cycle discipline

## Core Workflow

1. **Break Feature into Increments** — Decompose into small, testable baby steps ordered simplest → complex → See `references/test-first-design.md`
   - ✅ Checkpoint: Increment list ordered from degenerate case to edge cases

2. **🔴 RED — Write ONE Failing Test** — Express next behavior as a single test; confirm it fails → See `references/red-green-refactor.md`
   - ✅ Checkpoint: Test fails with expected assertion error (not compilation error)

3. **🟢 GREEN — Minimal Code to Pass** — Write the absolute minimum production code; all tests pass
   - ✅ Checkpoint: New test + all previous tests green

4. **🔵 REFACTOR — Clean Without Behavior Change** — Remove duplication, improve names, extract methods; all tests still pass → Watch for anti-patterns in `references/tdd-anti-patterns.md`
   - ✅ Checkpoint: All tests green after each refactoring step

5. **Repeat** — Pick next increment, return to RED. Each cycle: 2–10 minutes.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Red-Green-Refactor | `references/red-green-refactor.md` | TDD cycle walkthrough |
| Test-First Design | `references/test-first-design.md` | Tests driving design decisions |
| TDD Anti-Patterns | `references/tdd-anti-patterns.md` | Common TDD mistakes |
| Kata Exercises | `references/kata-exercises.md` | TDD practice exercises |

## Quick Reference — TDD Cycle

```
  ┌──────────┐     ┌──────────┐     ┌──────────┐
  │  🔴 RED   │────▶│ 🟢 GREEN │────▶│ 🔵 REFACTOR│
  │  Failing  │     │ Make it  │     │ Clean up │
  │  test     │     │ pass     │     │          │
  └──────────┘     └──────────┘     └─────┬────┘
       ▲                                   │
       └───────────────────────────────────┘
```

```csharp
// 🔴 RED — Test first
[Fact]
public void CalculateFee_WhenStandardEscrow_ShouldReturn2Point5Percent()
{
    var calculator = new FeeCalculator();
    var fee = calculator.Calculate(EscrowType.Standard, Money.From(1000m));
    fee.Should().Be(Money.From(25m));
}

// 🟢 GREEN — Minimal implementation
public Money Calculate(EscrowType type, Money amount)
    => Money.From(amount.Value * 0.025m); // Hardcoded — we'll generalize later

// 🔵 REFACTOR — (next cycle will drive generalization)
```

## Constraints

### MUST DO
- Always write the test BEFORE production code — no exceptions
- Run the test and confirm it FAILS before writing production code
- Write the MINIMAL code to pass — no speculative generality
- Run ALL tests after each GREEN and REFACTOR step
- Keep each cycle small — 2 to 10 minutes
- Name tests as behavior specs, not implementation descriptions
- Label each phase: `🔴 RED`, `🟢 GREEN`, `🔵 REFACTOR`

### MUST NOT
- Write production code without a failing test demanding it
- Write multiple tests at once — one test per RED phase
- Skip REFACTOR step repeatedly — debt accumulates
- Test implementation details — test observable behavior
- Jump to complex cases before handling simple ones
- Refactor while a test is failing — get to GREEN first

## Output Template

```markdown
# TDD Session: {Feature}
**Goal:** {what} | **Framework:** xUnit + FluentAssertions

## Increment Plan
1. {Degenerate case} → 2. {Simple case} → 3. {Complex} → 4. {Edge cases}

## Cycle 1: {Behavior}
### 🔴 RED
{failing test code} — **Expected failure:** {message}
### 🟢 GREEN
{minimal code} — **All tests:** ✅ (N passed)
### 🔵 REFACTOR
{changes or "No refactoring needed"}

## Final State
{Complete production code + complete test suite}
```
