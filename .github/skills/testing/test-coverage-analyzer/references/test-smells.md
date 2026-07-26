# Test Smells — Fragile Tests and Test Coupling

## Purpose

Catalog common test smells that undermine test suite reliability, with detection strategies and fixes for each.

## Test Smell Catalog

### 1. No Assertions (The Liar)

**Description:** Test executes code but never asserts anything. Always passes regardless of behavior.

```csharp
// ❌ SMELL: No assertion
[Fact]
public async Task Handle_ShouldProcessEscrow()
{
    var command = new CreateEscrowCommand(UserId.New(), UserId.New(), Money.From(1000m));
    await _sut.Handle(command, CancellationToken.None);
    // Test always passes — no assertion!
}

// ✅ FIX: Add meaningful assertion
[Fact]
public async Task Handle_WhenValidCommand_ShouldReturnSuccessWithEscrowId()
{
    var command = new CreateEscrowCommand(UserId.New(), UserId.New(), Money.From(1000m));
    var result = await _sut.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBe(EscrowTransactionId.Empty);
}
```

**Detection:** Search for test methods without `Assert`, `Should`, `.Verify`, or `.Received`.

```bash
# Find test methods without assertions (.NET)
grep -rn "\[Fact\]\|[Theory\]" tests/ -A 20 | \
  grep -L "Should\|Assert\|Verify\|Received"
```

### 2. Brittle Tests (The Inspector)

**Description:** Tests break when implementation changes even though behavior is preserved.

```csharp
// ❌ SMELL: Asserting on exact mock call counts and internal details
_repoMock.Verify(r => r.GetByIdAsync(escrowId, ct), Times.Exactly(1));
_cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
    It.IsAny<DistributedCacheEntryOptions>(), ct), Times.Exactly(1));

// ✅ FIX: Assert on observable behavior
result.Status.Should().Be(EscrowStatus.Funded);
// Only verify critical side effects
_uowMock.Verify(u => u.SaveChangesAsync(ct), Times.Once);
```

### 3. Mystery Guest

**Description:** Test depends on external state not visible in the test body.

```csharp
// ❌ SMELL: Depends on external file
[Fact]
public void Import_ShouldParseCorrectly()
{
    var result = _parser.Parse("testdata/escrows.csv"); // Where is this file?
    result.Should().HaveCount(10);                       // Why 10?
}

// ✅ FIX: Make data explicit in the test
[Fact]
public void Import_WhenThreeRows_ShouldParseThreeEscrows()
{
    var csv = "buyer,seller,amount\nA,B,100\nC,D,200\nE,F,300";
    var result = _parser.Parse(csv);
    result.Should().HaveCount(3);
}
```

### 4. Eager Test (God Test)

**Description:** Single test verifies multiple unrelated behaviors.

```csharp
// ❌ SMELL: Tests creation, validation, persistence, AND notification
[Fact]
public async Task CreateEscrow_ShouldDoEverything()
{
    var result = await _sut.Handle(command, ct);
    result.IsSuccess.Should().BeTrue();
    result.Value.Status.Should().Be(EscrowStatus.Pending);
    result.Value.Amount.Should().Be(Money.From(1000m));
    _repoMock.Verify(r => r.AddAsync(It.IsAny<EscrowTransaction>(), ct));
    _uowMock.Verify(u => u.SaveChangesAsync(ct));
    _notifierMock.Verify(n => n.SendAsync(It.IsAny<Notification>(), ct));
}

// ✅ FIX: Split into focused tests
[Fact] public async Task Handle_WhenValid_ShouldReturnSuccess() { }
[Fact] public async Task Handle_WhenValid_ShouldPersistEscrow() { }
[Fact] public async Task Handle_WhenValid_ShouldSendNotification() { }
```

### 5. Test Logic (The Brain)

**Description:** Tests contain loops, conditionals, or complex computation.

```csharp
// ❌ SMELL: Logic in test — the test itself might have bugs
[Fact]
public void CalculateFees_ForAllTypes_ShouldBeCorrect()
{
    foreach (var type in Enum.GetValues<EscrowType>())
    {
        var expected = type switch
        {
            EscrowType.Standard => 25m,
            EscrowType.Premium => 15m,
            _ => 10m
        };
        _sut.Calculate(type, Money.From(1000m)).Value.Should().Be(expected);
    }
}

// ✅ FIX: Use parameterized test with explicit values
[Theory]
[InlineData(EscrowType.Standard, 25)]
[InlineData(EscrowType.Premium, 15)]
[InlineData(EscrowType.Enterprise, 10)]
public void CalculateFee_ShouldReturnExpected(EscrowType type, decimal expectedFee)
{
    var fee = _sut.Calculate(type, Money.From(1000m));
    fee.Value.Should().Be(expectedFee);
}
```

### 6. Commented-Out Tests

**Description:** Tests disabled via comments or `[Skip]` attributes without clear reason.

```csharp
// ❌ SMELL: Why is this commented out?
// [Fact]
// public void Fund_WhenExpired_ShouldThrow() { ... }

[Fact(Skip = "Broken after refactoring")]  // When will this be fixed?
public void Release_WhenDisputed_ShouldRequireResolution() { }
```

**Fix:** Either fix and re-enable, or delete with a comment explaining why the behavior is no longer relevant.

### 7. Sleep/Delay

**Description:** Tests use `Thread.Sleep` or `Task.Delay` for timing.

```csharp
// ❌ SMELL: Flaky and slow
[Fact]
public async Task Cache_ShouldExpireAfterTimeout()
{
    _cache.Set("key", "value");
    await Task.Delay(TimeSpan.FromSeconds(5)); // Slow and unreliable!
    _cache.Get("key").Should().BeNull();
}

// ✅ FIX: Use FakeTimeProvider (.NET 8+)
[Fact]
public void Cache_ShouldExpireAfterTimeout()
{
    var timeProvider = new FakeTimeProvider();
    var cache = new TimedCache(timeProvider);
    cache.Set("key", "value", TimeSpan.FromMinutes(5));
    
    timeProvider.Advance(TimeSpan.FromMinutes(6));
    cache.Get("key").Should().BeNull();
}
```

## Smell Detection Checklist

| Smell | Automated Detection | Grep Pattern |
|-------|-------------------|-------------|
| No Assertions | Search for test methods without assertion keywords | `[Fact]` blocks missing `Should\|Assert\|Verify` |
| Brittle Tests | Count `Verify(...)` calls per test (>3 = smell) | `Times.Exactly\|Times.Once` count |
| Mystery Guest | Search for file path strings in tests | `File.Read\|Path.Combine` in test files |
| Eager Test | Count assertions per test (>5 = smell) | `Should()` count per `[Fact]` |
| Test Logic | Search for loops/conditionals in tests | `foreach\|for\|if\|switch` in test files |
| Commented Tests | Search for commented `[Fact]` or `[Theory]` | `//.*\[Fact\]\|Skip =` |
| Sleep/Delay | Search for timing calls | `Thread.Sleep\|Task.Delay` in test files |
