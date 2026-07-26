# TDD Anti-Patterns — Common Mistakes to Avoid

## Purpose

Identify and fix common TDD mistakes that undermine the value of test-first development.

## Process Anti-Patterns

### 1. Ice Cream Cone (Inverted Test Pyramid)

```
    ❌ Current                ✅ Target
   ┌──────────┐          ┌──────────────┐
   │  Manual  │          │   Unit Tests  │  (many, fast)
   │  Tests   │          ├──────────────┤
   ├──────────┤          │ Integration   │  (some, medium)
   │ UI Tests │          ├──────────────┤
   ├──────────┤          │  E2E Tests    │  (few, slow)
   │  API     │          └──────────────┘
   │  Tests   │
   ├──────────┤
   │  Unit    │
   └──────────┘
```

**Fix:** Invest heavily in unit tests. Each layer above should have fewer tests.

### 2. Test After (Not Test First)

Writing code first, then tests, misses TDD's design benefits:

```csharp
// ❌ Test After — retrofitting tests to existing code
// Production code already written with hardcoded dependencies
// Tests are awkward, require excessive mocking, miss edge cases

// ✅ Test First — tests drive the design
// Write test → it reveals the interface needed → implement minimal code
```

### 3. Big Step TDD

Taking steps that are too large:

```csharp
// ❌ Big step — entire feature in one test
[Fact]
public async Task CreateEscrow_ShouldValidateAmountAndPartiesAndTermsAndCreateAndNotifyAndAudit()
{
    // 50 lines of setup, 20 assertions... this is not TDD
}

// ✅ Baby steps — one behavior per test
[Fact] public void Create_WhenValidInput_ShouldSetPendingStatus() { }
[Fact] public void Create_WhenZeroAmount_ShouldThrowValidationError() { }
[Fact] public void Create_WhenBuyerEqualsSeller_ShouldThrowDomainError() { }
```

### 4. Skipping Refactor

Green → next test → Green → next test (never refactoring):

```csharp
// After 10 cycles without refactoring, you have:
// - Duplicated test setup in every test
// - Magic numbers everywhere
// - 200-line method that "works" but is unmaintainable

// Fix: ALWAYS pause after GREEN to check for refactoring opportunities
```

## Test Code Anti-Patterns

### 5. The Liar — Test That Always Passes

```csharp
// ❌ No assertion — this test always passes
[Fact]
public async Task Handle_ShouldWork()
{
    await _sut.Handle(command, CancellationToken.None);
    // Where's the assertion?!
}

// ✅ Always assert on observable behavior
[Fact]
public async Task Handle_WhenValid_ShouldReturnSuccess()
{
    var result = await _sut.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
}
```

### 6. The Inspector — Over-Specifying Interactions

```csharp
// ❌ Testing every internal call — brittle to refactoring
_repoMock.Verify(r => r.GetByIdAsync(It.IsAny<EscrowTransactionId>(),
    It.IsAny<CancellationToken>()), Times.Exactly(1));
_validatorMock.Verify(v => v.ValidateAsync(It.IsAny<FundEscrowCommand>(),
    It.IsAny<CancellationToken>()), Times.Exactly(1));
_mapperMock.Verify(m => m.Map<EscrowDto>(It.IsAny<EscrowTransaction>()),
    Times.Exactly(1));
_loggerMock.Verify(l => l.LogInformation(It.IsAny<string>()),
    Times.Exactly(2));

// ✅ Test behavior, not implementation
result.IsSuccess.Should().BeTrue();
result.Value.Status.Should().Be(EscrowStatus.Funded);
// Only verify critical side effects
_uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
```

### 7. The Giant — Test Class with Too Many Tests

**Symptom:** Test class > 500 lines or > 30 tests.

**Root cause:** Production class has too many responsibilities.

**Fix:** Split production class → split test class.

### 8. The Mockery — Mocking Everything

```csharp
// ❌ Mocking value objects and simple types
var moneyMock = new Mock<Money>(); // Money is a value object — don't mock it!
var guidMock = new Mock<Guid>();   // This doesn't even make sense

// ✅ Only mock interfaces for external dependencies
var repoMock = new Mock<IEscrowRepository>();  // External dependency — mock it
var money = Money.From(1000m);                  // Value object — use real one
```

### 9. Chain Gang — Tests That Must Run in Order

```csharp
// ❌ Test2 depends on state set by Test1
[Fact] public void Test1_CreateEscrow() { _escrow = Create(); }
[Fact] public void Test2_FundEscrow() { _escrow.Fund(amount); } // Relies on Test1

// ✅ Each test is independent
[Fact] public void Fund_WhenPendingEscrow_ShouldTransitionToFunded()
{
    var escrow = EscrowTransaction.Create(buyerId, sellerId, amount); // Own setup
    escrow.Fund(amount);
    escrow.Status.Should().Be(EscrowStatus.Funded);
}
```

### 10. Slow Poke — Tests That Take Too Long

| Test Type | Target | Red Flag |
|-----------|--------|----------|
| Single unit test | < 50ms | > 200ms |
| Full unit suite | < 10s | > 60s |
| Single integration test | < 2s | > 10s |

**Common causes:** Real I/O, `Thread.Sleep`, database calls in "unit" tests, large object graphs.

## Anti-Pattern Detection Checklist

```markdown
- [ ] Any test without assertions? (The Liar)
- [ ] Any test with > 5 Verify calls? (The Inspector)
- [ ] Any test class with > 30 tests? (The Giant)
- [ ] Any test mocking value objects? (The Mockery)
- [ ] Any tests dependent on execution order? (Chain Gang)
- [ ] Any test taking > 200ms? (Slow Poke)
- [ ] Any production code written before its test? (Test After)
- [ ] Any refactoring skipped for > 3 cycles? (Skipping Refactor)
```
