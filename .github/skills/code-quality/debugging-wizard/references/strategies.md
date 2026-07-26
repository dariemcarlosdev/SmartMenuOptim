# Debugging Strategies Reference

> **Load when:** Applying systematic debugging techniques — binary search, git bisect, time travel debugging.

## Hypothesis-Driven Debugging

The scientific method applied to software bugs. Every debugging session should follow this loop:

```
Observe → Hypothesize → Predict → Test → Conclude → Repeat
```

### The Debugging Log

Maintain a written log during complex debugging sessions:

```markdown
| # | Time  | Hypothesis                          | Test                        | Result        |
|---|-------|-------------------------------------|-----------------------------|---------------|
| 1 | 10:15 | Null ref from unmapped DTO field    | Check AutoMapper config     | ❌ Mapping OK |
| 2 | 10:25 | EF Core lazy loading not triggered  | Add .Include() for Buyer    | ❌ Still null |
| 3 | 10:35 | Buyer is null in seed data          | Query DB directly           | ✅ Confirmed  |
```

**Why this works:** Prevents circular debugging (retrying disproven hypotheses) and creates an audit trail for post-incident review.

## Binary Search Debugging

Narrow down the fault location by systematically halving the search space.

### Code-Level Binary Search

When the bug is in a long execution path and you cannot pinpoint it:

1. **Find the midpoint** — Identify the middle of the suspect code path.
2. **Add a diagnostic check** — Log the state or assert a condition at the midpoint.
3. **Run the reproduction** — Is the state correct at the midpoint?
   - **Yes** → Bug is in the second half. Repeat with the second half.
   - **No** → Bug is in the first half. Repeat with the first half.
4. **Converge** — After log₂(N) iterations, you've isolated the exact location.

```csharp
// Example: Binary search through a pipeline
public async Task<EscrowResult> ProcessEscrowAsync(CreateEscrowCommand cmd, CancellationToken ct)
{
    var validated = await _validator.ValidateAsync(cmd, ct);
    Debug.Assert(validated.IsValid, $"Validation failed: {validated}"); // Check 1

    var escrow = _mapper.Map<Escrow>(cmd);
    Debug.Assert(escrow.BuyerId is not null, "BuyerId null after mapping"); // Check 2

    await _repository.AddAsync(escrow, ct);
    Debug.Assert(escrow.Id != default, "ID not set after save"); // Check 3

    await _eventBus.PublishAsync(new EscrowCreatedEvent(escrow.Id), ct);
    return new EscrowResult(escrow.Id); // Check 4
}
```

## Git Bisect

Find the exact commit that introduced a regression using binary search over git history.

### Basic Usage

```bash
# Start bisect session
git bisect start

# Mark current commit as bad (has the bug)
git bisect bad

# Mark a known good commit (before the bug existed)
git bisect good v2.1.0

# Git checks out the midpoint — test it
dotnet test --filter "EscrowCreationTests"

# Mark the result
git bisect good  # if tests pass
git bisect bad   # if tests fail

# Repeat until git identifies the first bad commit
# Git will output: "<sha> is the first bad commit"

# When done, reset to original state
git bisect reset
```

### Automated Git Bisect

Automate the good/bad decision with a test script:

```bash
# Automated bisect using a test command
git bisect start HEAD v2.1.0
git bisect run dotnet test --filter "EscrowProcessingTests" --no-build

# Or with a custom script
git bisect run bash -c 'dotnet build && dotnet test --filter "SpecificTest" --no-build'
```

### Git Bisect Tips

- Choose a test that reliably reproduces the bug in under 30 seconds
- If a commit doesn't build, mark it with `git bisect skip`
- Use `git bisect log` to see the history of your bisect session
- Use `git bisect visualize` to see remaining commits in a GUI

## Rubber Duck Debugging

Explain the bug to someone (or something) step by step. The act of articulating the problem often reveals the solution.

### Structured Rubber Duck Protocol

1. **State the expected behavior** — "When a buyer creates an escrow, the amount should be held."
2. **State the actual behavior** — "The amount is deducted but the escrow record shows $0."
3. **Walk through the code path** — Read each line aloud and explain what it does.
4. **Question every assumption** — "I assume this mapping works correctly — but have I verified it?"
5. **Identify the gap** — The bug often lives in the gap between what you assume and what actually happens.

## Divide and Conquer with Feature Flags

When a bug appears after deploying multiple changes, use feature flags to isolate which change caused it:

```csharp
// Toggle features to isolate the regression
if (await _featureManager.IsEnabledAsync("NewPaymentFlow"))
{
    await _newPaymentService.ProcessAsync(payment, ct);
}
else
{
    await _legacyPaymentService.ProcessAsync(payment, ct);
}
```

Disable flags one at a time until the bug disappears — the last disabled flag contains the regression.

## Time Travel Debugging

### .NET Time Travel Debugging with WinDbg

Available on Windows with WinDbg Preview:

1. **Record a trace** — Capture the execution with TTD recording
2. **Replay forward and backward** — Step backward from the crash to find the cause
3. **Query the trace** — Use LINQ-like queries over the execution history

```
// WinDbg TTD commands
!tt 0        // Go to start of trace
!tt 100      // Go to end of trace
g-           // Step backward
ba r4 @rsp   // Break on memory read (reverse)
```

### Poor Man's Time Travel: Structured Log Replay

When TTD is not available, use structured logs as a time machine:

```csharp
// Log enough state to reconstruct the execution path
Log.Information("Escrow {EscrowId} state transition: {From} → {To}, Amount: {Amount}, Trigger: {Trigger}",
    escrow.Id, previousState, newState, escrow.Amount, triggerEvent);
```

Then query logs to reconstruct the timeline:

```bash
# Reconstruct an escrow's lifecycle from logs
rg "EscrowId.*ESC-12345" logs/ --sort=path | head -50
```

## Wolf Fence Algorithm

A systematic elimination technique for bugs in complex systems:

1. **Place a "fence" (assertion) in the middle of the system**
2. **The bug is on one side of the fence** — determine which side
3. **Move the fence to the middle of the remaining half**
4. **Repeat until the bug is cornered**

This is binary search applied to distributed systems — place health checks at service boundaries and narrow down which service contains the bug.

```csharp
// Health check fences at each service boundary
app.MapGet("/health/database", async (AppDbContext db) =>
{
    await db.Database.CanConnectAsync();
    return Results.Ok("Database OK");
});

app.MapGet("/health/cache", async (IDistributedCache cache) =>
{
    await cache.SetStringAsync("health", "ok");
    return Results.Ok("Cache OK");
});

app.MapGet("/health/payment-gateway", async (IPaymentClient client) =>
{
    await client.PingAsync();
    return Results.Ok("Payment Gateway OK");
});
```
