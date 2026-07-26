---
name: polyglot-analyzer
description: "Multi-language quality comparison — language distribution, cross-language boundaries, unified quality gates for polyglot projects"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: architecture
  triggers: analyze languages, polyglot analysis, language distribution, cross-language quality, multi-language report, what languages do we use, language boundaries
  role: analyzer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: quality-analyzer, architecture-reviewer, dependency-analyzer
---

# Polyglot Analyzer

A multi-language quality analysis skill that maps language distribution, detects cross-language boundaries, applies unified quality thresholds per language, and generates a consolidated quality report. Designed for polyglot .NET projects that combine C#, TypeScript/JavaScript, SQL, YAML, HTML/CSS, and other languages. Uses native PowerShell and grep — no external tooling required.

## When to Use This Skill

- "What languages are in this project?" or "Language distribution"
- "Analyze quality across all languages"
- "Find cross-language boundaries" or "Where do languages interact?"
- "Unified quality report" or "Polyglot health check"
- When onboarding to a polyglot codebase
- Before establishing cross-team code quality standards

## Core Workflow

1. **Map Language Distribution** — Inventory all source files by extension, compute lines of code per language, calculate percentage distribution:
   ```powershell
   Get-ChildItem -Recurse -File | Where-Object { $_.FullName -notmatch '\\(node_modules|bin|obj|\.git)\\' } |
     Group-Object Extension | Sort-Object Count -Descending |
     Select-Object @{N='Extension';E={$_.Name}}, Count,
       @{N='LOC';E={($_.Group | ForEach-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines } | Measure-Object -Sum).Sum}}
   ```
   Load `references/language-thresholds.md` for per-language quality standards.
   - **Checkpoint:** Language inventory complete with file count and LOC per language.

2. **Detect Cross-Language Boundaries** — Find integration points between languages. Load `references/boundary-patterns.md` for pattern catalog:
   - **C# ↔ JavaScript/TypeScript**: `Select-String -Pattern 'IJSRuntime|IJSObjectReference|DotNetObjectReference|interop' -Recurse -Include *.cs`
   - **C# ↔ Native**: `Select-String -Pattern 'DllImport|LibraryImport|P/Invoke|extern' -Recurse -Include *.cs`
   - **C# ↔ SQL**: `Select-String -Pattern 'FromSqlRaw|ExecuteSqlRaw|SqlCommand|\.Query\(|\.Execute\(' -Recurse -Include *.cs`
   - **C# ↔ HTML/Razor**: Count `.razor` files with code-behind vs inline `@code`
   - **C# ↔ YAML/JSON**: `Select-String -Pattern 'IConfiguration|IOptions<|appsettings' -Recurse -Include *.cs`
   - **Checkpoint:** All cross-language boundaries cataloged with direction and file references.

3. **Apply Per-Language Quality Gates** — For each language found, run appropriate quality checks:
   - **C#**: Cyclomatic complexity heuristic, SATD scan, `dotnet format --verify-no-changes`
   - **TypeScript/JavaScript**: ESLint config check, `Select-String -Pattern 'any|// @ts-ignore' -Recurse -Include *.ts`
   - **SQL**: Scan for raw string queries, missing parameterization
   - **YAML/JSON**: Validate structure with `dotnet` or PowerShell parsers
   - **CSS/SCSS**: Scan for `!important`, deeply nested selectors
   - **Checkpoint:** Per-language quality scores computed.

4. **Assess Boundary Health** — Evaluate each cross-language boundary for: proper error handling, type safety across boundary, serialization correctness, resource cleanup (IDisposable on interop). Score each boundary 🟢/🟡/🔴.
   - **Checkpoint:** Boundary health scores assigned.

5. **Generate Polyglot Report** — Compile unified report with language distribution chart, per-language quality scores, boundary health matrix, and unified recommendations. Load `references/polyglot-report.md` for template.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Language Thresholds | `references/language-thresholds.md` | Applying quality gates per language |
| Boundary Patterns | `references/boundary-patterns.md` | Detecting cross-language integration |
| Polyglot Report | `references/polyglot-report.md` | Generating the consolidated report |

## Quick Reference

```powershell
# Language distribution (quick)
Get-ChildItem -Recurse -File |
  Where-Object { $_.FullName -notmatch '\\(node_modules|bin|obj|\.git)\\' } |
  Group-Object Extension | Sort-Object Count -Descending |
  Select-Object Name, Count | Format-Table -AutoSize

# JS/TS interop boundaries
Select-String -Pattern 'IJSRuntime|JSInvokable|interop' -Recurse -Include *.cs,*.razor

# Native interop (P/Invoke)
Select-String -Pattern 'DllImport|LibraryImport' -Recurse -Include *.cs

# Raw SQL detection (potential injection risk)
Select-String -Pattern 'FromSqlRaw|ExecuteSqlRaw|SqlCommand' -Recurse -Include *.cs

# TypeScript quality signals
Select-String -Pattern '\bany\b|// @ts-ignore|@ts-nocheck' -Recurse -Include *.ts,*.tsx
```

| Language | Quality Gate | 🟢 Good | 🟡 Caution | 🔴 Fail |
|----------|-------------|---------|-----------|---------|
| C# | Cyclomatic Complexity (avg) | <10 | 10–20 | >20 |
| C# | SATD per KLOC | <2 | 2–5 | >5 |
| TypeScript | `any` usage per KLOC | 0 | 1–3 | >3 |
| TypeScript | `@ts-ignore` count | 0 | 1–5 | >5 |
| SQL | Raw string queries | 0 | 1–3 | >3 |
| CSS | `!important` count | 0–2 | 3–10 | >10 |
| YAML/JSON | Schema validation errors | 0 | 1–3 | >3 |

| Boundary Type | Key Risks | Detection Pattern |
|---------------|-----------|-------------------|
| C# ↔ JS/TS | Memory leaks, serialization | `IJSRuntime`, `DotNetObjectReference` |
| C# ↔ Native | Crashes, memory corruption | `DllImport`, `LibraryImport` |
| C# ↔ SQL | Injection, perf (N+1) | `FromSqlRaw`, raw string concat |
| C# ↔ Config | Missing keys, type mismatch | `IConfiguration`, `IOptions<T>` |

## Constraints

### MUST DO
- Inventory ALL languages present — do not ignore minority languages
- Report lines of code, not just file counts
- Detect and catalog every cross-language boundary
- Apply language-appropriate quality gates (not just C# rules everywhere)
- Exclude `node_modules/`, `bin/`, `obj/`, `.git/`, and vendor directories

### MUST NOT
- Do not apply C# complexity thresholds to other languages
- Do not ignore configuration languages (YAML, JSON) — they are a quality surface
- Do not count auto-generated or vendored files
- Do not present file-count-only distribution — LOC is required for meaningful comparison
- Do not skip boundary health assessment — boundaries are where polyglot bugs hide

## Output Template

```markdown
# Polyglot Quality Report

**Project:** [Name]  |  **Date:** YYYY-MM-DD  |  **Languages Found:** N

## Language Distribution
| Language | Files | LOC | % of Codebase | Quality Score |
|----------|-------|-----|---------------|---------------|

## Cross-Language Boundaries
| # | Boundary | Direction | Files | Health | Key Risk |
|---|----------|-----------|-------|--------|----------|

## Per-Language Quality
### C# (N files, N LOC)
### TypeScript (N files, N LOC)
### SQL (N files, N LOC)

## Boundary Health Matrix
| From → To | Count | 🟢 | 🟡 | 🔴 |
|-----------|-------|-----|-----|-----|

## Recommendations
1. [Highest-risk boundary or language quality issue]
2. [Second priority]
3. [Third priority]
```
