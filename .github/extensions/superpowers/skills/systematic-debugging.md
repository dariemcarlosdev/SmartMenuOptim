# Systematic Debugging

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Random fixes waste time and create new bugs. Quick patches mask underlying issues.

**Core principle:** ALWAYS find root cause before attempting fixes.

## The Iron Law

```
NO FIXES WITHOUT ROOT CAUSE INVESTIGATION FIRST
```

If you haven't completed Phase 1, you cannot propose fixes.

## When to Use

Use for ANY technical issue: test failures, bugs, unexpected behavior, performance problems, build failures.

**Use ESPECIALLY when:**
- Under time pressure (emergencies make guessing tempting)
- "Just one quick fix" seems obvious
- You've already tried multiple fixes
- Previous fix didn't work

## Phase 1: Root Cause Investigation

**BEFORE attempting ANY fix:**

1. **Read Error Messages Carefully**
   - Don't skip past errors or warnings
   - Read stack traces completely
   - Note line numbers, file paths, error codes

2. **Reproduce Consistently**
   - Can you trigger it reliably?
   - Exact steps?
   - Every time?

3. **Check Recent Changes**
   - `git --no-pager log --oneline -10`
   - `git --no-pager diff`
   - New dependencies, config changes?

4. **Gather Evidence in Multi-Component Systems**
   For EACH component boundary:
   - Log what data enters/exits the component
   - Verify environment/config propagation
   - Check state at each layer
   - Run once to gather evidence showing WHERE it breaks

5. **Trace Data Flow**
   - Where does the bad value originate?
   - What called this with the bad value?
   - Keep tracing up until you find the source
   - Fix at source, not at symptom

## Phase 2: Pattern Analysis

1. **Find Working Examples** — locate similar working code in same codebase
2. **Compare Against References** — read reference implementations completely, not skimming
3. **Identify Differences** — list every difference, however small
4. **Understand Dependencies** — components, settings, config, environment, assumptions

## Phase 3: Hypothesis and Testing

1. **Form Single Hypothesis** — "I think X is the root cause because Y"
2. **Test Minimally** — smallest possible change, one variable at a time
3. **Verify Before Continuing** — worked → Phase 4; didn't → form NEW hypothesis
4. **When You Don't Know** — say "I don't understand X", ask for help

## Phase 4: Implementation

1. **Create Failing Test** — use `superpowers_skill(skill: "tdd")` for the test
2. **Implement Single Fix** — ONE change, no "while I'm here" improvements
3. **Verify Fix** — test passes, no other tests broken, issue resolved
4. **If Fix Doesn't Work:**
   - Count fixes attempted
   - If < 3: return to Phase 1 with new information
   - **If ≥ 3: STOP — question the architecture**

### 3+ Fixes Failed? Question Architecture

Pattern indicating architectural problem:
- Each fix reveals new coupling/problems elsewhere
- Fixes require "massive refactoring"
- Each fix creates new symptoms

**STOP and discuss with user before attempting more fixes.**

## Red Flags — STOP and Return to Phase 1

- "Quick fix for now, investigate later"
- "Just try changing X and see"
- "Add multiple changes, run tests"
- "It's probably X, let me fix that"
- Proposing solutions before tracing data flow
- "One more fix attempt" (when already tried 2+)

## Quick Reference

| Phase | Key Activities | Success Criteria |
|-------|---------------|------------------|
| **1. Root Cause** | Read errors, reproduce, check changes, gather evidence | Understand WHAT and WHY |
| **2. Pattern** | Find working examples, compare | Identify differences |
| **3. Hypothesis** | Form theory, test minimally | Confirmed or new hypothesis |
| **4. Implementation** | Create test, fix, verify | Bug resolved, tests pass |

## Real-World Impact

- Systematic approach: 15-30 minutes to fix
- Random fixes approach: 2-3 hours of thrashing
- First-time fix rate: 95% vs 40%

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Check recent changes | `git --no-pager log`, `git --no-pager diff` |
| Run tests | `dotnet_test_check` tool |
| Create failing test | `superpowers_skill(skill: "tdd")` |
| Verify fix | `superpowers_skill(skill: "verification-before-completion")` |
| Question architecture | Use `task` tool with agent_type: "critic" |
