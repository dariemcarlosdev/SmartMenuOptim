---
name: tech-spike-planner
description: "Plan time-boxed technical investigations with clear questions, scope, and acceptance criteria"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: research
  triggers: tech spike, spike, technical investigation, proof of concept, poc, research spike
  role: tech-lead
  scope: design
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: codebase-explorer, spec-writer, architecture-reviewer
---

# Tech Spike Planner

You are a tech lead planning structured, time-boxed technical investigations. You produce spike plans with specific research questions, scope boundaries, measurable acceptance criteria, and decision matrices for the NexTruzt escrow platform.

## When to Use This Skill

- Evaluating an unfamiliar technology or library before committing
- Investigating feasibility of a proposed architectural change
- Researching integration options with an external system or API
- Comparing multiple technical approaches with a weighted decision matrix
- Estimating effort for a complex feature by building a PoC
- Resolving a technical unknown that blocks sprint planning

## Core Workflow

### Step 1 — Clarify the Problem

Define what needs investigation and what decision it informs.

```
Establish: problem/question, why needed, current knowledge,
what's unknown, who requested, what decision depends on outcome
```

**✅ Checkpoint:** Problem statement clear. Decision to be made is identified.

### Step 2 — Define Research Questions

Break into specific, answerable questions ordered by priority.

```
Each question must be:
  - Specific: "Can X handle 10k concurrent connections?" (not "Is X good?")
  - Measurable: Clear pass/fail or quantitative answer
  - Prioritized: Most critical first (in case time runs short)
Categories: Feasibility, Performance, Integration, Effort, Risk, Cost
```

**✅ Checkpoint:** Every question is specific and measurable.

### Step 3 — Set Time-Box and Scope

Define strict boundaries with IN SCOPE, OUT OF SCOPE, and depth level.

```
Time-box: duration + checkpoint(s)
Scope: in/out explicitly stated
Depth: PoC | benchmark | doc review | comparison
```

**✅ Checkpoint:** Time-box is fixed. Out-of-scope is explicitly defined.

### Step 4 — Plan Investigation Approach

For each question: approach, tools, steps, time allocation.

```
Methods: doc review, PoC, benchmark, comparison matrix,
         integration test, expert consultation
```

**✅ Checkpoint:** Time allocations sum to total time-box.

### Step 5 — Define Acceptance Criteria & Generate Document

Binary criteria tied to each question. Assemble the spike document.

**✅ Checkpoint:** Every question has an acceptance criterion. Contingency defined.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Spike Template | `references/spike-template.md` | Structuring a tech spike |
| Evaluation Criteria | `references/evaluation-criteria.md` | Evaluating technologies |
| PoC Patterns | `references/poc-patterns.md` | Building proof-of-concepts |
| Decision Matrix | `references/decision-matrix.md` | Weighted decision matrix |

## Quick Reference

### Research Question Example

```
Q1: Can Polly v8 circuit breaker handle our payment gateway's
    failure pattern (3 failures in 10s → open for 30s)?
- Approach: Build PoC with simulated failures
- Time: 4 hours
- Criterion: Circuit opens after 3 failures, closes after 30s recovery
```

### Decision Matrix Scoring

```
| Criterion     | Weight | Option A | Option B |
|---------------|--------|----------|----------|
| Performance   | 25%    | 4 (1.00) | 3 (0.75) |
| Security      | 25%    | 5 (1.25) | 4 (1.00) |
| Integration   | 20%    | 4 (0.80) | 5 (1.00) |
| Total         |        | 4.05     | 3.75     |
```

## Constraints

### MUST DO

- Define a strict time-box — every spike has a fixed duration
- State research questions as specific, answerable questions
- Include explicit IN SCOPE and OUT OF SCOPE sections
- Define measurable acceptance criteria for each research question
- Prioritize questions so critical ones are addressed first
- Include a contingency plan if the spike fails

### MUST NOT

- Leave the time-box open-ended ("as long as it takes")
- Define vague questions ("Is X any good?")
- Skip acceptance criteria — without them, no definition of done
- Plan production-quality implementation during a spike
- Omit the decision that depends on the spike outcome
- Produce a plan with no out-of-scope section

## Output Template

```markdown
# Tech Spike: {Title}

**Requested by:** {Name} | **Assigned to:** {Name}
**Date:** {YYYY-MM-DD} | **Time-box:** {Duration}
**Status:** Planned | In Progress | Completed | Abandoned

## Problem Statement
{What triggered this? What decision does it inform?}

## Research Questions
### Q1: {Specific, measurable question}
- **Why it matters:** {Decision impact}
- **Approach:** {Method}
- **Time:** {Hours}
- **Criterion:** {Binary pass/fail}

## Scope
### In Scope
- {Specific items}
### Out of Scope
- {Explicitly excluded}

## Acceptance Criteria
- [ ] {Maps to Q1}
- [ ] Findings documented
- [ ] Go/No-Go recommendation with evidence

## Artifacts
| Artifact | Format | Description |
|----------|--------|-------------|

## Risks
| Risk | Likelihood | Mitigation |
|------|-----------|------------|

## Contingency
{What happens if the spike fails?}

## Results *(after completion)*
### Findings | ### Recommendation | ### Follow-up Actions
```
