# DREAD Risk Scoring Guide

Detailed rubric for scoring threats using the DREAD model with justification guidelines.

## DREAD Scoring Matrix

| Factor | Score 1 (Low) | Score 2 (Medium) | Score 3 (High) |
|--------|--------------|------------------|-----------------|
| **D**amage | Minor inconvenience, no data loss | Non-critical data breach, service degradation | Complete data breach, system compromise, financial loss |
| **R**eproducibility | Requires rare conditions, timing-dependent | Reproducible with specific setup or moderate effort | Easily reproducible every time, scripted |
| **E**xploitability | Requires advanced skills, custom tools, insider access | Moderate technical skill, known tools | Trivial — publicly available exploit, no special skills |
| **A**ffected Users | Single user, edge case scenario | Subset of users (e.g., one tenant, one role) | All users, entire platform |
| **D**iscoverability | Hidden, requires insider knowledge or code access | Discoverable via reconnaissance or scanning tools | Publicly visible, documented in error messages |

## Risk Rating Thresholds

| Total Score | Risk Level | Action Timeline |
|-------------|-----------|-----------------|
| **12-15** | **Critical** | Immediate — stop other work, fix now |
| **8-11** | **High** | Current sprint — address before next release |
| **5-7** | **Medium** | Next release — plan for upcoming sprint |
| **1-4** | **Low** | Accept or backlog — address when convenient |

## Scoring Examples for Escrow Platform

### Example 1: SQL Injection in Escrow Query

```
Threat: SQL injection in escrow search endpoint allows data extraction
Component: API → Database data flow

D (Damage):        3 — Complete database extraction, financial data exposed
R (Reproducibility): 3 — Consistent, every request with crafted input
E (Exploitability):  2 — Requires knowledge of SQL and endpoint structure
A (Affected Users):  3 — All users' data at risk
D (Discoverability): 2 — Discoverable via automated scanning tools

TOTAL: 13 → CRITICAL
```

### Example 2: Missing Authorization on Admin Endpoint

```
Threat: Regular user can access escrow admin dashboard
Component: API Endpoint (Elevation of Privilege)

D (Damage):        3 — Can modify any escrow, release funds inappropriately
R (Reproducibility): 3 — Navigate to /admin/escrows, works every time
E (Exploitability):  3 — No special tools needed, just change URL
A (Affected Users):  3 — All platform users affected by admin actions
D (Discoverability): 2 — URL guessable, may appear in JS bundles

TOTAL: 14 → CRITICAL
```

### Example 3: Verbose Error Messages in Production

```
Threat: Stack traces expose internal paths, library versions, SQL queries
Component: API Response (Information Disclosure)

D (Damage):        1 — Information aids further attacks but not directly exploitable
R (Reproducibility): 3 — Every unhandled exception triggers it
E (Exploitability):  1 — Information gathering, not direct exploitation
A (Affected Users):  1 — Affects security posture, not individual users
D (Discoverability): 3 — Visible to any user who triggers an error

TOTAL: 9 → HIGH
```

### Example 4: SignalR Circuit Exhaustion (Blazor Server)

```
Threat: Attacker opens thousands of SignalR connections, exhausting server memory
Component: Blazor Server (Denial of Service)

D (Damage):        2 — Service unavailable, but no data loss
R (Reproducibility): 3 — Scripted, easy to reproduce
E (Exploitability):  3 — Simple script opening WebSocket connections
A (Affected Users):  3 — All platform users lose access
D (Discoverability): 2 — Known Blazor Server limitation, documented

TOTAL: 13 → CRITICAL
```

### Example 5: Audit Log Gaps for Escrow Operations

```
Threat: No audit trail for escrow fund releases, enabling repudiation
Component: Escrow Service (Repudiation)

D (Damage):        2 — Dispute resolution impossible, financial liability
R (Reproducibility): 2 — Only when specific operations lack logging
E (Exploitability):  1 — Requires triggering the specific unlogged operation
A (Affected Users):  1 — Affects specific escrow participants
D (Discoverability): 1 — Requires code review or incident to discover

TOTAL: 7 → MEDIUM
```

## Scoring Best Practices

### DO
- **Justify each score** — don't just assign numbers; explain why
- **Consider the specific system** — a payment platform scores higher on Damage than a blog
- **Use the escrow context** — financial data = higher Damage scores
- **Be conservative** — when in doubt, score higher (it's a security assessment)
- **Consider attack chains** — a Medium finding may enable a Critical one

### DON'T
- Don't assign uniform scores — each factor should be evaluated independently
- Don't score hypothetical threats the same as confirmed code-level findings
- Don't inflate scores to make the report look more alarming
- Don't ignore low-scored items entirely — they may become high when combined

## DREAD Score Comparison Template

```markdown
| # | Threat | D | R | E | A | D | Total | Risk |
|---|--------|---|---|---|---|---|-------|------|
| T1 | SQL injection in search | 3 | 3 | 2 | 3 | 2 | 13 | Critical |
| T2 | Missing admin auth | 3 | 3 | 3 | 3 | 2 | 14 | Critical |
| T3 | Verbose errors | 1 | 3 | 1 | 1 | 3 | 9 | High |
| T4 | Circuit exhaustion | 2 | 3 | 3 | 3 | 2 | 13 | Critical |
| T5 | Audit log gaps | 2 | 2 | 1 | 1 | 1 | 7 | Medium |
```
