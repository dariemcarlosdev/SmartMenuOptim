# ADR Template — MADR Format

Full MADR template with section guidance for the NexTruzt.io escrow platform.

## Conventions

- **File naming:** `adr-NNNN-kebab-case-title.md` (e.g., `adr-0012-adopt-postgresql-for-escrow-ledger.md`)
- **Directory:** `docs/adr/` (preferred) or `docs/architecture/decisions/`
- **Index:** Include a `README.md` listing all ADRs with status for navigation

## Complete MADR Template

```markdown
---
adr: NNNN
title: "{Decision Title}"
date: YYYY-MM-DD
status: Proposed
deciders: escrow-platform-team
supersedes: ADR-XXXX          # optional
superseded-by: ADR-YYYY       # optional
---

# ADR-NNNN: {Decision Title}

**Date:** YYYY-MM-DD
**Status:** Proposed
**Deciders:** {team or individuals}

## Context and Problem Statement

{Describe the forces at play: business requirements, technical constraints,
compliance needs, team capabilities. For NexTruzt.io, always consider:
regulatory requirements, audit trails, multi-tenant isolation, transaction integrity.}

## Decision Drivers

- {Driver 1 — e.g., "Must maintain audit trail per PCI-DSS"}
- {Driver 2 — e.g., "Team has 3+ years experience with chosen tech"}
- {Driver 3 — be specific and measurable where possible}

## Considered Options

1. **{Option A}** — {one-line summary}
2. **{Option B}** — {one-line summary}
3. **{Option C}** — {one-line summary} *(if applicable)*

## Decision Outcome

Chosen option: **"{Option X}"**, because {rationale tied to drivers above}.

### Consequences

- **Good**, because {positive outcome}
- **Bad**, because {trade-off accepted}

### Confirmation

{How will the team verify this decision works? Metrics, review checkpoints.}

## Pros and Cons of the Options

### {Option A}
- Good, because {advantage}
- Neutral, because {observation}
- Bad, because {disadvantage}

### {Option B}
- Good, because {advantage}
- Bad, because {disadvantage}

## Links

- Supersedes: [ADR-XXXX](adr-XXXX-title.md) *(if applicable)*
- {Related ADRs, RFCs, issues, or external resources}
```

## Section Guidance

| Section | Purpose | Common Mistake |
|---|---|---|
| Context and Problem Statement | Why this decision is needed | Too brief; missing constraints |
| Decision Drivers | Evaluation criteria | Vague ("good performance") vs measurable |
| Considered Options | Alternatives evaluated | Strawman alternatives or only one option |
| Decision Outcome | Choice and rationale | Not linking rationale back to drivers |
| Consequences | Impact of the decision | Only listing positives; omitting trade-offs |
| Confirmation | Verification approach | Missing entirely |
| Pros and Cons | Detailed option analysis | Unbalanced analysis favoring chosen option |
| Links | Traceability | Missing links to related/superseded ADRs |

See [Status Lifecycle](status-lifecycle.md) for status values and transition rules.
