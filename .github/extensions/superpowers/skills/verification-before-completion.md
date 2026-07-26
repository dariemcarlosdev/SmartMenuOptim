# Verification Before Completion

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Claiming work is complete without verification is dishonesty, not efficiency.

**Core principle:** Evidence before claims, always.

## The Iron Law

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

If you haven't run the verification command in this message, you cannot claim it passes.

## The Gate Function

Before claiming any status:

1. **IDENTIFY:** What command proves this claim?
2. **RUN:** Execute the FULL command (fresh, complete)
3. **READ:** Full output, check exit code, count failures
4. **VERIFY:** Does output confirm the claim?
   - If NO → state actual status with evidence
   - If YES → state claim WITH evidence
5. **ONLY THEN:** Make the claim

Skip any step = lying, not verifying.

## Verification Requirements

| Claim | Requires | NOT Sufficient |
|-------|----------|----------------|
| Tests pass | `dotnet_test_check` output: 0 failures | Previous run, "should pass" |
| Build succeeds | `dotnet_build_check` output: succeeded | "Linter passed" |
| Bug fixed | Test original symptom: passes | Code changed, assumed fixed |
| Conventions met | `check_conventions` output: clean | "I followed patterns" |
| Security clean | `owasp_security_scan` output: no issues | "I used parameterized queries" |
| Requirements met | Line-by-line checklist against spec | "Tests passing" |

## Red Flags — STOP

If you catch yourself:
- Using "should", "probably", "seems to"
- Expressing satisfaction before verification ("Great!", "Done!")
- About to commit without verification
- Relying on partial verification
- Thinking "just this once"

## Rationalization Prevention

| Excuse | Reality |
|--------|---------|
| "Should work now" | RUN the verification |
| "I'm confident" | Confidence ≠ evidence |
| "Just this once" | No exceptions |
| "Linter passed" | Linter ≠ compiler ≠ tests |
| "Agent said success" | Verify independently |
| "Partial check is enough" | Partial proves nothing |

## Key Patterns

**Tests:**
```
✅ [Run dotnet_test_check] [See: 34/34 pass] "All 34 tests pass"
❌ "Should pass now" / "Looks correct"
```

**Build:**
```
✅ [Run dotnet_build_check] [See: Build succeeded] "Build passes"
❌ "Code compiles fine" (without running build)
```

**Requirements:**
```
✅ Re-read plan → Create checklist → Verify each → Report gaps or completion
❌ "Tests pass, phase complete"
```

**Agent delegation:**
```
✅ Agent reports success → read_agent → Check actual output → Verify changes → Report
❌ Trust agent report without reading output
```

## The Bottom Line

**No shortcuts for verification.**

Run the command. Read the output. THEN claim the result.

Non-negotiable.

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Run tests | `dotnet_test_check` tool |
| Run build | `dotnet_build_check` tool |
| Check conventions | `check_conventions` tool |
| Security scan | `owasp_security_scan` tool |
| Check agent output | `read_agent` tool |
| Check secrets | `check_secrets` tool |
