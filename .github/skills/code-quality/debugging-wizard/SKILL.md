---
name: debugging-wizard
description: "Systematic debugging methodology — reproduce, isolate, hypothesize, fix, prevent. Parses stack traces, correlates logs, applies hypothesis-driven debugging."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: quality
  triggers: debug, error, bug, exception, stack trace, troubleshoot, not working, crash, fix issue
  role: specialist
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: test-generator, monitoring-expert, code-reviewer
---

# Debugging Wizard

A systematic, hypothesis-driven debugging specialist that transforms chaotic troubleshooting into a repeatable scientific process — reproduce, isolate, hypothesize, verify, fix, and prevent recurrence.

## When to Use This Skill

- "This code isn't working" or "I'm getting an error"
- Stack trace analysis — .NET exceptions, JavaScript errors, Python tracebacks
- Intermittent bugs that are hard to reproduce (race conditions, timing issues)
- Performance degradation investigation (memory leaks, CPU spikes)
- Post-incident root cause analysis
- Regression debugging — "this worked yesterday"
- Integration failures between services or layers
- Blazor circuit disconnects, SignalR failures, or EF Core query issues

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Debugging Tools | `references/debugging-tools.md` | Setting up debuggers (.NET, JS, Python) |
| Common Bug Patterns | `references/common-patterns.md` | Race conditions, memory leaks, null refs, deadlocks |
| Debugging Strategies | `references/strategies.md` | Binary search, git bisect, time travel debugging |
| .NET Diagnostics | `references/dotnet-debugging.md` | VS debugging, dotnet-dump, dotnet-trace, diagnostics |

## Core Workflow

### Step 1 — Reproduce the Bug

Before anything else, establish a reliable reproduction path.

1. **Gather evidence** — Collect the exact error message, stack trace, log entries, and screenshots.
2. **Define reproduction steps** — Write precise steps that trigger the bug every time.
3. **Identify environment** — Note the OS, runtime version, database state, and configuration.
4. **Establish baseline** — Confirm expected behavior vs. actual behavior.

**✅ Validation checkpoint:** You can trigger the bug on demand. If intermittent, you have a hypothesis about timing/conditions.

### Step 2 — Isolate the Fault

Narrow the search space systematically — do not guess randomly.

1. **Read the stack trace** — Start from the innermost exception. Identify the throwing method, line, and assembly.
2. **Trace the data flow** — Follow the input from entry point (controller, handler, component) to the failure site.
3. **Binary search** — If the codebase is large, comment out or bypass half the pipeline to determine which half contains the bug.
4. **Check recent changes** — Use `git log --oneline -20` and `git bisect` to find the introducing commit.
5. **Examine boundaries** — Bugs cluster at integration points: API boundaries, serialization, database queries, async transitions.

**✅ Validation checkpoint:** You know the exact method and approximate line where the fault manifests.

### Step 3 — Hypothesize and Verify

Form a specific, falsifiable hypothesis for each potential cause.

1. **State the hypothesis** — "The NullReferenceException occurs because `user.Email` is null when the account was created via SSO without an email claim."
2. **Design a test** — Write a minimal test case or add a diagnostic log that confirms or refutes the hypothesis.
3. **Execute the test** — Run it. If the hypothesis is wrong, revise and try the next one.
4. **Document eliminated hypotheses** — Keep a log of what you tried and ruled out.

**✅ Validation checkpoint:** You have a confirmed root cause with evidence (test failure, log output, debugger state).

### Step 4 — Fix and Verify

Apply the minimal correct fix, then prove it works.

1. **Write the failing test first** — A unit or integration test that captures the exact bug scenario.
2. **Apply the fix** — Change the minimum amount of code needed. Avoid scope creep.
3. **Run the test suite** — Confirm the new test passes AND all existing tests still pass.
4. **Test edge cases** — Consider related inputs that might trigger similar failures.

**✅ Validation checkpoint:** All tests pass. The reproduction steps no longer trigger the bug.

### Step 5 — Prevent Recurrence

Make the class of bug structurally impossible or detectable.

1. **Add guard clauses** — Validate inputs at the boundary with `ArgumentNullException.ThrowIfNull()` or FluentValidation.
2. **Improve logging** — Add structured log entries at the failure point so future occurrences are immediately visible.
3. **Add monitoring** — If the bug was a production incident, add a metric or alert for the failure condition.
4. **Update documentation** — If the bug reveals a non-obvious constraint, document it near the code.
5. **Consider a regression test** — If the bug was subtle, ensure the test is in the CI pipeline.

**✅ Validation checkpoint:** The fix is merged, monitored, and the bug class is harder to reintroduce.

## Quick Reference

### Parsing a .NET Stack Trace

```
System.NullReferenceException: Object reference not set to an instance of an object.
   at EscrowApp.Application.Escrows.Commands.CreateEscrow.Handle(CreateEscrowCommand request, CancellationToken ct)
       in /src/Application/Escrows/Commands/CreateEscrow.cs:line 42
   at MediatR.Mediator.Send[TResponse](IRequest`1 request, CancellationToken ct)
```

**Read bottom-up for call chain, top-down for cause.** Line 42 in `CreateEscrow.cs` is the fault site. Check what's null on that line — likely a navigation property or unmapped DTO field.

### Quick Diagnostic Commands (.NET)

```bash
# Collect a memory dump from a running process
dotnet-dump collect -p <PID> -o dump.dmp

# Analyze the dump
dotnet-dump analyze dump.dmp
> dumpheap -stat          # Find memory-heavy types
> dso                     # Dump stack objects
> clrstack                # Managed call stacks

# Trace performance counters
dotnet-counters monitor -p <PID> --counters System.Runtime

# Collect a trace for analysis
dotnet-trace collect -p <PID> --duration 00:00:30
```

## Constraints

### MUST DO

- Always reproduce the bug before attempting a fix
- Read the full stack trace — do not skip inner exceptions
- Form an explicit hypothesis before changing code
- Write a failing test that captures the bug before fixing it
- Verify the fix does not break existing tests
- Document the root cause in the commit message
- Check for the same bug pattern elsewhere in the codebase
- Use structured logging for diagnostic output — not `Console.WriteLine`

### MUST NOT

- Do not apply "shotgun debugging" — changing random things until it works
- Do not suppress exceptions without understanding the cause (`catch { }`)
- Do not fix symptoms instead of root causes
- Do not skip the reproduction step — "I think I know what it is" leads to wrong fixes
- Do not leave diagnostic code (temporary logs, breakpoints, `Thread.Sleep`) in the final commit
- Do not expand the fix scope beyond the bug — file separate issues for related problems
- Do not blame the framework without evidence — the bug is almost always in your code

## Output Template

```markdown
# Bug Analysis Report

**Bug:** {one-sentence description}
**Severity:** Critical | High | Medium | Low
**Environment:** {runtime, OS, configuration}

## Reproduction Steps

1. {Step 1}
2. {Step 2}
3. {Expected: X, Actual: Y}

## Stack Trace / Error

```
{Full stack trace or error output}
```

## Root Cause Analysis

**Hypothesis:** {What you believe caused the bug and why}
**Evidence:** {Test result, log output, or debugger state that confirms it}
**Root Cause:** {The actual underlying issue — not just the symptom}

## Investigation Log

| # | Hypothesis | Test | Result |
|---|-----------|------|--------|
| 1 | {First theory} | {How tested} | ❌ Ruled out |
| 2 | {Second theory} | {How tested} | ✅ Confirmed |

## Fix

**File(s) Changed:** {list}
**Test Added:** {test name and location}
**Change Description:** {what was changed and why}

## Prevention

- {Guard clause, validation, or structural change added}
- {Monitoring or alerting added}
- {Documentation updated}
```

## Integration Notes

### Copilot CLI
Trigger with: `debug this error`, `why is this crashing`, `fix this exception`, `troubleshoot [description]`

### Claude
Include this file in project context. Trigger with: "Debug this error: [paste stack trace]"

### Gemini
Reference via `GEMINI.md` or direct file inclusion. Trigger with: "Help me debug [description]"
