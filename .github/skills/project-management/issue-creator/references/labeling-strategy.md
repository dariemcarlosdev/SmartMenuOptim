# Labeling Strategy

Label taxonomy for consistent GitHub issue organization.

## Label Categories

### Type Labels (Required — exactly one per issue)

| Label | Color | Description |
|-------|-------|-------------|
| `feature` | `#0E8A16` | New functionality or enhancement |
| `bug` | `#D93F0B` | Something is broken |
| `chore` | `#FBCA04` | Refactoring, dependencies, CI/CD |
| `documentation` | `#0075CA` | Documentation changes only |
| `spike` | `#C5DEF5` | Time-boxed investigation |

### Priority Labels (Required — exactly one per issue)

| Label | Color | Meaning | SLA |
|-------|-------|---------|-----|
| `P0-critical` | `#B60205` | Production broken, immediate action | Same day |
| `P1-high` | `#D93F0B` | Blocks sprint or affects many users | This sprint |
| `P2-medium` | `#FBCA04` | Important, plan for next sprint | Next sprint |
| `P3-low` | `#0E8A16` | Nice to have, backlog | When capacity allows |

### Scope Labels (Recommended — one or more)

| Label | Description |
|-------|-------------|
| `domain` | Domain layer (entities, value objects, events) |
| `application` | Application layer (commands, queries, handlers) |
| `infrastructure` | Infrastructure (EF Core, external APIs, messaging) |
| `presentation` | UI/Blazor or API controllers |
| `security` | Authentication, authorization, data protection |
| `performance` | Latency, throughput, resource optimization |

### Status Labels (For workflow tracking)

| Label | Description |
|-------|-------------|
| `needs-investigation` | Requires analysis before implementation |
| `needs-design` | Requires technical design/spec |
| `ready` | Ready to be picked up |
| `blocked` | Cannot proceed (document why in comment) |
| `good-first-issue` | Suitable for new team members |

### Feature Area Labels (NexTruzt-specific)

| Label | Description |
|-------|-------------|
| `escrow` | Escrow lifecycle and management |
| `payments` | Payment processing and gateway integration |
| `auth` | Authentication and authorization |
| `notifications` | Email, SMS, push notifications |
| `reporting` | Reports, dashboards, analytics |
| `admin` | Admin portal functionality |

## Labeling Rules

### MUST DO

```
1. Every issue has exactly ONE type label (feature/bug/chore/documentation/spike)
2. Every issue has exactly ONE priority label (P0-P3)
3. Bug reports always include the `bug` type label
4. Security-related issues always include the `security` scope label
5. Labels are applied at creation time, not retroactively
```

### MUST NOT

```
1. Never use more than one type label per issue
2. Never use more than one priority label per issue
3. Never create ad-hoc labels — use the taxonomy above
4. Never use labels as the only status tracking (use project boards)
```

## Label Assignment Matrix

| Issue Type | Type | Priority | Scope (typical) |
|-----------|------|----------|-----------------|
| New API endpoint | `feature` | P1-P3 | `application`, `presentation` |
| Production error | `bug` | P0-P1 | varies |
| NuGet upgrade | `chore` | P2-P3 | `infrastructure` |
| Auth vulnerability | `bug` | P0-P1 | `security` |
| New Blazor page | `feature` | P2-P3 | `presentation` |
| DB migration | `chore` | P2 | `infrastructure`, `domain` |

## Automation Integration

```yaml
# .github/labeler.yml — auto-label PRs by path
domain:
  - changed-files:
    - any-glob-to-any-file: 'src/Domain/**'

infrastructure:
  - changed-files:
    - any-glob-to-any-file: 'src/Infrastructure/**'

presentation:
  - changed-files:
    - any-glob-to-any-file: 'src/Web/**'
```
