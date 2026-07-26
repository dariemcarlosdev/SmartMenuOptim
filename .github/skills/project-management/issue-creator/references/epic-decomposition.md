# Epic Decomposition

Breaking large features into implementable, trackable sub-tasks.

## Decomposition Principles

1. **Independently implementable** — Each sub-task can be worked on in isolation
2. **Independently deployable** — Can ship without waiting for others (where possible)
3. **Small enough to review** — 1-3 days of work maximum
4. **Ordered by dependency** — Clear dependency chain, parallelize where possible
5. **Vertically sliced** — Each delivers user-visible value (not horizontal layers)

## Decomposition Strategies

### Vertical Slice (Preferred)

Split by feature behavior, not technical layer:

```
Epic: Escrow Dispute Workflow

BAD (horizontal slices — no value until all done):
  ❌ Create dispute database schema
  ❌ Create dispute domain entities
  ❌ Create dispute API endpoints
  ❌ Create dispute UI pages

GOOD (vertical slices — each delivers value):
  ✅ Buyer can raise a dispute on a funded escrow
  ✅ Admin can view and assign disputes
  ✅ Admin can resolve dispute (refund or release)
  ✅ Email notifications for dispute lifecycle events
```

### By User Role

```
Epic: Escrow Dashboard

Sub-tasks by role:
  1. Buyer dashboard — view my escrows, filter by status
  2. Seller dashboard — view incoming escrows, pending actions
  3. Admin dashboard — view all escrows, search, audit log
  4. Shared components — status badge, amount formatter, pagination
```

### By CRUD Operation

```
Epic: Escrow Management API

Sub-tasks by operation:
  1. Create escrow (POST /api/escrows)
  2. Get escrow by ID (GET /api/escrows/{id})
  3. List escrows with filtering (GET /api/escrows)
  4. Update escrow status (PATCH /api/escrows/{id}/status)
  5. Cancel escrow (DELETE /api/escrows/{id})
```

### By Workflow Step

```
Epic: Escrow Lifecycle

Sub-tasks by lifecycle step:
  1. Escrow creation and validation
  2. Fund deposit and verification
  3. Condition tracking (buyer/seller approval)
  4. Fund release or refund
  5. Escrow closure and audit record
```

## Dependency Mapping

### Notation

```
[A] → [B]  means B depends on A (A must be done first)
[A] | [B]  means A and B can be done in parallel
```

### Example: Escrow Dispute Feature

```
[1. Domain entities & interfaces]
        │
        ├──→ [2. Create dispute handler] ──→ [5. Dispute notifications]
        │
        ├──→ [3. List disputes query]
        │
        └──→ [4. Resolve dispute handler] ──→ [5. Dispute notifications]

Parallel tracks: [2] and [3] can run simultaneously
Blocking: [5] waits for [2] and [4]
```

### GitHub Issue Representation

```markdown
## [Feature] Escrow Dispute Workflow (Epic)

### Sub-Tasks

- [ ] #101 — Domain: Dispute entity and IDisputeRepository (dependency: none)
- [ ] #102 — Command: Raise dispute (dependency: #101)
- [ ] #103 — Query: List disputes with filtering (dependency: #101)
- [ ] #104 — Command: Resolve dispute (dependency: #101)
- [ ] #105 — Notifications: Dispute lifecycle emails (dependency: #102, #104)
- [ ] #106 — UI: Dispute management page (dependency: #103, #104)
```

## Sizing Guide

| Size | Duration | Complexity | Example |
|------|----------|-----------|---------|
| **S** | < 1 day | Single handler/component | Add validation rule |
| **M** | 1-3 days | Cross-layer feature | New CRUD endpoint |
| **L** | 3-5 days | Multi-component feature | New workflow step |
| **XL** | > 5 days | **MUST DECOMPOSE** | Full feature epic |

## Decomposition Checklist

Before finalizing sub-tasks, verify:

- [ ] Each sub-task has a clear definition of done
- [ ] No sub-task exceeds 5 days of estimated effort
- [ ] Dependencies are explicitly mapped
- [ ] At least one sub-task can start immediately (no blockers)
- [ ] Each sub-task has acceptance criteria
- [ ] The sum of sub-tasks covers the full epic scope
- [ ] Sub-tasks are labeled with the parent epic reference

## NexTruzt Clean Architecture Decomposition Pattern

For a typical feature, decompose across architecture layers:

```
1. Domain: Entity + Value Objects + Interface  (S, no dependency)
2. Application: Command + Handler + Validator  (M, depends on #1)
3. Infrastructure: Repository + EF Config      (M, depends on #1)
4. Presentation: Endpoint + DTO mapping        (M, depends on #2)
5. Tests: Unit + Integration                   (M, depends on #2, #3)
6. UI: Blazor page + code-behind               (M, depends on #4)
```
