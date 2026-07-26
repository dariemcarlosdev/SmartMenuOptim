# Specification Template (Feature Forge)

Complete specification template for features produced by requirements workshops.

## Feature Specification Document

```markdown
# Feature Specification: {Feature Name}

**Author:** {Name}
**Date:** {YYYY-MM-DD}
**Status:** Draft | In Review | Approved
**Version:** 1.0
**Workshop participants:** {Names}

---

## 1. Overview

**Feature:** {One-line description}
**Business Goal:** {What business outcome this serves}
**Target Users:** {Primary user roles}
**Priority:** {Critical | High | Medium | Low}

## 2. Problem Statement

{What pain or gap exists today? Why does this matter?
Include metrics: error rates, support tickets, revenue impact.}

## 3. User Stories

### US-001: {Story Title}
As a {role}, I want {action}, so that {benefit}.

**Acceptance Criteria:**
- Given {precondition}, when {action}, then {result}
- Given {precondition}, when {action}, then {result}

### US-002: {Story Title}
As a {role}, I want {action}, so that {benefit}.

## 4. EARS Requirements

### Functional Requirements

| ID | EARS Requirement | Priority | Story |
|----|-----------------|----------|-------|
| REQ-001 | When {trigger}, the system shall {action}. | High | US-001 |
| REQ-002 | While {state}, the system shall {action}. | High | US-001 |
| REQ-003 | If {error condition}, then the system shall {action}. | Medium | US-002 |

### Non-Functional Requirements

| ID | Category | EARS Requirement | Target |
|----|----------|-----------------|--------|
| NFR-001 | Performance | The system shall respond within {N}ms (P95). | {target} |
| NFR-002 | Security | The system shall require {auth level} for {action}. | {policy} |
| NFR-003 | Reliability | If {failure}, then the system shall {recovery}. | {SLA} |

## 5. Scope

### In Scope
- {Deliverable 1}
- {Deliverable 2}

### Out of Scope
- {Explicitly excluded item}

### MVP vs. Full Feature
| Capability | MVP | Full |
|-----------|-----|------|
| {Capability 1} | ✅ | ✅ |
| {Capability 2} | ❌ | ✅ |

## 6. Workflow

### Happy Path
1. {Step 1}
2. {Step 2}
3. {Step 3}

### State Diagram
```
[Created] → [Funded] → [Approved] → [Released] → [Closed]
                  ↓           ↓
              [Expired]   [Disputed] → [Resolved]
```

## 7. Data Model

| Entity | Key Fields | Relationships |
|--------|-----------|---------------|
| {Entity} | {fields} | {relationships} |

## 8. Implementation Checklist

- [ ] Domain: Entities, value objects, interfaces
- [ ] Application: Commands, queries, validators, handlers
- [ ] Infrastructure: Repository, EF configuration, migrations
- [ ] Presentation: Endpoints, DTOs, Blazor pages
- [ ] Testing: Unit tests, integration tests
- [ ] Documentation: API docs, user guide updates

## 9. Open Questions

- [ ] {Question} — Owner: {name} — Due: {date}

## 10. Appendix

{Diagrams, mockups, references}
```

## Section Writing Guidelines

### Problem Statement

Write the problem statement BEFORE the solution:

```
❌ "We need to add a dispute workflow."
✅ "Buyers have no way to formally dispute an escrow when conditions aren't met.
    This results in 15 support tickets/week and $50K in manual resolution costs.
    A formal dispute workflow would reduce support load by 80% and provide
    audit-compliant resolution tracking."
```

### User Stories

Keep stories small and focused:

```
❌ "As a user, I want to manage escrows" (too vague)
✅ "As a buyer, I want to raise a dispute on a funded escrow,
    so that I can formally request resolution when conditions aren't met"
```

### EARS Requirements

One requirement per sentence. Use "shall" not "should":

```
❌ "The system should handle disputes and send notifications" (two things, vague)
✅ "When a buyer raises a dispute, the system shall change escrow status to Disputed."
✅ "When escrow status changes to Disputed, the system shall notify the seller via email."
```

## Traceability Matrix

Link requirements → stories → tests → implementation:

```markdown
| Requirement | User Story | Test | Implementation |
|-------------|-----------|------|----------------|
| REQ-001 | US-001 | CreateEscrow_ValidData_ReturnsCreated | CreateEscrowHandler.cs |
| REQ-002 | US-001 | CreateEscrow_InvalidAmount_ReturnsValidationError | CreateEscrowValidator.cs |
```
