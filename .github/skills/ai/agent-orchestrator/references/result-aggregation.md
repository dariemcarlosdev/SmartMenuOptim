# Result Aggregation Reference

> **Load when:** Collecting and merging agent outputs, validating results, resolving conflicts.

## Collection Patterns

### Pattern 1: Notification-Driven Collection (Preferred)

Wait for agent completion notifications rather than polling.

```
Orchestrator                 Agent Fleet
    │                           │
    ├── Launch Agent 1 ────────►│
    ├── Launch Agent 2 ────────►│
    ├── Launch Agent 3 ────────►│
    │                           │
    │◄──── Agent 2 complete ────┤  (notification)
    │   → read_agent(agent_2)   │
    │                           │
    │◄──── Agent 1 complete ────┤  (notification)
    │   → read_agent(agent_1)   │
    │                           │
    │◄──── Agent 3 complete ────┤  (notification)
    │   → read_agent(agent_3)   │
    │                           │
    ├── Aggregate & Report ─────►
```

**Implementation:**
```
1. Launch all agents with mode: "background"
2. Continue other work (or wait)
3. As notifications arrive, read_agent with since_turn for incremental output
4. After all complete, aggregate results
```

### Pattern 2: Sequential Read with Since_Turn

For ordered collection where you need results incrementally.

```
# Read agent 1 (full output)
read_agent(agent_1, since_turn: 0)  → turns 1, 2, 3

# Read agent 2 (full output)
read_agent(agent_2, since_turn: 0)  → turns 1, 2

# Follow up with agent 1 (only new output since turn 3)
write_agent(agent_1, "Also check for X")
read_agent(agent_1, since_turn: 3)  → turns 4, 5
```

## Validation Checklist

Run after collecting all agent results and before merging.

### File Validation

```markdown
□ All files reported as "created" exist on disk
  → Verify with: glob for each created file path
□ All files reported as "modified" have expected changes
  → Verify with: view the file, check for expected content
□ No unexpected files were created or modified
  → Verify with: git status to see all changes
□ No files were deleted that shouldn't have been
  → Verify with: git status for deleted files
```

### Code Validation

```markdown
□ Project builds without errors
  → Run: dotnet build --no-restore
□ All tests pass
  → Run: dotnet test --no-build
□ No new compiler warnings introduced
  → Check build output for warnings
□ Code follows project conventions
  → Check: file-scoped namespaces, sealed classes, nullable annotations
```

### Conflict Detection

```markdown
□ No two agents modified the same file
  → Cross-reference file lists from each agent
□ No two agents created files in the same directory with conflicting names
  → Check for naming collisions
□ No circular dependencies introduced between new files
  → Verify using dependency analysis
□ Using statements are consistent across new files
  → Check for missing or conflicting imports
```

## Merge Strategies

### Strategy 1: Append (No Overlap)

When agents produce results for different, non-overlapping areas.

```
Agent 1 result: Auth module analysis → Section 1 of report
Agent 2 result: Escrow module analysis → Section 2 of report
Agent 3 result: Payment module analysis → Section 3 of report

Merged report = Section 1 + Section 2 + Section 3
```

### Strategy 2: Deduplicate (Overlapping Findings)

When agents may discover the same issues from different perspectives.

```
Agent 1 findings: [A, B, C, D]
Agent 2 findings: [C, D, E, F]  (C, D overlap with Agent 1)

Deduplication:
  1. Match by file path + line number + issue type
  2. Keep the more detailed description
  3. Merged: [A, B, C (agent 1 version), D (agent 2 version), E, F]
```

### Strategy 3: Conflict Resolution (Same File Modified)

When two agents modify the same file (should be avoided, but handle gracefully).

```
Both agents modified EscrowService.cs:
  Agent 1: Added ReleaseAsync() method
  Agent 2: Added CancelAsync() method

Resolution options:
  1. If changes are in different methods → manually merge both
  2. If changes conflict → use critic agent to pick the better version
  3. If changes are incompatible → take the one that matches the higher-priority task
```

## Error Handling

### Agent Failure Recovery

| Failure Type | Detection | Recovery Action |
|---|---|---|
| Agent timeout | Status remains "running" past deadline | Stop agent, read partial output, retry with simpler prompt |
| Agent error | Status is "failed" | Read error message, fix prompt, retry once |
| Partial result | Output missing expected sections | Write follow-up to agent requesting missing parts |
| Invalid output | Doesn't match expected format | Parse what's available, fill gaps manually |
| All agents fail | Multiple failures in wave | Fall back to doing the work yourself |

### Retry Protocol

```
Attempt 1: Original prompt (full context)
  → If fails:
Attempt 2: Simplified prompt (reduced scope, more explicit instructions)
  → If fails:
Manual Takeover: Do it yourself using the agent's partial output as a starting point
```

## Reporting Template

### Summary Report

```markdown
## Orchestration Results

**Task:** {original_request}
**Strategy:** {parallel|serial|hybrid}
**Agents:** {completed}/{total} succeeded
**Duration:** {total_time}

### Results by Agent

| Agent | Type | Status | Key Output |
|---|---|---|---|
| {agent_id} | {type} | ✅ Done | {1-line summary} |
| {agent_id} | {type} | ❌ Failed | {failure_reason} |

### Merged Findings

{deduplicated, ordered findings from all agents}

### Files Changed

| File | Action | Agent | Validated |
|---|---|---|---|
| {path} | Created | {agent_id} | ✅ |
| {path} | Modified | {agent_id} | ✅ |

### Validation Status

- Build: ✅ Pass / ❌ Fail
- Tests: ✅ {n} passed / ❌ {n} failed
- Conflicts: ✅ None / ⚠️ {n} resolved

### Issues & Recovery

| Issue | Resolution |
|---|---|
| {problem} | {how_resolved} |
```

## Quality Gates

Before delivering merged results to the user, all gates must pass:

```
Gate 1: All agents completed (or failures handled)     □
Gate 2: All created/modified files validated             □
Gate 3: Build passes                                     □
Gate 4: Tests pass                                       □
Gate 5: No unresolved conflicts                          □
Gate 6: Token budget not exceeded                        □
Gate 7: Summary covers all work units                    □
```
