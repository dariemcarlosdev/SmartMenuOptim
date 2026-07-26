---
name: threat-modeler
description: "STRIDE-based threat modeling with data flow diagrams, DREAD scoring, and mitigation priorities — triggered by 'threat model', 'model threats', 'attack surface analysis'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: security
  triggers: threat model, model threats, attack surface analysis, threat assessment, STRIDE analysis, security architecture, risk assessment, data flow analysis
  role: expert
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: owasp-audit, secret-scanner, code-reviewer
---

# Threat Model Analyst

A structured threat modeling skill using STRIDE methodology with DREAD risk scoring. Analyzes system architecture, maps data flows, identifies trust boundaries, applies STRIDE per component, scores risks, and prioritizes mitigations.

## When to Use This Skill

- "Create a threat model for this system"
- "What are the security threats to this application?"
- "Analyze the attack surface"
- "STRIDE analysis for this architecture"
- Before designing a new system or feature
- When preparing for a security audit or pentest

## Core Workflow

1. **Map Architecture & Data Flows** — Catalog all components (clients, servers, stores, external services, brokers). Map data flows with classification, protocol, auth, and encryption. Create ASCII data flow diagram. Load `references/data-flow-diagrams.md` for DFD conventions.
   - **Checkpoint:** All components inventoried, all data flows documented, DFD created.

2. **Identify Trust Boundaries** — Mark every transition where trust level changes (Internet→DMZ, DMZ→Internal, App→DB, User→Admin). Every flow crossing a boundary is an attack vector.
   - **Checkpoint:** All trust boundaries identified and risk-ranked.

3. **Apply STRIDE Per Component** — For each component and data flow, evaluate all six categories: Spoofing, Tampering, Repudiation, Information Disclosure, Denial of Service, Elevation of Privilege. Load `references/stride-analysis.md` for category-specific questions and patterns.
   - **Checkpoint:** All six STRIDE categories evaluated for every component.

4. **Score with DREAD** — Calculate DREAD score (Damage, Reproducibility, Exploitability, Affected Users, Discoverability) for each threat. Load `references/dread-scoring.md` for scoring rubric.
   - **Checkpoint:** All threats scored and risk-ranked (Critical/High/Medium/Low).

5. **Prioritize Mitigations** — Rank by impact × effort. Create phased roadmap: Immediate (Critical), Short-term (High), Medium-term (Medium), Ongoing. Load `references/mitigation-catalog.md` for countermeasure selection.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| STRIDE Analysis | `references/stride-analysis.md` | Categorizing threats |
| DREAD Scoring | `references/dread-scoring.md` | Risk assessment scoring |
| Data Flow Diagrams | `references/data-flow-diagrams.md` | Creating DFDs |
| Mitigation Catalog | `references/mitigation-catalog.md` | Selecting countermeasures |

## Quick Reference

```
STRIDE: Spoofing | Tampering | Repudiation | Info Disclosure | DoS | Elevation of Privilege
DREAD:  Damage(1-3) + Reproducibility(1-3) + Exploitability(1-3) + Affected(1-3) + Discoverability(1-3)
        12-15 = Critical | 8-11 = High | 5-7 = Medium | 1-4 = Low
```

| STRIDE | Question | Example Threat |
|--------|----------|---------------|
| **S** | Can attacker impersonate? | Forged JWT, stolen session |
| **T** | Can data be modified? | Request tampering, SQL injection |
| **R** | Can action be denied? | No audit trail for transactions |
| **I** | Can data leak? | PII in logs, verbose errors |
| **D** | Can service be disrupted? | API flooding, connection exhaustion |
| **E** | Can privileges escalate? | User accessing admin endpoints |

## Constraints

### MUST DO
- Map ALL components and data flows before applying STRIDE
- Identify ALL trust boundaries explicitly
- Evaluate ALL six STRIDE categories for each component
- Calculate DREAD scores for every threat identified
- Provide specific, actionable mitigations (not generic advice)
- Include an ASCII data flow diagram
- Prioritize mitigations with a phased roadmap
- Consider both external attackers and malicious insiders
- Document assumptions made during analysis

### MUST NOT
- Do not skip STRIDE categories — document why if N/A
- Do not assign arbitrary DREAD scores — justify each
- Do not propose mitigations without considering feasibility
- Do not ignore business context (blog vs. payment system)
- Do not copy generic threat lists — tailor to actual system
- Do not assume infrastructure security — question it

## Output Template

```markdown
# Threat Model Document

**System:** [name]  |  **Date:** YYYY-MM-DD  |  **Analyst:** AI Threat Model Analyst

## System Overview
## Architecture Diagram (ASCII DFD with trust boundaries)
## Component Inventory
| ID | Component | Type | Technology | Data Sensitivity | Trust Level |
## Data Flow Catalog
| ID | Source | Dest | Data | Protocol | Auth | Encryption |
## Trust Boundaries
| ID | Boundary | Components | Risk Level |
## STRIDE Analysis (per component)
| STRIDE | Threat | Description | DREAD Score | Risk |
## Threat Summary (all threats ranked)
## Mitigation Roadmap (Phase 1: Immediate → Phase 4: Ongoing)
## Assumptions and Limitations
```
