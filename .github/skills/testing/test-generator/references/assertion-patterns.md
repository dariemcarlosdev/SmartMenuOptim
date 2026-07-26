# Assertion Patterns — FluentAssertions Best Practices

## Purpose

Guide the use of FluentAssertions to write expressive, readable, and diagnostically useful assertions in .NET tests.

## Why FluentAssertions?

```csharp
// ❌ Built-in xUnit — poor failure message
Assert.Equal("Funded", escrow.Status.ToString());
// Failure: Assert.Equal() Failure. Expected: "Funded", Actual: "Pending"

// ✅ FluentAssertions — rich failure context
escrow.Status.Should().Be(EscrowStatus.Funded,
    because: "escrow should transition to Funded after successful payment");
// Failure: Expected escrow.Status to be EscrowStatus.Funded
//   because escrow should transition to Funded after successful payment,
//   but found EscrowStatus.Pending.
```

## Common Assertion Patterns

### Value Assertions

```csharp
// Equality
result.Amount.Should().Be(Money.From(5000m));
result.Status.Should().Be(EscrowStatus.Funded);
result.Should().NotBeNull();

// Numeric ranges
fee.Value.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(100m);
escrow.DaysRemaining.Should().BeInRange(1, 30);

// String assertions
error.Message.Should().Contain("insufficient funds");
user.Email.Should().EndWith("@nextruzt.io");
name.Should().NotBeNullOrWhiteSpace();

// DateTime assertions
escrow.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
escrow.ExpiresAt.Should().BeAfter(escrow.CreatedAt);
```

### Collection Assertions

```csharp
// Collection content
var escrows = await handler.Handle(query, CancellationToken.None);
escrows.Should().NotBeEmpty()
    .And.HaveCount(3)
    .And.OnlyContain(e => e.Status == EscrowStatus.Active);

// Ordering
escrows.Should().BeInDescendingOrder(e => e.CreatedAt);

// Specific elements
escrows.Should().ContainSingle(e => e.Id == expectedId);

// Equivalency (deep comparison, ignoring order)
actualList.Should().BeEquivalentTo(expectedList, options => options
    .WithStrictOrdering()
    .Excluding(e => e.CreatedAt));
```

### Exception Assertions

```csharp
// Sync exception
var act = () => escrow.Release(unauthorizedUser);
act.Should().Throw<UnauthorizedAccessException>()
    .WithMessage("*not authorized*");

// Async exception
var act = () => handler.Handle(invalidCommand, CancellationToken.None);
await act.Should().ThrowAsync<ValidationException>()
    .Where(e => e.Errors.Any(err => err.PropertyName == "Amount"));

// Should NOT throw
var act = () => handler.Handle(validCommand, CancellationToken.None);
await act.Should().NotThrowAsync();
```

### Object Graph Assertions

```csharp
// Equivalency — compare by value, not reference
actual.Should().BeEquivalentTo(expected, options => options
    .Excluding(e => e.Id)            // Ignore auto-generated fields
    .Excluding(e => e.CreatedAt)     // Ignore timestamps
    .Using<Money>(ctx =>             // Custom comparison for Money
        ctx.Subject.Value.Should().BeApproximately(
            ctx.Expectation.Value, 0.01m))
    .WhenTypeIs<Money>());
```

### Type and Inheritance Assertions

```csharp
result.Should().BeOfType<EscrowFundedEvent>();
result.Should().BeAssignableTo<IDomainEvent>();
result.Should().NotBeOfType<EscrowCancelledEvent>();
```

## Anti-Patterns to Avoid

### ❌ Assertion-Free Tests

```csharp
// BAD — no assertion, always passes
[Fact]
public async Task Handle_ShouldWork()
{
    await _sut.Handle(command, CancellationToken.None);
    // Where's the assertion?!
}
```

### ❌ Weak Assertions

```csharp
// BAD — only checks not null, doesn't verify correctness
result.Should().NotBeNull();

// GOOD — verify actual business state
result.Should().NotBeNull();
result.Status.Should().Be(EscrowStatus.Funded);
result.Amount.Should().Be(Money.From(5000m));
```

### ❌ Over-Assertion

```csharp
// BAD — testing too many things in one test
[Fact]
public async Task Handle_ShouldDoEverything()
{
    var result = await _sut.Handle(command, ct);
    result.IsSuccess.Should().BeTrue();
    result.Value.Status.Should().Be(EscrowStatus.Created);
    result.Value.Amount.Should().Be(Money.From(100m));
    _repoMock.Verify(r => r.AddAsync(It.IsAny<EscrowTransaction>(), ct), Times.Once);
    _uowMock.Verify(u => u.SaveChangesAsync(ct), Times.Once);
    _notifierMock.Verify(n => n.SendAsync(It.IsAny<Notification>(), ct), Times.Once);
    _auditMock.Verify(a => a.LogAsync(It.IsAny<string>(), ct), Times.Once);
    // 7 assertions = 7 reasons this test could fail. Split into focused tests.
}
```

## Assertion Scope — Multiple Assertions with Full Reporting

```csharp
using (new AssertionScope())
{
    escrow.Status.Should().Be(EscrowStatus.Funded);
    escrow.Amount.Should().Be(Money.From(5000m));
    escrow.FundedAt.Should().NotBeNull();
}
// Reports ALL failures at once instead of stopping at the first
```
