# Hooks Reference — Copilot CLI vs Claude Code

> Cross-platform hooks comparison for the NexTruzt.io AI development framework.
> Both platforms fire hooks at lifecycle events. This doc maps events, capabilities, and our implementations.

---

## Hook Events Comparison

### Copilot CLI Hook Events

| Event | When It Fires | Can Block | Available In |
|-------|--------------|-----------|-------------|
| `onSessionStart` | Session begins | No | `joinSession()` |
| `onSessionEnd` | Session terminates | No | `joinSession()` |
| `onUserPromptSubmitted` | User submits a prompt, before processing | No* | `joinSession()` |
| `onPreToolUse` | Before a tool call executes | Yes (return `"reject"`) | `joinSession()` |
| `onPostToolUse` | After a tool call succeeds | No | `joinSession()` |
| `onErrorOccurred` | When an error occurs during tool execution | No | `joinSession()` |

*\* Can inject `additionalContext` to influence behavior but cannot block the prompt.*

**Implementation:** Node.js ES modules (`.mjs`) in `.github/extensions/*/extension.mjs`

---

### Claude Code Hook Events

| Event | When It Fires | Can Block | Matcher |
|-------|--------------|-----------|---------|
| `SessionStart` | Session begins or resumes | No | `startup`, `resume`, `clear`, `compact` |
| `SessionEnd` | Session terminates | No | `clear`, `resume`, `logout`, etc. |
| `UserPromptSubmit` | User submits a prompt, before processing | Yes | *(no matcher)* |
| `PreToolUse` | Before a tool call executes | Yes (`permissionDecision: "deny"`) | Tool name regex: `Bash`, `Edit\|Write` |
| `PostToolUse` | After a tool call succeeds | No | Tool name regex |
| `PostToolUseFailure` | After a tool call fails | No | Tool name regex |
| `PermissionRequest` | Permission dialog appears | Yes | Tool name regex |
| `PermissionDenied` | Tool call denied by classifier | No (but can `retry: true`) | Tool name regex |
| `Notification` | Claude sends notification | No | `permission_prompt`, `idle_prompt` |
| `SubagentStart` | Subagent spawned | No | Agent type: `Bash`, `Explore`, `Plan` |
| `SubagentStop` | Subagent finishes | Yes | Agent type |
| `TaskCreated` | Task created via TaskCreate | No | *(no matcher)* |
| `TaskCompleted` | Task marked complete | No | *(no matcher)* |
| `Stop` | Claude finishes responding | Yes | *(no matcher)* |
| `StopFailure` | Turn ends due to API error | No | `rate_limit`, `server_error`, etc. |
| `TeammateIdle` | Agent team member going idle | No | *(no matcher)* |
| `InstructionsLoaded` | CLAUDE.md or rules file loads | No | `session_start`, `path_glob_match` |
| `ConfigChange` | Config file changes mid-session | No | `user_settings`, `project_settings` |
| `CwdChanged` | Working directory changes (`cd`) | No | *(always fires)* |
| `FileChanged` | Watched file changes on disk | Yes | Filename (e.g., `.envrc`) |
| `WorktreeCreate` | Git worktree being created | No | *(no matcher)* |
| `WorktreeRemove` | Git worktree being removed | No | *(no matcher)* |
| `PreCompact` | Before context compaction | No | `manual`, `auto` |
| `PostCompact` | After compaction completes | No | `manual`, `auto` |
| `Elicitation` | MCP server requests user input | No | MCP server name |
| `ElicitationResult` | User responds to MCP elicitation | No | MCP server name |

**Implementation:** Shell scripts, HTTP endpoints, LLM prompts, or agent hooks in `.claude/settings.json`

---

## Event Mapping: Copilot CLI ↔ Claude Code

| Copilot CLI Event | Claude Code Equivalent | Notes |
|---|---|---|
| `onSessionStart` | `SessionStart` | Direct equivalent |
| `onSessionEnd` | `SessionEnd` | Direct equivalent |
| `onUserPromptSubmitted` | `UserPromptSubmit` | Claude can also block prompts |
| `onPreToolUse` | `PreToolUse` | Both can block; Claude has richer matcher syntax |
| `onPostToolUse` | `PostToolUse` | Direct equivalent |
| `onErrorOccurred` | `PostToolUseFailure` / `StopFailure` | Claude splits into tool vs API errors |
| *(none)* | `PermissionRequest` | Claude-only: intercept permission dialogs |
| *(none)* | `SubagentStart` / `SubagentStop` | Claude-only: subagent lifecycle |
| *(none)* | `InstructionsLoaded` | Claude-only: react to config loading |
| *(none)* | `PreCompact` / `PostCompact` | Claude-only: context compaction hooks |
| *(none)* | `FileChanged` | Claude-only: file watcher hooks |
| *(none)* | `CwdChanged` | Claude-only: directory change hooks |
| *(none)* | `Notification` | Claude-only: notification interception |
| *(none)* | `TaskCreated` / `TaskCompleted` | Claude-only: task lifecycle |
| *(none)* | `Stop` | Claude-only: validate before turn ends |

---

## Our Implementations

### Copilot CLI Extensions (`.github/extensions/`)

| Extension | Hooks Used | Purpose |
|-----------|-----------|---------|
| **security-scanner** | `onPreToolUse`, `onPostToolUse`, `onUserPromptSubmitted` | Blocks secrets in writes, OWASP reminders, payment/auth context |
| **build-guardian** | `onPostToolUse` | Tracks modified `.cs` files, reminds to validate build |
| **context-optimizer** | `onSessionStart`, `onUserPromptSubmitted` | Injects project summary, warns on long prompts |
| **doc-sync** | `onSessionStart`, `onPostToolUse` | Reminds to update docs when source changes |
| **dotnet-conventions** | `onSessionStart`, `onPostToolUse` | Checks `.cs`/`.razor` conventions after edits |
| **research-first** | `onSessionStart`, `onUserPromptSubmitted` | Injects "read docs first" before implementation |

### Claude Code Hooks (`.claude/settings.json` + `.claude/hooks/`)

| Hook Script | Event | Matcher | Purpose |
|-------------|-------|---------|---------|
| **security-scanner.ps1** | `PreToolUse` | `Edit\|Write\|MultiEdit` | Blocks hardcoded secrets, API keys, connection strings |
| **dotnet-conventions.ps1** | `PostToolUse` | `Edit\|Write\|MultiEdit` | Checks code-behind, namespaces, scoped CSS |
| **doc-sync-reminder.ps1** | `PostToolUse` | `Edit\|Write\|MultiEdit` | Reminds to update docs for source changes |
| **build-reminder.ps1** | `PostToolUse` | `Edit\|Write\|MultiEdit` | Reminds to verify build after `.cs` changes |
| **research-first.ps1** | `UserPromptSubmit` | *(all)* | Injects research-first guidance |
| **context-optimizer.ps1** | `SessionStart` | *(all)* | Injects project architecture context |

---

## Configuration Locations

| Platform | Config File | Hook Scripts |
|----------|------------|-------------|
| **Copilot CLI** | `.github/extensions/*/extension.mjs` | Inline (Node.js ES modules) |
| **Claude Code** | `.claude/settings.json` → `hooks` | `.claude/hooks/*.ps1` (Windows) or `.sh` (Linux/Mac) |

---

## Key Differences

| Feature | Copilot CLI | Claude Code |
|---------|------------|-------------|
| **Language** | JavaScript (ES modules, `.mjs`) | Any (shell, PowerShell, HTTP, LLM prompt) |
| **Hook types** | Code callbacks only | `command`, `http`, `prompt`, `agent` |
| **Blocking** | `onPreToolUse` returns `"reject"` | `PreToolUse` outputs `permissionDecision: "deny"` |
| **Context injection** | Return `{ additionalContext: "..." }` | Output `{ "additionalContext": "..." }` JSON |
| **Custom tools** | `registerTool()` in extension | Not in hooks (use MCP servers instead) |
| **Matcher syntax** | Programmatic (`if` statements in code) | Regex on tool name + `if` field for arguments |
| **Total events** | 6 | 27 |
| **Async hooks** | All async by nature (Node.js) | `async: true` flag for background execution |
| **Discovery** | `.github/extensions/*/extension.mjs` | `.claude/settings.json` + script paths |
| **SDK** | `@github/copilot-sdk/extension` | stdin/stdout JSON protocol |

---

## Adding New Hooks

### Copilot CLI

1. Create `.github/extensions/{name}/extension.mjs`
2. Import `joinSession` from `@github/copilot-sdk/extension`
3. Register hooks in the `joinSession()` call
4. Optionally register tools with `registerTool()`

### Claude Code

1. Create script in `.claude/hooks/{name}.ps1` (Windows) or `.sh` (Linux/Mac)
2. Add hook entry to `.claude/settings.json` under the appropriate event
3. Script reads JSON from stdin, outputs JSON to stdout
4. Use `exit 0` for no action, output JSON for context/decisions

---

*Maintained as part of the NexTruzt.io AI Development Framework*
