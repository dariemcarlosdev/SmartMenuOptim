---
name: architecture-reviewer
description: "Review architecture decisions for Clean Architecture compliance, SOLID principles, and dependency direction — trigger: review architecture, check layers, architecture health"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: architecture
  triggers: review architecture, check layers, architecture health, dependency direction, coupling analysis, architectural debt
  role: reviewer
  scope: review
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: design-pattern-advisor, dependency-analyzer
---

# Architecture Reviewer

Review software architecture against Clean Architecture principles, SOLID at the architectural level, and produce actionable findings with severity ratings.

## When to Use This Skill

- Before a major feature lands — validate it doesn't introduce layer violations
- During periodic architecture health checks on the codebase
- When onboarding to a new project to understand its structural quality
- After significant refactoring to confirm architectural integrity
- When coupling between modules feels too tight and you need evidence
- To identify and catalog architectural debt for prioritization

## Core Workflow

1. **Map Architecture Layers** — Identify architecture style, catalog layers/projects, build dependency graph from `.csproj` references and `using` statements
   - ✅ Checkpoint: Every project mapped to exactly one layer

2. **Check Dependency Direction** — Verify inner layers never reference outer layers; flag concrete dependencies where abstractions belong → See `references/dependency-direction.md`
   - ✅ Checkpoint: Zero inward-to-outward violations

3. **Assess Coupling** — Measure afferent/efferent coupling, identify circular dependencies → See `references/coupling-analysis.md`
   - ✅ Checkpoint: No circular dependencies; instability ratios computed

4. **Evaluate Layer Compliance** — Validate each layer follows its responsibilities; check cross-cutting concerns use abstractions → See `references/layer-compliance.md`
   - ✅ Checkpoint: Findings categorized by severity

5. **Generate Report** — Produce findings with severity ratings, remediation steps, and fitness scores → See `references/architecture-fitness.md`

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Layer Compliance | `references/layer-compliance.md` | Checking Clean Architecture boundaries |
| Coupling Analysis | `references/coupling-analysis.md` | Measuring coupling metrics |
| Dependency Direction | `references/dependency-direction.md` | Validating dependency inversion |
| Architecture Fitness | `references/architecture-fitness.md` | Architecture fitness functions |

## Quick Reference

```csharp
// ✅ Correct: Application depends on Domain abstraction
public sealed class CreateEscrowHandler : IRequestHandler<CreateEscrowCommand, EscrowId>
{
    private readonly IEscrowRepository _repository; // Domain interface
}

// ❌ Violation: Domain referencing Infrastructure
namespace EscrowApp.Domain.Entities;
using EscrowApp.Infrastructure.Data; // LAYER VIOLATION
```

## Constraints

### MUST DO
- Verify dependency direction — inner layers MUST NOT reference outer layers
- Check for circular dependencies between projects/namespaces
- Rate each finding: `CRITICAL`, `WARNING`, or `INFO`
- Provide concrete, actionable remediation steps
- Respect the project's declared architecture style

### MUST NOT
- Recommend patterns that add complexity without clear benefit
- Flag stylistic preferences as architectural violations
- Generate code changes — this skill produces analysis only
- Assume a single "correct" architecture

## Output Template

```markdown
# Architecture Review Report

**Project:** {project-name} | **Date:** {date}
**Architecture Style:** {style} | **Health:** {🟢|🟡|🔴}

## Dependency Diagram
{ASCII diagram — ✓ correct, ✗ violations}

## Findings
### CRITICAL
- **[C-001] {Title}** — Location: {file} | Impact: {desc} | Fix: {steps}

### WARNING
- **[W-001] {Title}** — Location: {file} | Fix: {steps}

## Coupling Analysis
| Module | Ca | Ce | Instability | Assessment |

## Architectural Debt Register
| ID | Description | Severity | Blast Radius | Effort | Priority |
```
