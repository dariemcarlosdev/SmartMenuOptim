# Test-Driven Development (TDD)

> Adapted from obra/superpowers (MIT) for Copilot CLI.

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

## The Iron Law

```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```

Write code before the test? Delete it. Start over. No exceptions.

## When to Use

**Always:** New features, bug fixes, refactoring, behavior changes.

**Exceptions (ask user):** Throwaway prototypes, generated code, configuration files.

## Red-Green-Refactor Cycle

### RED — Write Failing Test

Write one minimal test showing what should happen.

**Requirements:**
- One behavior per test
- Clear name: `MethodName_Scenario_ExpectedResult`
- Real code (no mocks unless unavoidable)
- Arrange-Act-Assert structure

```csharp
[Fact]
public async Task HoldFunds_ValidTransaction_ReturnsSuccess()
{
    // Arrange
    var command = new HoldFundsCommand(transactionId, 500m, "USD", idempotencyKey);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
}
```

### Verify RED — Watch It Fail

**MANDATORY. Never skip.**

Run: `dotnet test --filter "HoldFunds_ValidTransaction"`

Confirm:
- Test fails (not errors)
- Failure is expected (feature missing, not typo)
- Failure message makes sense

**Test passes?** You're testing existing behavior. Fix test.

### GREEN — Minimal Code

Write the simplest code to pass the test. Nothing more.

- Don't add features not required by the test
- Don't refactor other code
- Don't "improve" beyond the test

### Verify GREEN — Watch It Pass

**MANDATORY.**

Run: `dotnet test --filter "HoldFunds_ValidTransaction"`

Confirm:
- Test passes
- Other tests still pass
- No warnings or errors

**Test fails?** Fix code, not test.

### REFACTOR — Clean Up

After green only:
- Remove duplication
- Improve names
- Extract helpers

Keep tests green. Don't add behavior.

## Good Tests

| Quality | Good | Bad |
|---------|------|-----|
| **Minimal** | One thing. "and" in name? Split it. | `Test_ValidatesEmailAndDomainAndWhitespace` |
| **Clear** | Name describes behavior | `Test1` |
| **Shows intent** | Demonstrates desired API | Obscures what code should do |

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests passing immediately prove nothing. |
| "Need to explore first" | Fine. Throw away exploration, start with TDD. |
| "Test hard = design unclear" | Hard to test = hard to use. Listen to the test. |
| "TDD will slow me down" | TDD faster than debugging. |

## Red Flags — STOP and Start Over

- Code before test
- Test passes immediately (without new code)
- Can't explain why test failed
- Rationalizing "just this once"

**ALL of these mean: Delete code. Start over with TDD.**

## Bug Fix Flow

1. **RED:** Write test reproducing the bug
2. **Verify RED:** Watch it fail with the bug
3. **GREEN:** Fix the bug with minimal code
4. **Verify GREEN:** Test passes, all other tests pass
5. **REFACTOR:** Clean up if needed

## Verification Checklist

Before marking work complete:

- [ ] Every new function/method has a test
- [ ] Watched each test fail before implementing
- [ ] Each test failed for expected reason
- [ ] Wrote minimal code to pass each test
- [ ] All tests pass
- [ ] Edge cases and errors covered

## Copilot CLI Mappings

| Superpowers Concept | Copilot CLI Equivalent |
|---|---|
| Run tests | `dotnet_test_check` tool or `dotnet test` command |
| Build check | `dotnet_build_check` tool |
| Test framework | xUnit + FluentAssertions (per project conventions) |
| Mocking | Moq (per project conventions) |
