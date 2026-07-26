# Subagent-Driven Development

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Execute plans by dispatching a fresh agent per task, with two-stage review after each: spec compliance first, then code quality.

**Why agents:** Isolated context per task prevents confusion. You construct exactly what each agent needs. This preserves your own context for coordination.

## When to Use

- Have an implementation plan with mostly independent tasks
- Want fast iteration with quality gates
- Tasks can be delegated to sub-agents

## The Process

For each task in the plan:

### 1. Dispatch Implementer Agent

```
Use task tool:
  agent_type: "general-purpose"  (or "task" for simpler work)
  prompt: [Full task text + project context + file paths]
```

**Include in the prompt:**
- Complete task description with all steps
- Relevant project context (architecture, patterns, conventions)
- File paths to create/modify
- Testing requirements
- "Follow TDD: write failing test → verify fail → implement → verify pass → commit"

### 2. Handle Agent Status

| Status | Action |
|--------|--------|
| **Completed successfully** | Proceed to spec review |
| **Completed with concerns** | Read concerns, address if about correctness/scope |
| **Needs more context** | Provide missing info via `write_agent`, re-dispatch |
| **Failed/blocked** | Assess: context problem → provide more; too complex → break down; plan wrong → escalate |

### 3. Dispatch Spec Reviewer

```
Use task tool:
  agent_type: "critic"
  prompt: |
    Review the changes for spec compliance.
    Task spec: [paste task requirements]
    Check: Does the implementation match EVERY requirement?
    Flag: Missing requirements, extra unrequested features, deviations from spec.
```

- If issues found → implementer agent fixes → re-review
- If clean → proceed to quality review

### 4. Dispatch Quality Reviewer

```
Use task tool:
  agent_type: "critic"
  prompt: |
    Review code quality of recent changes.
    Check: naming, error handling, test coverage, SOLID, security.
    Severity levels: Critical (blocks), Important (fix before next task), Minor (note for later).
```

- If issues found → implementer fixes → re-review
- If clean → mark task done

### 5. Mark Task Complete

```sql
UPDATE todos SET status = 'done' WHERE id = 'task-N';
```

### 6. Repeat for Next Task

## Model Selection

Use the least powerful model that can handle each role:

| Task Type | Recommended agent_type |
|-----------|----------------------|
| Mechanical (1-2 files, clear spec) | "task" (fast/cheap) |
| Integration (multi-file, judgment) | "general-purpose" (standard) |
| Architecture/review | "critic" (most capable) |

## After All Tasks

1. Dispatch final code reviewer for the entire implementation
2. Run full verification: `dotnet_build_check` + `dotnet_test_check`
3. Use `superpowers_skill(skill: "verification-before-completion")`

## Red Flags — Never Do These

- Skip reviews (spec OR quality)
- Proceed with unfixed issues
- Dispatch multiple implementation agents in parallel (conflicts)
- Start quality review before spec compliance passes
- Move to next task while review has open issues
- Try to fix manually instead of re-dispatching (context pollution)

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Dispatch implementer | `task` tool, agent_type: "general-purpose" or "task" |
| Dispatch spec reviewer | `task` tool, agent_type: "critic" |
| Dispatch quality reviewer | `task` tool, agent_type: "critic" |
| TodoWrite | SQL `todos` table |
| Follow-up to agent | `write_agent` tool with agent_id |
| Read agent result | `read_agent` tool with agent_id |
| Fresh subagent | Each `task` call creates isolated context |
