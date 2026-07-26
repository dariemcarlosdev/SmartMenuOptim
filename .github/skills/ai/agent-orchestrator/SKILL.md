---
name: agent-orchestrator
description: "Orchestrates parallel sub-agent fleets with token-aware delegation, context minimization, DAG-based dependency management, and structured result aggregation. Enforces memory optimization rules across multi-agent workflows. Use when coordinating multiple AI agents, managing parallel tasks, optimizing token consumption across agent fleets."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: workflow
  triggers: orchestrate agents, parallel tasks, multi-agent, fleet management, agent coordination, delegate tasks, token budget, agent workflow
  role: expert
  scope: design
  platforms: copilot-cli, claude, gemini
  output-format: analysis
  related-skills: prompt-engineer, mcp-developer, codebase-explorer
---

# Agent Orchestrator

An expert orchestration engine that decomposes complex tasks into parallel sub-agent work units, manages token budgets across agent fleets, tracks dependencies via DAG-based scheduling, minimizes context duplication, and aggregates results — enforcing memory optimization principles throughout the multi-agent workflow.

## When to Use This Skill

- A user request naturally decomposes into 3+ independent work units that benefit from parallelism
- Multiple areas of a codebase must be analyzed, modified, or tested simultaneously
- Token consumption across agents must be tracked and optimized to stay within budget
- Task dependencies form a directed acyclic graph (DAG) requiring ordered execution
- Results from multiple agents must be validated, merged, and deduplicated before delivery
- A complex feature implementation spans many files across multiple architectural layers
- Error recovery is needed when sub-agents fail or produce partial results
- The orchestrator must decide between parallel delegation, serial execution, or doing it yourself

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Delegation Patterns | `references/delegation-patterns.md` | Deciding parallel vs serial, agent type selection, **approval gate format** |
| Context Minimization | `references/context-minimization.md` | Writing agent prompts, reducing token waste |
| Token Budget Allocation | `references/token-budget-allocation.md` | Planning fleet-wide token usage |
| Result Aggregation | `references/result-aggregation.md` | Collecting and merging agent outputs |
| DAG Dependency Management | `references/dag-dependency-management.md` | Managing task dependencies between agents |

## Core Workflow

### Step 1 — Analyze Task and Decompose

Break the user request into independent work units and identify dependencies.

1. **Parse the request** — Identify the top-level goal and all sub-goals.
2. **Decompose into work units** — Each unit must be independently executable by a single agent.
3. **Classify independence** — Mark each pair of units as: `independent` (parallelizable), `dependent` (must sequence), or `overlapping` (needs dedup).
4. **Build the dependency DAG** — Create edges from prerequisites to dependents.
5. **Estimate complexity** — Assign each unit a t-shirt size (S/M/L/XL) for token budget planning.

**Delegation Decision Tree:**

```
Is the task a single, simple lookup?
  → YES: Do it yourself (grep/glob/view). No agent needed.
  → NO: Does it decompose into 3+ independent units?
    → YES: Delegate to parallel agents.
    → NO: Is it complex multi-step reasoning?
      → YES: Delegate to a single general-purpose agent.
      → NO: Do it yourself — agent overhead exceeds benefit.
```

**✅ Checkpoint: Work units defined, independence verified (no unit reads another's output), DAG has no cycles, complexity estimated.**

### Step 2 — Plan the Fleet

Determine agent count, types, and token budget allocation.

1. **Select agent types** per work unit:
   - **explore** — Read-only research, codebase analysis, finding patterns (~20K tokens)
   - **task** — Build, test, lint, install — success/failure output only (~15K tokens)
   - **general-purpose** — Complex multi-step implementation, full toolset (~50K tokens)
   - **critic** — Validate plans, catch bugs, review implementations (~30K tokens)
2. **Allocate token budget** — Total budget = context window × 0.8. Divide across agents by complexity.
3. **Set blast radius** — Define what each agent is allowed to touch (files, directories, commands).
4. **Plan error recovery** — For each agent: retry policy, fallback strategy, manual takeover threshold.

**Token Budget Formula:**

```
Per-agent budget = (Total available × Unit complexity weight) / Sum of all weights
- S = 1 weight, M = 2, L = 3, XL = 5
- Reserve 20% for aggregation and follow-up
```

**✅ Checkpoint: Agent types assigned, token budget allocated (sum ≤ 80% of total), blast radius defined, error recovery planned.**

### Step 3 — Present Delegation Plan and Get User Approval

**⛔ MANDATORY GATE — Never skip this step.**

Before spawning ANY sub-agent, present the full delegation plan to the user using `ask_user` and wait for explicit approval.

1. **Build the delegation summary** — Create a clear, visual plan showing:
   - Total number of agents to be spawned
   - For each agent: name, type, task description, blast radius, estimated token budget
   - Dependency graph (which agents wait for which)
   - Wave breakdown (which agents run in parallel vs serial)
2. **Present to user with `ask_user`** — Use a structured form with:
   - The delegation plan as the message
   - An approval boolean: "Approve this delegation plan?"
   - Optional: allow the user to exclude specific agents or modify the plan
3. **Handle the response:**
   - **Approved** → Proceed to Step 4 (Minimize Context)
   - **Declined** → Ask what to change, rebuild the plan, re-present
   - **Cancelled** → Stop orchestration entirely

**Delegation Plan Format (present this to user):**

```markdown
## 🤖 Delegation Plan

**Goal:** {user_request_summary}
**Strategy:** {parallel | serial | hybrid}
**Total Agents:** {count} | **Estimated Tokens:** {budget}

### Wave 1 (Parallel)
| # | Agent Name | Type | Task | Files/Scope |
|---|-----------|------|------|-------------|
| 1 | {name} | explore | {what it will do} | {blast radius} |
| 2 | {name} | general-purpose | {what it will do} | {blast radius} |

### Wave 2 (After Wave 1 completes)
| # | Agent Name | Type | Task | Depends On |
|---|-----------|------|------|-----------|
| 3 | {name} | critic | {what it will do} | Agent 1, 2 |

### Dependency Graph
Agent 1 ──→ Agent 3
Agent 2 ──↗
```

**✅ Checkpoint: User has explicitly approved the delegation plan. Do NOT proceed without approval.**

### Step 4 — Minimize Context Per Agent

1. **Build the common preamble** — Project description, architecture overview, coding conventions. This is shared across all agents (written once, included everywhere).
2. **Write task-specific deltas** — For each agent, add ONLY the context unique to its task: specific file paths, function signatures, test expectations.
3. **Apply context dedup rules:**
   - Never include full file contents if a function signature suffices
   - Never repeat the preamble in the delta — agents get both
   - Use file path references instead of inline code when the agent has file access
4. **Validate prompt completeness** — Each agent must be able to execute its task without asking questions.

**Shared Context Protocol:**

```
Agent Prompt = Common Preamble + Task-Specific Delta + Output Format
                 (shared, ~2K)     (unique, ~1-5K)       (shared, ~500)
```

**✅ Checkpoint: Each agent prompt is self-contained, no duplicate context between agents, total prompt tokens ≤ budget.**

### Step 5 — Dispatch and Monitor

Launch agents in dependency order and track progress.

1. **Initialize tracking** — Insert all work units into SQL todos with dependencies.
2. **Launch wave 1** — Start all agents with no pending dependencies (the "ready" query).
3. **Monitor completion** — As agents complete, update status and check if new units are unblocked.
4. **Launch subsequent waves** — Start newly-ready agents as their dependencies resolve.
5. **Handle failures** — If an agent fails: retry once with refined prompt, then fall back to manual execution.

**SQL Tracking Pattern:**

```sql
-- Insert work units
INSERT INTO todos (id, title, description, status) VALUES
  ('analyze-domain', 'Analyze domain layer', 'Explore domain entities and aggregates', 'pending'),
  ('analyze-infra', 'Analyze infrastructure', 'Review EF Core and external services', 'pending'),
  ('implement-feature', 'Implement feature', 'Create handler and endpoint', 'pending');

-- Insert dependencies
INSERT INTO todo_deps (todo_id, depends_on) VALUES
  ('implement-feature', 'analyze-domain'),
  ('implement-feature', 'analyze-infra');

-- Find ready work units (no pending dependencies)
SELECT t.* FROM todos t
WHERE t.status = 'pending'
AND NOT EXISTS (
    SELECT 1 FROM todo_deps td
    JOIN todos dep ON td.depends_on = dep.id
    WHERE td.todo_id = t.id AND dep.status != 'done'
);
```

**✅ Checkpoint: All independent agents launched in wave 1, dependency tracking active, no agent blocked on incomplete prerequisites.**

### Step 6 — Collect, Aggregate, and Report

Gather results, validate, merge, and deliver to the user.

1. **Collect results** — Use `read_agent` with `since_turn` for incremental reads as agents complete.
2. **Validate each result:**
   - Files created/modified actually exist
   - Code compiles (run build if agents produced code)
   - No conflicting changes between agents (same file modified by multiple agents)
3. **Merge results** — Combine agent outputs in dependency order. Deduplicate overlapping findings.
4. **Resolve conflicts** — If two agents modified the same file, use the critic agent to pick the better version.
5. **Report summary** — Deliver a concise summary with key outcomes, files changed, and any issues.

**Result Validation Checklist:**

```
□ All agent statuses are 'completed' (no failures or timeouts)
□ Created files exist on disk
□ Modified files compile without errors
□ No merge conflicts between agent outputs
□ Test suite still passes after all changes
□ Total token usage is within budget
```

**✅ Checkpoint: All results validated, conflicts resolved, build passes, summary delivered to user.**

## Quick Reference

### Parallel Exploration Pattern

```
User: "Analyze the authentication, escrow, and payment modules"

Orchestrator:
  Steps 1-2: Decompose + plan fleet
  Step 3 — PRESENT TO USER via ask_user:
    "🤖 Delegation Plan
     Goal: Analyze 3 modules
     Wave 1 (parallel):
       - explore-agent-1: Analyze auth module in src/Auth/ (~20K)
       - explore-agent-2: Analyze escrow in src/Escrow/ (~20K)
       - explore-agent-3: Analyze payment in src/Payment/ (~20K)
     Wave 2: general-purpose: Synthesize findings (~30K)
     Total: ~90K tokens. Approve?"
  → User approves → Steps 4-6: Execute, monitor, aggregate
```

### Serial Implementation with Critic

```
User: "Implement the escrow release feature"

Orchestrator:
  Steps 1-2: Decompose + plan fleet
  Step 3 — PRESENT TO USER via ask_user:
    "🤖 Delegation Plan
     Goal: Implement escrow release with approval workflow
     Wave 1: explore → Analyze domain model (~20K)
     Wave 2: critic → Validate plan (~30K)
     Wave 3: general-purpose → Implement handler + tests (~50K)
     Wave 4: task → Build and test (~15K)
     Wave 5: critic → Final review (~30K)
     Total: ~145K tokens. Approve?"
  → User approves → Steps 4-6: Execute waves
```

## Constraints

### MUST DO

- **ALWAYS present the delegation plan to the user and get explicit approval before spawning any sub-agent**
- **ALWAYS show which agents will be created, what each will do, and their blast radius**
- **ALWAYS use `ask_user` tool for the approval gate — do not assume approval from silence**
- Run the delegation decision tree before spawning any agent — avoid unnecessary delegation
- Track all work units in SQL todos with explicit dependencies
- Apply the Shared Context Protocol — common preamble + task-specific delta for every agent
- Allocate token budget before dispatch — never launch agents without a budget plan
- Validate agent results before merging — check file existence, compilation, and conflicts
- Use `since_turn` for incremental reads — avoid re-reading completed turns
- Reserve 20% of token budget for aggregation, follow-up, and error recovery
- Launch independent agents in parallel — never serialize work that can be parallelized
- Use the critic agent for non-trivial plans before implementing

### MUST NOT

- **Do not spawn any agent without user approval — the approval gate in Step 3 is mandatory, never skip it**
- **Do not assume the user approves — always use the `ask_user` tool and wait for a response**
- Do not launch agents for simple lookups — use grep/glob/view directly
- Do not duplicate context between agents — use the Shared Context Protocol
- Do not launch dependent agents before their prerequisites complete
- Do not ignore agent failures — retry once, then fall back to manual execution
- Do not exceed the token budget — track consumption and stop if approaching limits
- Do not let agents modify the same file without conflict resolution
- Do not skip the dependency DAG — untracked dependencies cause race conditions
- Do not launch more than 5 agents in a single wave — diminishing returns and resource contention

## Output Template

```markdown
# Orchestration Report

**Task:** {user_request_summary}
**Strategy:** {parallel|serial|hybrid}
**Agents Dispatched:** {count}
**Total Token Budget:** {budget} | **Used:** {actual}

## Work Unit Breakdown

| # | Work Unit | Agent Type | Status | Tokens | Duration |
|---|-----------|-----------|--------|--------|----------|
| 1 | {unit_name} | {explore|task|general-purpose|critic} | {done|failed|skipped} | {tokens} | {seconds} |

## Dependency Graph

```
{unit_a} ──→ {unit_c}
{unit_b} ──↗
```

## Results Summary

{merged_findings_or_changes}

## Files Changed

| File | Change | Agent |
|---|---|---|
| {path} | {created|modified} | {agent_id} |

## Issues & Recovery

| Issue | Resolution |
|---|---|
| {what_went_wrong} | {how_it_was_resolved} |
```

## Integration Notes

### Copilot CLI
Trigger with: `orchestrate`, `parallel agents`, `delegate tasks`, `fleet management`, `coordinate agents`

### Claude
Include this file in project context. Trigger with: "Orchestrate agents to [complex multi-step task]"

### Gemini
Reference via `GEMINI.md` or direct inclusion. Trigger with: "Coordinate parallel agents for [task]"
