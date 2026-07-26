# Assertion Quality — Weak vs Strong Assertions

## Purpose

Guide assessment of assertion quality in test suites, distinguishing between assertions that provide real confidence and those that give false security.

## Assertion Strength Tiers

### Tier 1: No Assertion (❌ Useless)

Tests that execute code but never verify results. Worst possible — provides zero confidence while counting as "coverage."

```csharp
// ❌ NO ASSERTION — always passes
[Fact]
public async Task Handle_ShouldWork()
{
    await _sut.Handle(command, CancellationToken.None);
}
```

### Tier 2: Weak Assertion (⚠️ Low Confidence)

Asserts on existence but not correctness. Catches null reference exceptions but misses logic bugs.

```csharp
// ⚠️ WEAK — only checks "something was returned"
[Fact]
public async Task GetEscrow_ShouldReturnResult()
{
    var result = await _sut.Handle(query, CancellationToken.None);
    result.Should().NotBeNull();  // What about the actual values?
}
```

### Tier 3: Moderate Assertion (✅ Acceptable)

Asserts on specific values but may miss important properties.

```csharp
// ✅ MODERATE — checks key property but not all important state
[Fact]
public async Task Handle_WhenValid_ShouldReturnSuccess()
{
    var result = await _sut.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
    // What about the escrow status, amount, timestamps?
}
```

### Tier 4: Strong Assertion (✅✅ High Confidence)

Asserts on all relevant business properties with meaningful values.

```csharp
// ✅✅ STRONG — verifies specific business outcomes
[Fact]
public async Task Handle_WhenFundingEscrow_ShouldTransitionToFundedWithCorrectAmount()
{
    // Arrange
    var escrowId = EscrowTransactionId.New();
    var amount = Money.From(5000m);
    SetupEscrowInPendingState(escrowId, amount);

    // Act
    var result = await _sut.Handle(new FundEscrowCommand(escrowId, amount), ct);

    // Assert
    result.IsSuccess.Should().BeTrue();
    var escrow = await GetEscrowFromDb(escrowId);
    escrow.Status.Should().Be(EscrowStatus.Funded);
    escrow.FundedAmount.Should().Be(amount);
    escrow.FundedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
}
```

## Quality Audit Process

### Step 1: Categorize Each Test

```csharp
// Scan each test method and classify:
// 1. Count assertion calls (Should, Assert, Verify)
// 2. Check assertion specificity
// 3. Rate the test

// Automated classification rules:
// - 0 assertions → Tier 1 (No Assertion)
// - Only NotBeNull/NotBeEmpty → Tier 2 (Weak)
// - Checks 1-2 specific values → Tier 3 (Moderate)
// - Checks business state + side effects → Tier 4 (Strong)
```

### Step 2: Score by Test Class

```markdown
| Test Class | Tier 1 | Tier 2 | Tier 3 | Tier 4 | Score |
|-----------|--------|--------|--------|--------|-------|
| CreateEscrowTests | 0 | 1 | 3 | 5 | 82% |
| FundEscrowTests | 2 ⚠️ | 3 | 2 | 1 | 48% |
| DisputeTests | 0 | 0 | 4 | 6 | 90% |

Score = (T2×0.25 + T3×0.5 + T4×1.0) / TotalTests × 100
```

### Step 3: Prioritize Fixes

```markdown
Priority order for fixing assertion quality:
1. Tier 1 (No Assertion) in CRITICAL business logic → ADD ASSERTIONS NOW
2. Tier 1 in HIGH business logic → Fix in current sprint
3. Tier 2 (Weak) in CRITICAL logic → Strengthen assertions
4. Tier 2 in HIGH logic → Strengthen when modifying
```

## Common Weak Assertion Patterns

### Pattern: Assert Only "Not Null"

```csharp
// ⚠️ Passes even if data is completely wrong
result.Should().NotBeNull();

// ✅ Fix: Assert on actual content
result.Should().NotBeNull();
result.EscrowId.Should().Be(expectedId);
result.Status.Should().Be(EscrowStatus.Funded);
```

### Pattern: Assert Only on Count

```csharp
// ⚠️ Correct count doesn't mean correct content
escrows.Should().HaveCount(3);

// ✅ Fix: Also verify content
escrows.Should().HaveCount(3)
    .And.OnlyContain(e => e.Status == EscrowStatus.Active)
    .And.BeInDescendingOrder(e => e.CreatedAt);
```

### Pattern: Assert Only on Type

```csharp
// ⚠️ Correct type doesn't mean correct values
result.Should().BeOfType<EscrowDto>();

// ✅ Fix: Assert on properties
result.Should().BeOfType<EscrowDto>()
    .Which.Amount.Should().Be(Money.From(5000m));
```

### Pattern: Assert with Magic Numbers

```csharp
// ⚠️ What does 25 mean? Why 25?
fee.Value.Should().Be(25m);

// ✅ Fix: Make the expected value derived from the input
var expectedFee = amount.Value * StandardFeeRate; // 1000 * 0.025 = 25
fee.Value.Should().Be(expectedFee, because: "standard fee is 2.5% of amount");
```

## Assertion Best Practices

1. **Use `because` parameter** — Every non-obvious assertion should explain why
2. **Use `AssertionScope`** — Report all failures, not just the first one
3. **Assert on behavior, not implementation** — What changed? Not how it changed
4. **One concept per test** — 2-3 related assertions OK, 7+ is a smell
5. **Use `BeEquivalentTo`** — For complex object comparison, ignore irrelevant properties
