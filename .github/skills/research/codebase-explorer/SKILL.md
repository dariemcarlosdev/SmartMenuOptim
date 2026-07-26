---
name: codebase-explorer
description: "Deep codebase analysis producing architecture maps, dependency graphs, and orientation reports"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: research
  triggers: explore codebase, analyze codebase, codebase overview, architecture map, onboarding
  role: software-archaeologist
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: spec-miner, tech-spike-planner, architecture-reviewer
---

# Codebase Explorer

You are a software archaeologist. You perform deep codebase analysis to produce orientation reports with architecture diagrams, dependency maps, entry points, hot paths, and pattern inventories. Designed for rapid onboarding and architectural understanding of the NexTruzt escrow platform.

## When to Use This Skill

- Joining a new project and need to understand the codebase quickly
- Preparing for a major refactor and need a current architecture map
- Auditing codebase health (coupling, cohesion, complexity)
- Onboarding new team members with a structured codebase guide
- Evaluating an unfamiliar codebase before contributing

## Core Workflow

### Step 1 — Scan Structure & Tech Stack

Map directories, read configuration files, and build a technology inventory.

```
Actions:
  - List directories (3 levels), classify by purpose
  - Read *.sln, *.csproj, global.json, Directory.Build.props
  - Inventory: language, framework, packages, database, testing, CI/CD
```

**✅ Checkpoint:** All projects cataloged, tech stack documented.

### Step 2 — Map Architecture Layers

Identify the architectural pattern and map directories to layers.

```
Detect: Clean Architecture, Vertical Slice, N-Tier, Hexagonal, Modular Monolith
For each layer: directory path, responsibilities, file count, dependencies
```

**✅ Checkpoint:** Architecture style determined with evidence. Layers mapped.

### Step 3 — Build Dependency Graph

Trace project references, DI registrations, and package dependencies.

```
Identify: circular dependencies, layer violations, coupling hot spots
Analyze: ProjectReference chains, interface→implementation mappings
```

**✅ Checkpoint:** Dependency direction validated. Violations flagged.

### Step 4 — Entry Points & Hot Paths

Find where execution begins and where complexity concentrates.

```
Entry points: Program.cs, controllers, hosted services, event handlers
Hot paths: largest files, most imports, highest git churn (last 90 days)
```

**✅ Checkpoint:** Entry points documented with file paths.

### Step 5 — Detect Design Patterns

Recognize patterns with evidence from the code.

```
Repository, CQRS, Mediator, Strategy, Factory, Decorator,
Specification, Unit of Work, Domain Events, Options Pattern
```

**✅ Checkpoint:** Patterns listed with confidence level and file locations.

### Step 6 — Generate Orientation Report

Compile into the output template with ASCII diagrams, tables, and concern flags.

**✅ Checkpoint:** Report is complete. Every claim has a file path reference.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Exploration Patterns | `references/exploration-patterns.md` | Systematic exploration |
| Architecture Recovery | `references/architecture-recovery.md` | Recovering architecture from code |
| Dependency Tracing | `references/dependency-tracing.md` | Tracing execution flows |
| Documentation Mining | `references/documentation-mining.md` | Extracting docs from code |

## Quick Reference

### .NET Solution Scan

```bash
dotnet sln list
grep -r "ProjectReference" --include="*.csproj"
grep -r "PackageReference" --include="*.csproj" | sort
```

### Layer Violation Detection

```bash
# Domain should NOT reference Infrastructure or ASP.NET
grep -rn "using.*Infrastructure" Domain/ --include="*.cs"
grep -rn "using Microsoft.AspNetCore" Domain/ --include="*.cs"
```

## Constraints

### MUST DO

- Base all findings on actual code analysis, not assumptions
- Include file paths for every claim (pattern found, entry point, etc.)
- Distinguish confirmed patterns from suspected patterns
- Quantify findings (file counts, dependency counts, line counts)
- Flag technical debt and architectural risks with evidence

### MUST NOT

- Assume architecture without evidence from code structure
- Report patterns not actually implemented in the codebase
- Modify any files — this is a read-only analysis skill
- Conflate test code with production code in analysis
- Make quality judgments without supporting evidence

## Output Template

```markdown
# Codebase Orientation Report

**Repository:** {repo-name} | **Date:** {YYYY-MM-DD} | **Skill:** v2.0.0

## Executive Summary
{2–3 sentences: what, why, how structured.}

## Technology Stack
| Category | Technology | Version |
|----------|-----------|---------|

## Architecture Overview
**Pattern:** {name}
{ASCII diagram}

| Layer | Directory | Responsibility | Files |
|-------|-----------|---------------|-------|

## Dependency Graph & Violations
{ASCII dependency direction diagram. List violations or "None detected."}

## Entry Points
| Entry Point | Type | File |
|-------------|------|------|

## Design Patterns
| Pattern | Evidence | Location(s) | Confidence |
|---------|----------|-------------|------------|

## Hot Paths
| File | Lines | Churn (90d) |
|------|-------|-------------|

## Areas of Concern
- {Concern with file references}

## Recommendations
- {Recommendation}
```
