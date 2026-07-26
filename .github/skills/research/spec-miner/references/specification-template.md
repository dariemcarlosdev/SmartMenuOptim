# Specification Template (Spec-Miner)

Template for the discovered specification document produced by reverse-engineering.

## Discovered Specification Document

```markdown
# Discovered Specification: {Module/System Name}

**Analyzed by:** Spec-Miner Skill v2.0.0
**Date:** {YYYY-MM-DD}
**Codebase:** {repository name}
**Confidence:** {High | Medium | Low — overall assessment}

---

## 1. System Overview

{2-3 sentence summary of what this system does, derived from code analysis.
Include the architecture style, primary domain, and key technologies.}

**Architecture:** {Clean Architecture | Vertical Slice | N-Tier}
**Domain:** {e.g., Financial escrow management}
**Tech Stack:** {.NET 10, Blazor Server, EF Core, MediatR, SQL Server}

## 2. Domain Model

### Entities

| Entity | Key Properties | Aggregate? | Source File |
|--------|---------------|-----------|-------------|
| {name} | {properties} | Yes/No | {path} |

### Value Objects

| Value Object | Properties | Used By | Source File |
|-------------|-----------|---------|-------------|
| {name} | {properties} | {entity} | {path} |

### Enumerations

| Enum | Values | Purpose | Source File |
|------|--------|---------|-------------|
| {name} | {values} | {purpose} | {path} |

## 3. State Machines

### {Entity} Status

```
[State1] --{trigger}--> [State2] --{trigger}--> [State3]
                             \--{trigger}--> [State4]
```

| From | To | Trigger | Guard Condition | Source |
|------|----|---------|----------------|--------|
| {state} | {state} | {event} | {condition} | {file:line} |

## 4. Discovered Requirements

### Functional Requirements

| ID | EARS Requirement | Confidence | Source |
|----|-----------------|------------|--------|
| REQ-001 | When {trigger}, the system shall {action}. | High | {file:line} |
| REQ-002 | While {state}, the system shall {action}. | Medium | {file:line} |
| REQ-003 | If {error}, then the system shall {action}. | High | {file:line} |

### Non-Functional Requirements

| ID | Category | Requirement | Evidence | Source |
|----|----------|------------|----------|--------|
| NFR-001 | Performance | {discovered requirement} | {code evidence} | {file} |
| NFR-002 | Security | {discovered requirement} | {code evidence} | {file} |

### Security Requirements

| ID | Requirement | Implementation | Source |
|----|------------|----------------|--------|
| SEC-001 | {requirement} | {how it's implemented} | {file} |

## 5. Use Cases (from MediatR Handlers)

| Handler | Command/Query | Description | Validators | Source |
|---------|--------------|-------------|-----------|--------|
| {name} | {request type} | {what it does} | {validators} | {path} |

## 6. Integration Points

| System | Protocol | Direction | Handler | Source |
|--------|----------|-----------|---------|--------|
| {name} | HTTP/Message/DB | In/Out | {class} | {path} |

## 7. Business Rules (from Validators)

| Rule | Constraint | Error Message | Source |
|------|-----------|---------------|--------|
| {description} | {expression} | {message} | {file:line} |

## 8. Gaps and Unknowns

### Missing Specifications
- {Area where code exists but intent is unclear}
- {Undocumented business rule}

### Inconsistencies Found
- {Conflicting behavior between components}
- {Naming inconsistency suggesting different intent}

### Recommended Investigations
- [ ] {Area needing stakeholder clarification}
- [ ] {Area needing test coverage to confirm behavior}

## 9. Appendix

### Files Analyzed
| Path | Type | Lines | Analyzed |
|------|------|-------|----------|
| {path} | {entity/handler/config} | {N} | ✅/❌ |
```

## Confidence Assessment Guide

| Overall Confidence | Criteria |
|-------------------|----------|
| **High** | > 80% of requirements have test coverage, consistent patterns |
| **Medium** | 50-80% test coverage, some inconsistencies |
| **Low** | < 50% test coverage, significant gaps or contradictions |

## Tips for Filling the Template

1. **Start with entities** — they define the domain vocabulary
2. **Map state machines** — they reveal the core business workflows
3. **Extract handlers** — each handler is a use case
4. **Read validators** — they document business constraints
5. **Check tests** — they confirm (or contradict) the discovered rules
6. **Flag gaps** — missing tests, unclear naming, dead code
