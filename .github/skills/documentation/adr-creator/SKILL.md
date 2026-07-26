---
name: adr-creator
description: "Create Architecture Decision Records using MADR format. Triggers: adr, architecture decision, decision record"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: documentation
  triggers: adr, architecture decision, decision record, create adr
  role: software-architect
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: api-documenter, readme-generator
---

# ADR Creator

Create Architecture Decision Records (ADRs) using the MADR (Markdown Any Decision Records) format for the NexTruzt.io fintech escrow platform.

## When to Use

- A significant technology or framework choice is being made (database, auth provider, messaging)
- Choosing between competing architectural patterns (CQRS vs CRUD, monolith vs microservices)
- Establishing or changing a platform convention (coding standard, deployment strategy, API versioning)
- Deprecating or superseding an existing architectural decision
- Recording a retroactive decision for institutional knowledge
- A recurring technical debate needs a documented, authoritative resolution

## Core Workflow

### 1. Gather Context

Scan the repository for existing ADRs and understand the decision landscape.

```
→ Identify the decision trigger (new feature, tech debt, compliance requirement)
→ Determine scope of impact (single service, domain boundary, platform-wide)
→ List stakeholders and their primary concerns
→ Check for existing ADRs that this decision relates to or supersedes
```

✅ **Checkpoint:** Decision trigger, scope, and stakeholders are clearly identified.

### 2. Evaluate Options

Analyze alternatives using decision drivers from the project context.

```
→ Define 2-4 realistic options (always include "do nothing" if relevant)
→ Identify decision drivers: quality attributes, constraints, team capabilities
→ Assess each option against drivers using Good/Bad/Neutral framing
→ Load [Decision Drivers](references/decision-drivers.md) for fintech-specific criteria
```

✅ **Checkpoint:** Each option has clear pros/cons mapped to decision drivers.

### 3. Document the Decision

Write the ADR using the MADR template structure.

```
→ Load [ADR Template](references/adr-template.md) for the full MADR format
→ Write Context section first — this is the most valuable section for future readers
→ State the decision in active voice: "We will use X because..."
→ Document consequences honestly — include trade-offs and risks
→ Set initial status per [Status Lifecycle](references/status-lifecycle.md)
```

✅ **Checkpoint:** All MADR sections are complete with no placeholders remaining.

### 4. Assign Number and File

Determine the ADR number and generate the output file.

```
→ Scan docs/adr/ (or docs/architecture/decisions/) for existing ADRs
→ Assign the next sequential number: adr-NNNN-kebab-case-title.md
→ If no ADR directory exists, create docs/adr/ with an index README
→ Update any superseded ADRs with a link to this new record
```

✅ **Checkpoint:** File is numbered sequentially, named correctly, and placed in the ADR directory.

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [ADR Template](references/adr-template.md) | MADR template | Full MADR template with section guidance |
| [Decision Drivers](references/decision-drivers.md) | Gathering decision criteria | Quality attributes, constraints, stakeholder concerns |
| [Status Lifecycle](references/status-lifecycle.md) | Proposed → Accepted → Deprecated | Status transitions, superseding rules, amendment process |
| [ADR Examples](references/adr-examples.md) | Real-world ADR examples | .NET/fintech examples: CQRS, auth, database choices |

## Quick Reference

Minimal ADR skeleton for fast drafting:

```markdown
# ADR-NNNN: Title

**Date:** YYYY-MM-DD
**Status:** Proposed

## Context and Problem Statement

{What is the issue? Why does a decision need to be made?}

## Decision Drivers

- {driver 1}
- {driver 2}

## Considered Options

1. {Option A}
2. {Option B}

## Decision Outcome

Chosen option: "{Option X}", because {justification}.

### Consequences

- Good, because {positive outcome}
- Bad, because {trade-off accepted}
```

## Constraints

### MUST DO

- Follow the MADR format (Context, Drivers, Options, Outcome, Consequences)
- Use sequential numbering consistent with existing ADRs in the repository
- Write in clear prose that a new team member can understand without additional context
- Include the date the decision was proposed
- List at least two alternatives considered (including the chosen option)
- State consequences honestly — include trade-offs and risks, not just benefits
- Use a valid status: Proposed, Accepted, Deprecated, or Superseded
- Link to superseding/superseded ADRs bidirectionally

### MUST NOT

- Skip the Context section — it is the most important part for future readers
- Use vague language ("we might", "it could") — be definitive and use active voice
- Omit negative consequences — every decision has trade-offs
- Backfill decisions without marking them as retroactive in the context
- Modify the body of an accepted ADR — create a new superseding ADR instead
- Include implementation details — ADRs capture *what* and *why*, not *how*
- Reuse or skip ADR numbers in the sequence

## Output Template

See [ADR Template](references/adr-template.md) for the full template. Compact version:

```markdown
# ADR-{NNNN}: {Decision Title}

**Date:** {YYYY-MM-DD}
**Status:** {Proposed | Accepted | Deprecated | Superseded by [ADR-XXXX](adr-XXXX-title.md)}
**Deciders:** {team or individuals}

## Context and Problem Statement
{Why is this decision needed? Business drivers, technical constraints, compliance.}

## Decision Drivers
- {Specific, measurable criterion — e.g., "PCI-DSS audit trail requirement"}

## Considered Options
1. **{Option A}** — {one-line summary}
2. **{Option B}** — {one-line summary}

## Decision Outcome
Chosen option: **"{Option X}"**, because {rationale tied to drivers}.

### Consequences
- **Good**, because {benefit}
- **Bad**, because {trade-off}

## Pros and Cons of the Options
### {Option A}
- Good, because {advantage}
- Bad, because {disadvantage}

## Links
- {Related ADRs, issues, external references}
```
