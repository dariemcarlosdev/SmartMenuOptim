---
name: refactor-planner
description: "Plan safe, incremental refactoring with dependency mapping and blast radius analysis — triggered by 'plan refactor', 'refactor this', 'improve code structure'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: code-quality
  triggers: plan refactor, refactor this, improve code structure, code smells, redesign, restructure, extract class, extract method, simplify
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: code-reviewer, code-documenter, owasp-audit
---

# Refactor Planner

A structured refactoring planning skill that identifies code smells, maps dependencies, assesses blast radius, and generates a step-by-step plan where each step keeps the test suite green.

## When to Use This Skill

- "Plan a refactor for this module"
- "This class is too big — how do I break it up?"
- "Improve the structure of this code"
- "Find code smells and plan fixes"
- When technical debt needs systematic reduction
- Before a large feature addition that requires cleaner foundations

## Core Workflow

1. **Identify Code Smells** — Scan target code for Bloaters, OO Abusers, Change Preventers, Dispensables, and Couplers. Load `references/code-smells.md` for the full catalog.
   - **Checkpoint:** All smells cataloged with location and impact before proceeding.

2. **Map Dependencies** — Trace direct, reverse, and transitive dependencies. Document interface contracts, DI registrations, and test coverage. Load `references/dependency-mapping.md` for mapping technique.
   - **Checkpoint:** Dependency tree complete — all callers and callees identified.

3. **Assess Blast Radius** — For each proposed change, evaluate files affected, public API impact, DB schema impact, and test impact. Classify as 🟢 Contained (1-3 files), 🟡 Moderate (4-10), or 🔴 Wide (10+). Load `references/migration-strategies.md` for safe migration patterns.
   - **Checkpoint:** Blast radius classified for every proposed step.

4. **Create Step-by-Step Plan** — Each step must be atomic, verifiable, reversible, and small. Select refactoring technique from `references/refactoring-catalog.md`. Order from lowest to highest risk.
   - **Checkpoint:** Each step has a verification checklist (tests pass, compiles clean, API preserved).

5. **Define Verification Gates** — After each step: all existing tests pass, new tests cover extracted behavior, code compiles without warnings, architecture boundaries maintained.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Code Smells Catalog | `references/code-smells.md` | Identifying code smells |
| Refactoring Techniques | `references/refactoring-catalog.md` | Choosing refactoring techniques |
| Dependency Mapping | `references/dependency-mapping.md` | Analyzing blast radius |
| Migration Strategies | `references/migration-strategies.md` | Planning safe migrations |

## Quick Reference

```text
Target: OrderService
├── Direct deps: IOrderRepository, IPaymentGateway, ILogger<OrderService>
├── Reverse deps: OrderController, OrderCommandHandler, OrderIntegrationTests
├── Interface: IOrderService (3 methods)
├── DI Registration: services.AddScoped<IOrderService, OrderService>()
└── Tests: OrderServiceTests (12 tests), OrderIntegrationTests (4 tests)
```

Blast radius classification:
- 🟢 **Contained** — 1-3 files, no public API change
- 🟡 **Moderate** — 4-10 files, backward-compatible API changes
- 🔴 **Wide** — 10+ files, breaking changes, schema migration needed

## Constraints

### MUST DO
- Ensure every step keeps the test suite green
- Map all dependencies before proposing changes
- Provide blast radius assessment for each step
- Include verification checkpoints between steps
- Preserve existing public API behavior unless explicitly planned
- Recommend adding tests before refactoring if coverage is insufficient
- Order steps from lowest risk to highest risk

### MUST NOT
- Do not propose "big bang" rewrites — always incremental steps
- Do not change behavior during refactoring — structure-only
- Do not skip the dependency mapping phase
- Do not propose refactoring without verifying test coverage exists
- Do not mix refactoring with feature changes in the same step
- Do not propose more than 10 files changed in one step

## Output Template

```markdown
# Refactoring Plan

**Target:** [Module/class]  |  **Date:** YYYY-MM-DD
**Overall Blast Radius:** 🟢/🟡/🔴

## Code Smell Analysis
| # | Smell | Location | Description | Impact |
|---|-------|----------|-------------|--------|

## Dependency Map
(tree diagram of deps, reverse deps, DI registrations, tests)

## Refactoring Steps
### Step N: [Technique] — [Description]
**What/Why/Blast Radius/Files Changed/Effort**
Before → After code examples
**Verification:** [ ] Tests pass  [ ] Compiles clean  [ ] Commit message

## Risk Assessment
| Step | Risk | Mitigation |

## Prerequisites
- [ ] Test coverage verified  - [ ] Feature branch created
```
