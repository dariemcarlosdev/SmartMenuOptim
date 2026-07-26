# Requesting Code Review

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Dispatch a critic agent to catch issues before they cascade.

**Core principle:** Review early, review often.

## When to Request Review

**Mandatory:**
- After each task in subagent-driven development
- After completing a major feature
- Before merge to main

**Optional but valuable:**
- When stuck (fresh perspective)
- Before refactoring (baseline check)
- After fixing complex bug

## How to Request

### 1. Gather Context

```bash
git --no-pager log --oneline -5    # Recent commits
git --no-pager diff HEAD~N         # Changes to review
```

### 2. Dispatch Critic Agent

```
Use task tool:
  agent_type: "critic"
  prompt: |
    Review these code changes for correctness, quality, and security.

    **What was implemented:** [description]
    **Requirements/spec:** [paste relevant section or file path]
    **Files changed:** [list files]

    Review criteria:
    1. Does the implementation match the spec? (completeness)
    2. Are there bugs, edge cases, or logic errors? (correctness)
    3. SOLID principles, clean code, naming? (quality)
    4. Input validation, authorization, injection prevention? (security)
    5. Test coverage adequate? (testing)

    For each issue found:
    - Severity: Critical / Important / Minor
    - Location: file and line
    - Issue: what's wrong
    - Fix: specific suggestion
```

### 3. Act on Feedback

| Severity | Action |
|----------|--------|
| **Critical** | Fix immediately — blocks everything |
| **Important** | Fix before proceeding to next task |
| **Minor** | Note for later, don't block progress |
| **Reviewer wrong** | Push back with technical reasoning |

### 4. Re-Review if Needed

If critic found Critical or Important issues:
1. Fix the issues
2. Re-dispatch critic with same scope
3. Repeat until clean

## Integration with Workflows

| Workflow | When to Review |
|----------|---------------|
| Subagent-driven development | After EACH task (mandatory) |
| Executing plans | After each batch of 3 tasks |
| Ad-hoc development | Before merge |

## Red Flags

- Skip review because "it's simple"
- Ignore Critical issues
- Proceed with unfixed Important issues
- Argue without technical evidence

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Dispatch code reviewer | `task` tool, agent_type: "critic" |
| Get review results | `read_agent` tool |
| Follow-up with reviewer | `write_agent` tool |
| Check git changes | `git --no-pager diff`, `git --no-pager log` |
| Security-focused review | `owasp_security_scan` tool + critic agent |
