---
applyTo: "**/*"
---

# Memory & Context Window Optimization Rules

> Universal rules for all AI models working on this codebase.
> Goal: maximize useful context, minimize waste, maintain continuity across sessions.

## 1. Context Window Discipline

### Load Only What You Need

- **Never bulk-read directories.** Use `glob` or `grep` to find specific files first, then read only relevant ones.
- **Use `view_range`** to read specific line ranges instead of full files when you know the target area.
- **Prefer `grep` with `output_mode: "files_with_matches"`** for initial discovery, then read only matched files.
- **Batch parallel reads.** When you need multiple files, read them all in a single tool-call turn.

### Avoid Context Pollution

- **Suppress verbose output.** Use `--quiet`, `--no-pager`, pipe to `head`/`Select-Object -First` on long outputs.
- **Don't re-read files** you've already seen in this session unless they were modified.
- **Don't echo file contents back** to the user unless explicitly asked — they can see the timeline.
- **Trim build/test output.** On success, report "Build succeeded" or "All N tests passed" — don't paste full logs.
- **Don't paste full stack traces** unless debugging a specific failure. Summarize the error first.

### Structured Over Verbose

- Use tables, bullet points, and concise summaries over prose when reporting findings.
- When showing code, show only the relevant snippet with enough context (5-10 lines), not entire files.
- Prefer `show_file` with `view_range` over dumping code into chat text.

## 2. Session Priming Strategy

### First Turn Efficiency

When starting a new session or task:

1. **Read the architecture summary** — use the `project_summary` tool (context-optimizer extension) instead of reading multiple files.
2. **Check docs/ first** — use `check_docs` tool to find relevant feature documentation before exploring source.
3. **Use scoped searches** — narrow grep/glob to the relevant layer directory:
   - UI changes → `Components/`
   - Business logic → `Features/`
   - Data access → `Data/`
   - Payment flow → `Services/Strategies/`
   - Domain model → `Models/`, `Events/`

### Context Checkpoint Pattern

For long-running tasks:

- After completing a logical unit of work, summarize what was done and what's next.
- If context is growing large, proactively use `/compact` to summarize and free space.
- Before `/compact`, ensure all important decisions and findings are captured in the plan or todos.

## 3. File Access Patterns

### Read Order Priority

When investigating a feature, read files in this order (most context-efficient first):

1. **docs/{feature}/README.md** — high-level understanding, cheapest context
2. **Interface/contract files** — understand the API surface (e.g., `IEscrowTransactionRepository.cs`)
3. **MediatR command/handler** — understand the business flow
4. **Implementation** — only if you need to understand internals
5. **Tests** — only if verifying behavior or writing new tests

### Write Order Priority

When implementing, minimize context churn:

1. **Plan first** — outline changes before opening files
2. **Edit bottom-up** — Domain → Application → Infrastructure → Presentation
3. **Batch edits per file** — make all edits to one file in a single turn
4. **Don't interleave reads and writes** to the same file — read once, plan edits, apply all

## 4. Search Efficiency

### Grep/Glob Best Practices

```
✅ grep pattern:"IFundHoldable" glob:"**/*.cs" output_mode:"files_with_matches"
   → Fast: returns only file paths

❌ grep pattern:"IFundHoldable" output_mode:"content" -A:50
   → Wasteful: loads 50 lines of context per match across entire repo
```

### Progressive Disclosure Pattern

1. **Find files** — `glob` or `grep` with `files_with_matches`
2. **Count matches** — `grep` with `count` to assess scope
3. **Read specific matches** — `grep` with `content` and `-n` on targeted files
4. **Deep dive** — `view` with `view_range` on the most relevant result

## 5. Sub-Agent Delegation

### When to Delegate vs. Do It Yourself

| Task | Approach | Why |
|------|----------|-----|
| Read 1-3 known files | Do it yourself | Faster, stays in context |
| Search for a symbol | Do it yourself (grep) | Single tool call |
| Analyze 5+ independent areas | Delegate to explore agents | Parallel, keeps main context clean |
| Complex multi-file refactor | Delegate to general-purpose | Separate context window |
| Run build/tests | Delegate to task agent | Summary only comes back |

### Delegation Context Rules

- **Give complete context** to sub-agents — they don't share your memory.
- **Don't duplicate** sub-agent findings by re-reading the same files afterward.
- **Trust sub-agent results** for status (pass/fail), verify only if suspicious.

## 6. Memory Across Sessions

### Session Store Usage

Before starting major work, check session history:

```sql
-- What was done recently in this project?
SELECT s.id, s.summary, s.updated_at 
FROM sessions s 
WHERE s.cwd LIKE '%EscrowApp%' 
ORDER BY s.updated_at DESC LIMIT 5;

-- Was this problem solved before?
SELECT content FROM search_index 
WHERE search_index MATCH 'keyword1 OR keyword2' 
ORDER BY rank LIMIT 10;
```

### Continuity Patterns

- **Check plan.md** at session start — it may contain unfinished work.
- **Check todos** — `SELECT * FROM todos WHERE status != 'done'` for pending items.
- **Reference previous sessions** when the user says "continue" or "pick up where we left off."

## 7. Token Budget Guidelines

### Awareness Thresholds

| Context Usage | Action |
|---------------|--------|
| < 30% | Normal operation — read freely |
| 30-60% | Be selective — use view_range, prefer summaries |
| 60-80% | Conservative — delegate to sub-agents, summarize findings |
| > 80% | Critical — suggest /compact, stop reading new files, work from memory |

### Cost-Per-Action Estimates

| Action | Relative Context Cost | Notes |
|--------|----------------------|-------|
| `grep` (files_with_matches) | Very Low | Just file paths |
| `glob` | Very Low | Just file paths |
| `grep` (content, 5 matches) | Low | Small snippets |
| `view` (50 lines) | Low | Targeted read |
| `view` (full file, 200 lines) | Medium | Only when necessary |
| `powershell` (build output) | Medium-High | Suppress verbose output |
| `view` (full file, 500+ lines) | High | Avoid — use view_range |
| Multiple full file reads | Very High | Batch and parallelize |

## 8. Anti-Patterns (Never Do These)

- ❌ **Cat-then-grep**: Don't read an entire file just to search it — use grep directly.
- ❌ **Exploratory full reads**: Don't read files "just to understand" without a specific question.
- ❌ **Re-reading after edit**: Don't view a file you just edited — you know what's in it.
- ❌ **Verbose confirmations**: Don't paste back what you wrote. Say "Created X with Y" not "Here's the file I created: [full content]."
- ❌ **Sequential single-file reads**: Don't read files one-per-turn. Batch parallel reads.
- ❌ **Ignoring docs/**: Don't explore source code when docs/ has a README for that feature.
- ❌ **Global unrestricted grep**: Always scope to relevant directories or file types.
