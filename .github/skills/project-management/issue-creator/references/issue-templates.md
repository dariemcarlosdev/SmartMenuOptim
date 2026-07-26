# Issue Templates

Templates for creating GitHub issues: features, bugs, and epics.

## Feature Issue Template

```markdown
## [Feature] {Concise description of what changes}

### Context

{Why this work matters. Link to specification, user feedback, or OKR.}

### Problem

{What is missing or insufficient today.}

### Proposed Solution

{Brief description of the expected change — behavior, not implementation.}

### Acceptance Criteria

- [ ] Given {precondition}, when {action}, then {expected result}
- [ ] Given {precondition}, when {action}, then {expected result}
- [ ] Error: When {invalid input/state}, then {expected error handling}
- [ ] Edge: When {boundary condition}, then {expected behavior}

### Technical Approach

- **Affected areas:** {files, components, services, layers}
- **Suggested approach:** {implementation direction}
- **Patterns to follow:** {existing conventions}
- **Open questions:** {areas needing investigation}

### Metadata

- **Priority:** {P0-P3}
- **Labels:** `feature`, {additional labels}
- **Estimated effort:** {S/M/L}
```

## Bug Report Template

```markdown
## [Bug] {Description of incorrect behavior}

### Context

{Where discovered. Link to logs, alerts, or user reports.}

### Steps to Reproduce

1. {Step one}
2. {Step two}
3. {Observe: what happens}

### Expected Behavior

{What should happen.}

### Actual Behavior

{What happens instead. Include error messages or logs.}

### Environment

- **App version:** {version}
- **OS / Browser:** {details}
- **.NET version:** {e.g., .NET 10}
- **Configuration:** {relevant settings}

### Acceptance Criteria

- [ ] The bug no longer reproduces following the steps above
- [ ] Regression test added covering this scenario
- [ ] {Related edge case covered}

### Technical Approach

- **Root cause hypothesis:** {best guess}
- **Affected areas:** {files, components}
- **Suggested fix:** {direction}

### Metadata

- **Priority:** {P0-P3}
- **Labels:** `bug`, {additional labels}
- **Estimated effort:** {S/M/L}
```

## Epic / Parent Issue Template

```markdown
## [Feature] {Epic title — the overall capability}

### Context

{Why this feature is being built. Link to specification or OKR.}

### Overview

{High-level description of the feature and its user value.}

### Sub-Tasks

- [ ] #{number} — {Sub-task 1} (dependency: none)
- [ ] #{number} — {Sub-task 2} (dependency: Sub-task 1)
- [ ] #{number} — {Sub-task 3} (dependency: none)
- [ ] #{number} — {Sub-task 4} (dependency: Sub-task 2, 3)

### Acceptance Criteria (Feature-Level)

- [ ] {End-to-end criterion that validates the whole feature}
- [ ] {Integration criterion across sub-tasks}

### Metadata

- **Priority:** {P0-P3}
- **Labels:** `feature`, `epic`
- **Estimated total effort:** {sum across sub-tasks}
```

## Chore / Tech Debt Template

```markdown
## [Chore] {Description of technical work}

### Context

{Why this tech debt matters. Impact on velocity, reliability, or security.}

### Current State

{What exists today and why it's problematic.}

### Desired State

{What the code/system should look like after this work.}

### Acceptance Criteria

- [ ] {Measurable improvement — e.g., "build time < 60s"}
- [ ] {No regression in existing behavior}
- [ ] {Tests pass after changes}

### Metadata

- **Priority:** {P0-P3}
- **Labels:** `chore`, `tech-debt`
- **Estimated effort:** {S/M/L}
```

## NexTruzt Platform Issue Examples

### Feature Example

```markdown
## [Feature] Add escrow dispute workflow

### Context
Buyers and sellers need a formal dispute resolution process when they disagree
on whether escrow conditions have been met. Required for SOX compliance.

### Acceptance Criteria
- [ ] Given a funded escrow, when the buyer raises a dispute, then escrow status
      changes to "Disputed" and both parties are notified
- [ ] Given a disputed escrow, when an admin resolves the dispute, then funds are
      released or refunded based on the resolution
- [ ] Error: When a non-participant attempts to raise a dispute, then return 403
```

### Bug Example

```markdown
## [Bug] Escrow creation returns 500 when currency is null

### Steps to Reproduce
1. POST /api/escrows with body: { "amount": 100, "currency": null }
2. Observe HTTP 500 instead of validation error

### Expected: HTTP 400 with validation message "Currency is required"
### Actual: HTTP 500 with unhandled NullReferenceException
```
