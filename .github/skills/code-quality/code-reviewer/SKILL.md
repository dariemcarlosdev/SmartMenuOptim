---
name: code-reviewer
description: "Systematic code review covering SOLID, Clean Code, security, performance, and testability — triggered by 'review code', 'check quality', 'PR review'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: review code, check quality, PR review, code review, review PR, review changes, quality check, review my code
  role: reviewer
  scope: review
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: refactor-planner, code-documenter, owasp-audit
---

# Code Reviewer

A systematic, multi-dimensional code review skill that evaluates source code against SOLID principles, Clean Code standards, security (OWASP-aware), performance, and testability — producing a severity-rated report with actionable recommendations.

## When to Use This Skill

- "Review this code" or "Review my PR"
- "Check code quality" or "Quality check"
- "What's wrong with this code?"
- Before merging a pull request
- After completing a feature for self-review
- Periodic codebase health checks

## Core Workflow

1. **Understand Context** — Identify scope (file, PR, module), language/framework, architecture style. Read `.editorconfig`, linter configs, `AGENTS.md`.
   - **Checkpoint:** Confirm review scope and architecture paradigm before proceeding.

2. **Architecture & SOLID Review** — Verify Clean Architecture boundaries (deps point inward), DI usage, SOLID compliance per class/method. Load `references/review-checklist.md` for full criteria.
   - **Checkpoint:** All architectural violations identified and categorized.

3. **Clean Code & Common Issues** — Evaluate method length (≤20 lines), nesting depth (≤2), naming, magic values, dead code, duplication. Load `references/common-issues.md` for detection patterns.
   - **Checkpoint:** All code smells cataloged with line numbers.

4. **Security, Performance & Testability** — Scan for OWASP patterns (injection, broken access control, XSS), N+1 queries, missing async/CancellationToken, hidden dependencies. Load `references/dotnet-review.md` for .NET-specific checks.
   - **Checkpoint:** All findings rated by severity before report generation.

5. **Generate Report** — Compile findings into structured report with severity ratings, positive observations, and prioritized recommendations. Load `references/feedback-examples.md` for tone guidance.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Review Checklist | `references/review-checklist.md` | Starting any review |
| Common Issues | `references/common-issues.md` | N+1 queries, magic numbers, dead code |
| Feedback Examples | `references/feedback-examples.md` | Writing constructive feedback |
| .NET Review Patterns | `references/dotnet-review.md` | Reviewing C#/.NET code |

## Quick Reference

```csharp
// Severity: High — SRP violation
public class OrderService  // Handles validation + persistence + notification
{
    public async Task ProcessOrderAsync(Order order) { /* 85 lines */ }
}
// Recommendation: Extract OrderValidator, IOrderRepository, OrderNotificationService
```

| Severity | Definition | Action |
|----------|-----------|--------|
| **Critical** | Security vulnerability, data loss, production crash | Must fix before merge |
| **High** | Major bug, SOLID violation, performance issue | Should fix before merge |
| **Medium** | Code smell, minor violation, maintainability | Fix in current sprint |
| **Low** | Style improvement, optimization, nice-to-have | Consider for future |

## Constraints

### MUST DO
- Review ALL files in scope — do not skip files
- Provide specific file paths and line numbers for every finding
- Rate every finding: Critical, High, Medium, or Low
- Provide concrete, actionable recommendations
- Include positive observations — note what is done well
- Group repeated patterns — do not report the same issue multiple times

### MUST NOT
- Do not rewrite the code — provide recommendations and examples only
- Do not flag issues suppressed by `#pragma` or `SuppressMessage`
- Do not produce false positives — only flag genuine issues with justification
- Do not suggest over-engineering for trivial code
- Do not contradict the project's established conventions

## Output Template

```markdown
# Code Review Report

**Scope:** [Files/PR reviewed]  |  **Date:** YYYY-MM-DD  |  **Reviewer:** AI Code Reviewer

## Summary
- **Files reviewed:** N | **Total findings:** N
- **Critical:** N | **High:** N | **Medium:** N | **Low:** N

## Findings
| # | Severity | Category | File | Line(s) | Finding | Recommendation |
|---|----------|----------|------|---------|---------|----------------|

## Positive Observations
## Architecture Compliance
## Recommendations Priority (top 3)
```
