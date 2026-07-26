# Spike Template

Structured template for planning time-boxed technical investigations.

## Spike Document Template

```markdown
# Tech Spike: {Title}

**Requested by:** {Name / Team}
**Assigned to:** {Name(s)}
**Date:** {YYYY-MM-DD}
**Time-box:** {Duration, e.g., "2 days (16 hours)"}
**Status:** Planned | In Progress | Completed | Abandoned

## Problem Statement

{What triggered this spike? What decision does it inform?
Current state vs. desired state. What's unknown?}

## Research Questions

*Ordered by priority — address Q1 first.*

### Q1: {Specific, measurable question}
- **Why it matters:** {Decision impact}
- **Approach:** {Investigation method}
- **Time allocation:** {Hours or % of time-box}
- **Acceptance criterion:** {Binary pass/fail condition}

### Q2: {Specific, measurable question}
- **Why it matters:** {Decision impact}
- **Approach:** {Investigation method}
- **Time allocation:** {Hours or %}
- **Acceptance criterion:** {Binary condition}

## Scope

### In Scope
- {Specific investigation items}

### Out of Scope
- {Explicitly excluded items}

### Depth
{proof-of-concept | benchmark | documentation review | comparison}

## Investigation Plan

### Prerequisites
- [ ] {Environment, access, tooling needed}

### Steps
1. {Setup}
2. {Q1 investigation}
3. {Midpoint checkpoint}
4. {Q2 investigation}
5. {Document findings}

### Checkpoint
- **When:** {Midpoint}
- **Review:** {What to assess}
- **Decision:** Continue | Pivot | Stop

## Acceptance Criteria
- [ ] {Maps to Q1}
- [ ] {Maps to Q2}
- [ ] Findings documented in spike report
- [ ] Go/No-Go recommendation with evidence

## Expected Output Artifacts

| Artifact           | Format   | Description                    |
|--------------------|----------|--------------------------------|
| Spike Report       | Markdown | Findings and recommendation    |
| Proof-of-Concept   | Code     | Minimal working example        |
| Comparison Matrix  | Table    | Weighted scoring (if comparing)|
| ADR                | Markdown | Decision record (if decided)   |

## Risks to the Spike

| Risk                         | Likelihood | Mitigation             |
|------------------------------|------------|------------------------|
| {Access not available}       | {H/M/L}   | {Fallback}             |
| {More complex than expected} | {H/M/L}   | {Reduce scope}         |

## Contingency

**If the spike fails:** {Extend? Different approach? Default choice?}

## Results *(filled after completion)*

### Findings
{Summary per research question}

### Recommendation
{Go / No-Go / Conditional — with justification}

### Follow-up Actions
- [ ] {Action item}
```

## .NET-Specific Spike Starter

For NexTruzt platform spikes, include:

```bash
# Create isolated spike project
dotnet new console -n Spike.{TopicName} --framework net10.0
cd Spike.{TopicName}

# Common spike packages
dotnet add package MediatR
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package FluentValidation
dotnet add package Polly.Extensions.Http
```

## Time-Box Guidelines

| Spike Type | Typical Duration | Depth |
|-----------|-----------------|-------|
| Library evaluation | 2-4 hours | Doc review + hello world |
| Integration feasibility | 1 day | Connect and prove one flow |
| Architecture comparison | 2-3 days | PoC per option + benchmark |
| Performance investigation | 1-2 days | Benchmark with realistic data |
| Security assessment | 1 day | Threat model + config review |
