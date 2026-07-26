---
name: tech-debt-tracker
description: "Detect, quantify, and prioritize technical debt — SATD detection, hour estimation, priority matrix, and sprint planning"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: track tech debt, find technical debt, SATD scan, debt inventory, debt report, how much tech debt, prioritize debt, debt sprint planning
  role: tracker
  scope: tracking
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: quality-analyzer, smart-refactor, refactor-planner
---

# Tech Debt Tracker

A systematic technical debt detection and quantification skill that scans for Self-Admitted Technical Debt (SATD), estimates remediation effort in hours, builds a priority matrix, and generates sprint-ready debt reduction plans. Based on Potdar & Shihab (2014) SATD classification methodology and industry estimation heuristics. Uses native PowerShell and grep — no external tooling required.

## When to Use This Skill

- "How much tech debt do we have?" or "Find technical debt"
- "Scan for TODOs and FIXMEs" or "SATD inventory"
- "Prioritize our tech debt" or "What should we fix first?"
- "Generate a debt report for stakeholders"
- Before sprint planning to allocate debt reduction capacity
- Quarterly codebase health assessments

## Core Workflow

1. **Scan for SATD Annotations** — Detect Self-Admitted Technical Debt across all source files. Use multi-pattern search with context for classification:
   ```powershell
   Select-String -Pattern 'TODO|FIXME|HACK|XXX|UNDONE|WORKAROUND|KLUDGE|REFACTOR|REVIEW|OPTIMIZE|TEMP|BRITTLE' -Recurse -Include *.cs,*.razor,*.csproj,*.json -Context 0,2
   ```
   Load `references/satd-patterns.md` for the full Potdar & Shihab classification taxonomy.
   - **Checkpoint:** All SATD annotations collected with file, line, context, and raw text.

2. **Classify Debt by Category** — Categorize each finding using the Potdar & Shihab taxonomy:
   - **Design Debt** — HACK, WORKAROUND, KLUDGE, architectural shortcuts
   - **Defect Debt** — FIXME, BUG, known-broken paths
   - **Requirement Debt** — TODO with feature implications, incomplete implementations
   - **Documentation Debt** — TODO doc, missing XML comments on public APIs
   - **Test Debt** — TODO test, skipped tests, low coverage markers
   - **Checkpoint:** Every finding categorized; uncategorizable items flagged for manual review.

3. **Estimate Remediation Effort** — Apply estimation heuristics per category. Load `references/estimation-model.md` for the full methodology:
   - **Simple** (1–2 hrs): Rename, add comment, fix typo, remove dead code
   - **Moderate** (2–8 hrs): Extract method, add validation, write missing test
   - **Complex** (8–24 hrs): Redesign class, replace pattern, add error handling layer
   - **Major** (24–80 hrs): Architecture change, replace library, rewrite module
   - **Checkpoint:** Hour estimates assigned; total debt quantified in person-hours.

4. **Build Priority Matrix** — Score each item on Impact (1–5) × Effort-to-fix (1–5). Compute priority = Impact / Effort (higher is better ROI). Sort by priority descending. Factor in: proximity to critical path, blast radius, frequency of modification (use `git log --oneline -- <file> | Measure-Object` for change frequency).
   - **Checkpoint:** Priority matrix complete with ROI scores.

5. **Generate Debt Report** — Compile into stakeholder-ready report with executive summary, category breakdown, priority matrix, sprint recommendations (allocate 15–20% of sprint capacity to debt). Load `references/debt-report-template.md` for formatting.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| SATD Patterns | `references/satd-patterns.md` | Classifying debt annotations |
| Estimation Model | `references/estimation-model.md` | Assigning hour estimates |
| Debt Report Template | `references/debt-report-template.md` | Generating stakeholder reports |

## Quick Reference

```powershell
# Full SATD scan with context
Select-String -Pattern 'TODO|FIXME|HACK|XXX|UNDONE|WORKAROUND|KLUDGE' `
  -Recurse -Include *.cs,*.razor -Context 0,2 |
  ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line.Trim())" }

# Count by category
Select-String -Pattern 'TODO' -Recurse -Include *.cs | Measure-Object  # Requirement debt
Select-String -Pattern 'FIXME' -Recurse -Include *.cs | Measure-Object # Defect debt
Select-String -Pattern 'HACK|WORKAROUND|KLUDGE' -Recurse -Include *.cs | Measure-Object # Design debt

# File change frequency (debt in hot files = higher priority)
git log --oneline --since="6 months ago" -- "*.cs" |
  ForEach-Object { ($_ -split ' ', 2)[1] } | Group-Object | Sort-Object Count -Descending | Select-Object -First 10
```

| Debt Category | Typical Markers | Avg Effort | Risk Level |
|---------------|----------------|------------|------------|
| Design Debt | HACK, WORKAROUND, KLUDGE | 8–24 hrs | 🔴 High |
| Defect Debt | FIXME, BUG | 2–8 hrs | 🔴 High |
| Requirement Debt | TODO (feature) | 4–16 hrs | 🟡 Medium |
| Documentation Debt | TODO doc, missing /// | 1–4 hrs | 🟢 Low |
| Test Debt | TODO test, [Skip] | 2–8 hrs | 🟡 Medium |

## Constraints

### MUST DO
- Scan ALL source files in scope — no sampling
- Classify every SATD finding into a category
- Provide hour estimates for every item (range is acceptable)
- Include a priority matrix with ROI scoring
- Exclude `bin/`, `obj/`, and auto-generated files from scan

### MUST NOT
- Do not auto-fix debt — this skill is detection and planning only
- Do not undercount by ignoring non-standard markers (scan for synonyms)
- Do not report debt without remediation estimates
- Do not ignore test debt — it compounds design and defect debt
- Do not present raw grep output as the report — always classify and quantify

## Output Template

```markdown
# Technical Debt Report

**Scope:** [Target]  |  **Date:** YYYY-MM-DD  |  **Analyst:** AI Debt Tracker

## Executive Summary
- **Total SATD items:** N  |  **Estimated effort:** N person-hours
- **Design:** N (N hrs)  |  **Defect:** N (N hrs)  |  **Requirement:** N (N hrs)
- **Documentation:** N (N hrs)  |  **Test:** N (N hrs)

## Priority Matrix (Top 10)
| # | Item | Category | File | Line | Impact | Effort | Priority (I/E) |
|---|------|----------|------|------|--------|--------|----------------|

## Category Breakdown
### Design Debt (N items, N hrs)
### Defect Debt (N items, N hrs)
### Requirement Debt (N items, N hrs)

## Sprint Recommendations
- Allocate N hours (15–20% of sprint) to debt reduction
- **Sprint focus:** [Top category] — addresses N items, saves N hrs future cost
- **Quick wins:** [Items with Priority > 3.0]

## Trend (if historical data available)
| Sprint | Total Items | Total Hours | Delta |
```
