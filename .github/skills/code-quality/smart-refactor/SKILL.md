---
name: smart-refactor
description: "Metrics-driven refactoring with baseline/after comparison — measure complexity reduction scientifically"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: measure refactor, refactor with metrics, complexity reduction, before after refactor, scientific refactor, quantify improvement, refactor and measure
  role: advisor
  scope: refactor
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: quality-analyzer, refactor-planner, code-reviewer
---

# Smart Refactor

A metrics-driven refactoring skill that applies Fowler's refactoring catalog with quantitative before/after measurement. Unlike `refactor-planner` (which focuses on planning and dependency analysis), this skill focuses on execution and scientific measurement — proving that each refactoring step reduces complexity. Uses native .NET tooling for baseline capture and delta comparison.

## When to Use This Skill

- "Refactor this and show me the improvement"
- "Reduce complexity with metrics" or "Measure the refactoring"
- "Before/after comparison of this refactoring"
- "Quantify the improvement" or "Scientific refactor"
- When justifying refactoring effort to stakeholders with data
- After `refactor-planner` produces a plan and you need measured execution

## Core Workflow

1. **Capture Baseline Metrics** — Before any changes, measure the target: cyclomatic complexity (count `if/else/switch/for/while/catch` + 1 per method), cognitive complexity (add nesting penalties), method count, line count, SATD count. Run `dotnet build --no-restore` to confirm green baseline. Store metrics for comparison.
   - **Checkpoint:** Baseline metrics captured and build passes.

```powershell
# Baseline capture for a target file
$file = "path/to/Target.cs"
$cc = (Select-String -Pattern '\b(if|else if|switch|case|for|foreach|while|do|catch)\b' -Path $file).Count
$loc = (Get-Content $file | Measure-Object -Line).Lines
$methods = (Select-String -Pattern '(public|private|protected|internal)\s+(static\s+)?(async\s+)?\w+[\w<>\[\],\s]*\s+\w+\s*\(' -Path $file).Count
Write-Host "CC: $($cc+$methods), LOC: $loc, Methods: $methods"
```

2. **Select Refactoring Technique** — Match code smell to Fowler's catalog technique. Load `references/refactoring-catalog.md` for the full C#-adapted catalog. Common mappings:
   - Long Method → Extract Method, Decompose Conditional
   - Complex Conditional → Replace Conditional with Polymorphism, Guard Clauses
   - Large Class → Extract Class, Extract Interface
   - Feature Envy → Move Method, Inline Class
   - **Checkpoint:** Technique selected with expected complexity reduction estimate.

3. **Apply Refactoring with Guard Rails** — Execute the refactoring in atomic steps. After each step: `dotnet build --no-restore` must pass. If tests exist: `dotnet test --no-build` must pass. Load `references/safety-checklist.md` for pre/post verification steps. Never change behavior — only structure.
   - **Checkpoint:** Build and tests green after each atomic step.

4. **Capture After Metrics** — Re-measure the same metrics from Step 1 on the refactored code. Compute deltas: ΔCC, ΔCogC, ΔLOC, ΔMethods. Load `references/complexity-reduction.md` for expected reduction ranges by technique.
   - **Checkpoint:** After metrics captured; deltas computed.

5. **Generate Comparison Report** — Produce a before/after scorecard with percentage improvements, technique applied, and quality gate verdict (PASS if all metrics improved or held neutral, FAIL if any metric regressed without justification).

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Refactoring Catalog | `references/refactoring-catalog.md` | Selecting technique for a code smell |
| Complexity Reduction | `references/complexity-reduction.md` | Estimating expected improvement |
| Safety Checklist | `references/safety-checklist.md` | Before/after each refactoring step |

## Quick Reference

| Technique | Typical CC Reduction | Typical CogC Reduction |
|-----------|---------------------|----------------------|
| Extract Method | −3 to −8 per extraction | −5 to −12 |
| Guard Clauses | −2 to −5 | −4 to −10 |
| Decompose Conditional | −4 to −10 | −8 to −15 |
| Replace Conditional w/ Polymorphism | −5 to −15 | −10 to −25 |
| Extract Class | −3 to −8 (original) | −5 to −15 (original) |

```csharp
// BEFORE: CC=6, CogC=9
public decimal Calculate(Order order)
{
    if (order == null) throw new ArgumentNullException(nameof(order));
    decimal total = 0;
    foreach (var item in order.Items)
    {
        if (item.IsDiscounted)
            total += item.Price * 0.9m;
        else if (item.IsBulk && item.Quantity > 10)
            total += item.Price * item.Quantity * 0.85m;
        else
            total += item.Price * item.Quantity;
    }
    return total;
}

// AFTER: CC=2, CogC=2 (main method); logic distributed to strategies
public decimal Calculate(Order order)
{
    ArgumentNullException.ThrowIfNull(order);
    return order.Items.Sum(item => _pricingStrategy.CalculateItemTotal(item));
}
```

## Constraints

### MUST DO
- Capture baseline metrics BEFORE any code change
- Run `dotnet build` after every atomic refactoring step
- Report exact before/after numbers — no qualitative-only assessments
- Identify which Fowler technique was applied for each change
- Verify behavior preservation: tests must pass, or explain why no tests exist

### MUST NOT
- Do not change behavior during refactoring — structure only
- Do not skip the baseline capture step
- Do not report improvement without measurement
- Do not apply multiple techniques simultaneously — one per atomic step
- Do not claim improvement if metrics regress without clear justification

## Output Template

```markdown
# Smart Refactor Report

**Target:** [File/Class]  |  **Date:** YYYY-MM-DD

## Before/After Scorecard
| Metric | Before | After | Delta | % Change |
|--------|--------|-------|-------|----------|
| Cyclomatic Complexity | N | N | −N | −N% |
| Cognitive Complexity | N | N | −N | −N% |
| Lines of Code | N | N | ±N | ±N% |
| Method Count | N | N | ±N | ±N% |
| SATD Annotations | N | N | −N | −N% |

## Techniques Applied
| # | Technique | Target | CC Δ | CogC Δ |
|---|-----------|--------|------|--------|

## Quality Gate: ✅ PASS / ❌ FAIL
## Verification: [ ] Build passes  [ ] Tests pass  [ ] No behavior change
```
