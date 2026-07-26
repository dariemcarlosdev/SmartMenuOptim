# Delegation Patterns Reference

> **Load when:** Deciding whether to parallelize, serialize, or do-it-yourself; selecting agent types.

## Delegation Decision Tree

```
Incoming Task
    │
    ▼
Is it a single, simple lookup (1 file, 1 search, 1 known path)?
    │
    ├─ YES → Do it yourself (grep/glob/view). STOP.
    │
    ▼
Does it decompose into 3+ truly independent work units?
    │
    ├─ YES → Does each unit need full toolset + complex reasoning?
    │   │
    │   ├─ YES → Parallel general-purpose agents
    │   │
    │   └─ NO → Is each unit read-only research?
    │       │
    │       ├─ YES → Parallel explore agents (cheapest)
    │       │
    │       └─ NO → Parallel task agents (build/test/lint)
    │
    ▼
Is it a single complex multi-step task?
    │
    ├─ YES → Single general-purpose agent (or do it yourself)
    │
    ▼
Is it a validation/review of existing work?
    │
    ├─ YES → Critic agent
    │
    └─ NO → Do it yourself — agent overhead exceeds benefit. STOP.
```

## Agent Type Selection Matrix

| Task Category | Agent Type | Token Budget | Parallelizable | Example |
|---|---|---|---|---|
| Read-only codebase research | `explore` | ~20K | ✅ Yes (up to 5) | "Find all usages of EscrowService" |
| Build, test, lint, install | `task` | ~15K | ⚠️ Caution (side effects) | "Run dotnet test" |
| Complex multi-step implementation | `general-purpose` | ~50K | ❌ Serialize | "Implement the release handler" |
| Plan/code review, validation | `critic` | ~30K | ❌ Serialize | "Review my implementation plan" |
| Codebase exploration (many modules) | `explore` × N | ~20K each | ✅ Yes | "Analyze 5 services in parallel" |

## Blast Radius Rules

Each agent should have a clearly defined blast radius — the set of files and commands it's allowed to touch.

```
Agent: implement-escrow-release
  Blast Radius:
    READ: src/Domain/**, src/Application/**, src/Infrastructure/**
    WRITE: src/Application/Features/Escrows/ReleaseEscrow/**
    COMMANDS: dotnet build, dotnet test
    FORBIDDEN: Database migrations, NuGet changes, global config
```

**Rules:**
1. **Explore agents** — Read-only blast radius. Cannot create or modify files.
2. **Task agents** — Can run commands but scope to specific project/directory.
3. **General-purpose agents** — Can modify files but only within their assigned feature slice.
4. **Critic agents** — Read-only. Output is feedback text, not code changes.

## Parallel vs Serial Decision

| Criterion | Parallel | Serial |
|---|---|---|
| Units share no state | ✅ Parallel | |
| Unit B reads Unit A's output | | ✅ Serial |
| Units modify different files | ✅ Parallel | |
| Units modify the same file | | ✅ Serial (or split file) |
| Units can run in any order | ✅ Parallel | |
| Order matters for correctness | | ✅ Serial |
| Total agents ≤ 5 | ✅ Parallel | |
| Total agents > 5 | | ✅ Batch in waves of 5 |

## Real-World Delegation Examples

### Example 1: Multi-Module Analysis

```
User: "Review the auth, escrow, and payment modules for security issues"

Decomposition:
  Unit 1: Analyze auth module → explore agent → independent ✅
  Unit 2: Analyze escrow module → explore agent → independent ✅
  Unit 3: Analyze payment module → explore agent → independent ✅
  Unit 4: Synthesize findings → general-purpose → depends on 1, 2, 3

Strategy: Wave 1 (parallel: 1, 2, 3) → Wave 2 (serial: 4)
```

### Example 2: Feature Implementation

```
User: "Implement escrow release with approval workflow"

Decomposition:
  Unit 1: Explore existing domain model → explore agent
  Unit 2: Design implementation plan → yourself (small task)
  Unit 3: Get critic review of plan → critic agent → depends on 1, 2
  Unit 4: Implement the feature → general-purpose → depends on 3
  Unit 5: Run tests → task agent → depends on 4
  Unit 6: Final review → critic agent → depends on 5

Strategy: Mostly serial — each step depends on the previous.
           Unit 1 can run in background while you do Unit 2.
```

### Example 3: Don't Delegate (Simple Lookup)

```
User: "What does the EscrowService.Release() method do?"

Decision: Do it yourself.
  - Single file lookup
  - grep for "Release" in EscrowService
  - Read the method
  - No agent needed — overhead exceeds benefit
```

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|---|---|---|
| Delegating single lookups | Agent startup cost > task cost | Use grep/glob/view directly |
| Over-parallelizing dependent tasks | Race conditions, wasted work | Build dependency DAG first |
| Launching agents "just in case" | Wastes tokens and resources | Only delegate when decision tree says YES |
| Duplicate context in every agent | Multiplied token cost | Use Shared Context Protocol |
| No blast radius definition | Agents can conflict or break things | Define explicit read/write scopes |
| **Spawning agents without user approval** | **User loses control, wasted budget** | **Always present delegation plan and get explicit approval** |

## Approval Gate Pattern

The approval gate is a **mandatory governance checkpoint** between planning and execution.
It ensures the user always knows:

1. **What** agents will be spawned
2. **Why** each agent is needed
3. **What** each agent is allowed to touch
4. **How many** tokens the fleet will consume

### Implementation

```
Step 1: Decompose → Step 2: Plan Fleet → ⛔ GATE: Present to User → Step 4: Execute
                                             │
                                             ├─ Approved → Continue
                                             ├─ Declined → Revise plan, re-present
                                             └─ Cancelled → Stop entirely
```

### `ask_user` Format

Use the `ask_user` tool with a clear summary in `message` and a boolean approval field:

```json
{
  "message": "## 🤖 Delegation Plan\n\n**Goal:** ...\n**Agents:** 3 parallel explore + 1 serial general-purpose\n\n| # | Agent | Type | Task | Scope |\n|---|-------|------|------|-------|\n| 1 | auth-analyzer | explore | Analyze auth module | src/Auth/** |\n| 2 | escrow-analyzer | explore | Analyze escrow module | src/Escrow/** |\n| 3 | payment-analyzer | explore | Analyze payment module | src/Payment/** |\n| 4 | synthesizer | general-purpose | Merge findings | Read-only |\n\n**Estimated tokens:** ~110K\n**Dependency:** Agent 4 waits for 1, 2, 3",
  "requestedSchema": {
    "properties": {
      "approve": {
        "type": "boolean",
        "title": "Approve this delegation plan?",
        "description": "Set to true to proceed with agent delegation, false to revise the plan",
        "default": true
      },
      "notes": {
        "type": "string",
        "title": "Notes (optional)",
        "description": "Any changes you'd like to the plan (remove agents, change scope, etc.)"
      }
    },
    "required": ["approve"]
  }
}
```

### When to Skip the Gate (Never)

There are **no exceptions** to the approval gate. Even for:
- "Quick" 2-agent explorations — still present
- Re-launches after failure — still present (the plan may have changed)
- User said "autopilot" or "go ahead" — still present (they need to see the specific plan)

The only case where agents can be launched without the gate is when the user
**explicitly typed the agent command themselves** (e.g., manually calling the task tool).
