# ADR Status Lifecycle

Status values, transition rules, superseding process, and amendment guidance.

## Status Values

| Status | Badge | Meaning |
|---|---|---|
| **Proposed** | 🟡 | Drafted and open for review |
| **Accepted** | 🟢 | Approved by deciders and in effect |
| **Deprecated** | 🟠 | No longer relevant — context changed, no replacement needed |
| **Superseded** | 🔴 | Replaced by a newer ADR (must link to successor) |

## Transition Rules

| Transition | Who | When | Requirements |
|---|---|---|---|
| Proposed → Accepted | Deciders | Team consensus reached | All sections complete; ≥ 2 options; consequences include trade-offs |
| Proposed → Rejected | Author or deciders | Decision no longer needed | Optional: keep file to record why approach was rejected |
| Accepted → Deprecated | Architecture owner | Context no longer exists | Deprecation reason documented; no replacement needed |
| Accepted → Superseded | New ADR author | New decision replaces this one | New ADR exists; bidirectional links in place |

## How to Supersede an ADR

### Step 1 — Create the new ADR

Reference the old ADR in the Context section:

```markdown
---
adr: 0015
title: "Migrate from SQL Server to PostgreSQL for Escrow Ledger"
date: 2025-03-15
status: Proposed
supersedes: ADR-0003
---

# ADR-0015: Migrate from SQL Server to PostgreSQL for Escrow Ledger

## Context and Problem Statement

This decision supersedes [ADR-0003](adr-0003-use-sql-server-for-escrow-data-store.md).
Since ADR-0003 was accepted, platform requirements have evolved: ...
```

### Step 2 — Update the old ADR

Change **only** the status header and frontmatter — never modify the body:

```markdown
---
adr: 0003
title: "Use SQL Server for Escrow Data Store"
date: 2024-06-01
status: Superseded
superseded-by: ADR-0015
---

# ADR-0003: Use SQL Server for Escrow Data Store

**Status:** Superseded by [ADR-0015](adr-0015-migrate-to-postgresql.md)

(... original body unchanged ...)
```

### Superseding Rules

- **Never modify the body** of a superseded ADR
- **Always link bidirectionally** — new references old, old references new
- **Explain what changed** — the new ADR must describe why the original no longer applies
- **Preserve the original date**

## Amendment vs. New ADR

| Scenario | Action |
|---|---|
| Different technology, pattern, or provider | New superseding ADR |
| Significant scope change | New superseding ADR |
| Minor clarification (no decision change) | Amend with dated note |
| Factual error correction | Amend with correction note |
| Implementation details | Separate design doc (not an amendment) |

### Amendment Format

```markdown
## Amendments

### 2025-04-10 — Clarification on retry policy
The Polly retry policy (3 retries, exponential backoff, 30s max) also
applies to the new payment gateway integration.
*Amended by: @developer-name*
```

## Index Badges

```markdown
| ADR | Title | Status | Date |
|---|---|---|---|
| [ADR-0001](adr-0001-use-clean-architecture.md) | Use Clean Architecture | 🟢 Accepted | 2024-01-15 |
| [ADR-0003](adr-0003-use-sql-server.md) | Use SQL Server | 🔴 Superseded | 2024-06-01 |
| [ADR-0015](adr-0015-migrate-to-postgresql.md) | Migrate to PostgreSQL | 🟢 Accepted | 2025-03-15 |
```
