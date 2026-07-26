# Executing Plans

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Load plan, review critically, execute all tasks, report when complete.

## The Process

### Step 1: Load and Review Plan

1. Read plan file (plan.md or docs/ path)
2. Review critically — identify questions or concerns
3. If concerns: raise them with user before starting
4. If no concerns: populate SQL todos and proceed

```sql
-- Track all tasks
INSERT INTO todos (id, title, description, status) VALUES
  ('task-1', 'Task 1: [Title]', '[Description]', 'pending');

-- Track dependencies
INSERT INTO todo_deps (todo_id, depends_on) VALUES ('task-2', 'task-1');
```

### Step 2: Execute Tasks

For each task:
1. Mark as in_progress: `UPDATE todos SET status = 'in_progress' WHERE id = 'task-N'`
2. Follow each step exactly (plan has bite-sized steps)
3. Run verifications as specified — use `superpowers_skill(skill: "verification-before-completion")`
4. Mark as done: `UPDATE todos SET status = 'done' WHERE id = 'task-N'`

### Step 3: Complete Development

After all tasks complete:
- Run full test suite to verify nothing is broken
- Use `superpowers_skill(skill: "requesting-code-review")` for final review
- Commit with meaningful message

## When to Stop and Ask

**STOP executing immediately when:**
- Hit a blocker (missing dependency, test fails, instruction unclear)
- Plan has critical gaps
- You don't understand an instruction
- Verification fails repeatedly

**Ask for clarification rather than guessing.**

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| TodoWrite | SQL `todos` + `todo_deps` tables |
| Status tracking | `UPDATE todos SET status = '...'` |
| Ready query | `SELECT * FROM todos WHERE status='pending' AND NOT EXISTS (...)` |
| Build/test | `dotnet_build_check` / `dotnet_test_check` tools or `task` agent |
| Verification | `superpowers_skill(skill: "verification-before-completion")` |
