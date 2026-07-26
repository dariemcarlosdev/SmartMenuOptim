# Writing Implementation Plans

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Write comprehensive implementation plans assuming the engineer has zero context. Document everything: which files to touch, code, testing, how to verify. Bite-sized tasks. DRY. YAGNI. TDD. Frequent commits.

## Scope Check

If the spec covers multiple independent subsystems, break into separate plans — one per subsystem. Each plan should produce working, testable software on its own.

## File Structure First

Before defining tasks, map out which files will be created or modified:

- Design units with clear boundaries and well-defined interfaces
- Prefer smaller, focused files over large ones
- Files that change together should live together
- In existing codebases, follow established patterns

## Bite-Sized Task Granularity

Each step is one action (2-5 minutes):
- "Write the failing test" — step
- "Run it to make sure it fails" — step
- "Implement the minimal code to make the test pass" — step
- "Run the tests and make sure they pass" — step
- "Commit" — step

## Plan Document Header

Every plan MUST start with:

```markdown
# [Feature Name] Implementation Plan

**Goal:** [One sentence]
**Architecture:** [2-3 sentences about approach]
**Tech Stack:** [Key technologies]

---
```

## Task Structure

```markdown
### Task N: [Component Name]

**Files:**
- Create: `exact/path/to/file.cs`
- Modify: `exact/path/to/existing.cs`
- Test: `tests/exact/path/to/test.cs`

- [ ] **Step 1: Write the failing test**
  [Actual test code]

- [ ] **Step 2: Run test to verify it fails**
  Run: [exact command]
  Expected: FAIL with [reason]

- [ ] **Step 3: Write minimal implementation**
  [Actual implementation code]

- [ ] **Step 4: Run test to verify it passes**
  Run: [exact command]
  Expected: PASS

- [ ] **Step 5: Commit**
  `git add [files] && git commit -m "feat: [description]"`
```

## No Placeholders — EVER

These are plan failures:
- "TBD", "TODO", "implement later"
- "Add appropriate error handling"
- "Write tests for the above" (without actual test code)
- "Similar to Task N" (repeat the code)
- Steps without code blocks for code steps
- References to undefined types/functions

## Self-Review

After writing the complete plan:

1. **Spec coverage:** Skim each requirement. Can you point to a task that implements it?
2. **Placeholder scan:** Search for red flags from the "No Placeholders" section
3. **Type consistency:** Do names/signatures match across tasks?

Fix issues inline. If you find a spec requirement with no task, add the task.

## Execution Handoff

After saving the plan, track tasks in SQL todos:

```sql
INSERT INTO todos (id, title, description, status) VALUES
  ('task-1-name', 'Task 1: [Title]', '[Full description]', 'pending');
```

Then load the execution skill: `superpowers_skill(skill: "executing-plans")` or `superpowers_skill(skill: "subagent-driven-development")`

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Save plan | Create `plan.md` in session folder or docs/ |
| TodoWrite | SQL `todos` table with `todo_deps` |
| Subagent execution | `task` tool with agent_type: "general-purpose" |
| Inline execution | Follow executing-plans skill in current session |
| Next skill | `superpowers_skill(skill: "executing-plans")` |
