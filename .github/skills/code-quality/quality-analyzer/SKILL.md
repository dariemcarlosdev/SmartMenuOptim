---
name: quality-analyzer
description: "Analyze code quality metrics — cyclomatic complexity, cognitive complexity, maintainability index, SATD annotations, and style conformance"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: analyze quality, code metrics, complexity analysis, check complexity, maintainability, code health, quality report, measure quality
  role: analyzer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: code-reviewer, smart-refactor, tech-debt-tracker
---

# Quality Analyzer

A metrics-driven code quality analysis skill that measures cyclomatic complexity, cognitive complexity, maintainability index, Self-Admitted Technical Debt (SATD), and style conformance — using native .NET tooling and heuristic analysis. Based on McCabe (1976) cyclomatic complexity, SonarSource cognitive complexity model, and Visual Studio maintainability index formula.

## When to Use This Skill

- "Analyze code quality" or "Run quality metrics"
- "What's the complexity of this module?"
- "Check maintainability" or "Code health report"
- Before a release to assess codebase health
- After a sprint to measure quality trends
- When deciding which modules need refactoring priority

## Core Workflow

1. **Establish Scope & Baseline** — Identify target (file, project, solution). Run `dotnet build` to confirm compilability. Collect file inventory with `Get-ChildItem -Recurse -Include *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }`.
   - **Checkpoint:** Target compiles clean and file list is complete before analysis begins.

2. **Measure Cyclomatic Complexity** — For each method, count decision points using grep heuristics: `Select-String -Pattern '\b(if|else if|switch|case|for|foreach|while|do|catch|&&|\|\||[?]:)\b' -Path *.cs`. Add 1 for method entry. Flag methods exceeding threshold (>10 moderate, >20 high). Load `references/complexity-thresholds.md` for full McCabe scale.
   - **Checkpoint:** Every public method has a cyclomatic complexity score.

3. **Estimate Cognitive Complexity** — Extend cyclomatic count with nesting penalties: +1 per nesting level for control structures, +1 for breaks in linear flow (early return, continue, goto), +1 for recursion. Apply SonarSource cognitive complexity rules. Flag methods >15 cognitive complexity.
   - **Checkpoint:** Cognitive complexity scores computed; high-complexity methods identified.

4. **Detect SATD & Style Issues** — Scan for Self-Admitted Technical Debt: `Select-String -Pattern 'TODO|FIXME|HACK|XXX|UNDONE|WORKAROUND|KLUDGE' -Recurse -Include *.cs`. Run `dotnet format --verify-no-changes --verbosity diagnostic` for style violations. Load `references/dotnet-analyzers.md` for Roslyn analyzer configuration.
   - **Checkpoint:** SATD inventory and style violation count complete.

5. **Generate Quality Scorecard** — Compile metrics into report: per-method complexity, per-file maintainability index (171 − 5.2×ln(HV) − 0.23×CC − 16.2×ln(LOC)), SATD count by category, style violations. Load `references/quality-scorecard.md` for interpretation guidance.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Complexity Thresholds | `references/complexity-thresholds.md` | Interpreting McCabe and cognitive scores |
| .NET Analyzers | `references/dotnet-analyzers.md` | Configuring Roslyn analyzers |
| Quality Scorecard | `references/quality-scorecard.md` | Generating the final report |

## Quick Reference

```powershell
# Cyclomatic complexity estimation (per file)
Select-String -Pattern '\b(if|else\s+if|switch|case|for|foreach|while|do|catch)\b' -Path *.cs |
  Group-Object Path | Select-Object Count, Name | Sort-Object Count -Descending

# SATD detection
Select-String -Pattern 'TODO|FIXME|HACK|XXX|UNDONE' -Recurse -Include *.cs

# Style check
dotnet format --verify-no-changes --verbosity diagnostic
```

| Metric | 🟢 Good | 🟡 Moderate | 🔴 High Risk |
|--------|---------|-------------|--------------|
| Cyclomatic Complexity (per method) | 1–10 | 11–20 | >20 |
| Cognitive Complexity (per method) | 1–15 | 16–25 | >25 |
| Maintainability Index (per file) | 20–100 | 10–19 | <10 |
| SATD Annotations (per project) | 0–5 | 6–15 | >15 |
| Method Length (lines) | 1–20 | 21–40 | >40 |

## Constraints

### MUST DO
- Measure ALL public methods in scope — no sampling
- Report exact file paths and line numbers for every finding
- Classify every metric against the thresholds table
- Include trend direction if historical data is available
- Flag the top 5 highest-complexity methods as refactoring candidates

### MUST NOT
- Do not modify any source code — analysis only
- Do not count auto-generated code (`*.Designer.cs`, `*.g.cs`, `obj/`, `bin/`)
- Do not report complexity for trivial methods (getters, setters, ToString)
- Do not rely on external tools beyond `dotnet` CLI and PowerShell

## Output Template

```markdown
# Quality Analysis Report

**Scope:** [Target]  |  **Date:** YYYY-MM-DD  |  **Files Analyzed:** N

## Summary Dashboard
| Metric | Value | Rating |
|--------|-------|--------|
| Avg Cyclomatic Complexity | N | 🟢/🟡/🔴 |
| Max Cyclomatic Complexity | N (method) | 🟢/🟡/🔴 |
| Avg Cognitive Complexity | N | 🟢/🟡/🔴 |
| SATD Count | N | 🟢/🟡/🔴 |
| Style Violations | N | 🟢/🟡/🔴 |

## Top 5 High-Complexity Methods
| # | Method | File | CC | CogC | Recommendation |
|---|--------|------|-----|------|----------------|

## SATD Inventory
| # | Type | File | Line | Comment |
|---|------|------|------|---------|

## Recommendations (prioritized)
```
