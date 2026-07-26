# Test Data — AutoFixture, Bogus, and Test Data Strategies

## Purpose

Provide strategies for generating realistic, maintainable test data that keeps tests readable and reduces setup boilerplate.

## Strategy 1: AutoFixture — Automatic Test Data

AutoFixture generates random but valid instances, reducing boilerplate:

```csharp
using AutoFixture;
using AutoFixture.Xunit2;

public sealed class EscrowHandlerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Theory, AutoData]
    public async Task Handle_WhenValidCommand_ShouldSucceed(
        CreateEscrowCommand command) // AutoFixture generates this
    {
        // AutoFixture creates a valid command with random data
        var result = await _sut.Handle(command, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Customizing AutoFixture for Domain Types

```csharp
public sealed class DomainFixture : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register(() => Money.From(fixture.Create<decimal>() % 10000 + 1));
        fixture.Register(() => UserId.From(fixture.Create<Guid>()));
        fixture.Register(() => EscrowTransactionId.From(fixture.Create<Guid>()));

        // Avoid circular references
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList().ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }
}

// Use as attribute
public sealed class DomainAutoDataAttribute : AutoDataAttribute
{
    public DomainAutoDataAttribute()
        : base(() => new Fixture().Customize(new DomainFixture())) { }
}

[Theory, DomainAutoData]
public async Task Handle_WhenValid_ShouldSucceed(CreateEscrowCommand command)
{
    // command has valid Money, UserId, etc.
}
```

## Strategy 2: Bogus — Realistic Fake Data

Bogus generates human-readable fake data using rules:

```csharp
using Bogus;

public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    public static CreateEscrowCommand CreateValidEscrowCommand()
        => new Faker<CreateEscrowCommand>()
            .CustomInstantiator(f => new CreateEscrowCommand(
                BuyerId: UserId.From(f.Random.Guid()),
                SellerId: UserId.From(f.Random.Guid()),
                Amount: Money.From(f.Finance.Amount(100, 50000)),
                Description: f.Commerce.ProductDescription(),
                Currency: "USD"))
            .Generate();

    public static User CreateValidUser()
        => new Faker<User>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .Generate();

    public static IReadOnlyList<EscrowTransaction> CreateEscrowBatch(int count = 10)
        => Enumerable.Range(0, count)
            .Select(_ => EscrowTransaction.Create(
                UserId.From(_faker.Random.Guid()),
                UserId.From(_faker.Random.Guid()),
                Money.From(_faker.Finance.Amount(100, 50000))))
            .ToList();
}
```

## Strategy 3: Builder Pattern for Test Data

Best for complex domain entities with many states:

```csharp
public sealed class EscrowTransactionBuilder
{
    private UserId _buyerId = UserId.New();
    private UserId _sellerId = UserId.New();
    private Money _amount = Money.From(1000m);
    private EscrowStatus _status = EscrowStatus.Pending;

    public EscrowTransactionBuilder WithAmount(decimal amount)
    {
        _amount = Money.From(amount);
        return this;
    }

    public EscrowTransactionBuilder WithStatus(EscrowStatus status)
    {
        _status = status;
        return this;
    }

    public EscrowTransactionBuilder Funded()
        => WithStatus(EscrowStatus.Funded);

    public EscrowTransactionBuilder Disputed()
        => WithStatus(EscrowStatus.Disputed);

    public EscrowTransaction Build()
    {
        var escrow = EscrowTransaction.Create(_buyerId, _sellerId, _amount);
        // Use reflection or internal method to set status for testing
        if (_status != EscrowStatus.Pending)
            SetStatus(escrow, _status);
        return escrow;
    }

    public static EscrowTransactionBuilder Default() => new();
}

// Usage in tests
var escrow = EscrowTransactionBuilder.Default()
    .WithAmount(5000m)
    .Funded()
    .Build();
```

## Strategy 4: Object Mother

Centralized factory for common test scenarios:

```csharp
public static class TestEscrows
{
    public static EscrowTransaction PendingEscrow(decimal amount = 1000m)
        => EscrowTransaction.Create(UserId.New(), UserId.New(), Money.From(amount));

    public static EscrowTransaction FundedEscrow(decimal amount = 1000m)
    {
        var escrow = PendingEscrow(amount);
        escrow.Fund(Money.From(amount));
        return escrow;
    }

    public static EscrowTransaction DisputedEscrow()
    {
        var escrow = FundedEscrow();
        escrow.Dispute(UserId.New(), "Item not as described");
        return escrow;
    }
}
```

## When to Use Which Strategy

| Strategy | Best For | Trade-off |
|----------|---------|-----------|
| AutoFixture | Reducing boilerplate for simple types | Random data can be confusing in failures |
| Bogus | Realistic data for demos and complex scenarios | More setup than AutoFixture |
| Builder Pattern | Domain entities with many states | Requires upfront investment |
| Object Mother | Common reusable scenarios | Can become a god class if not curated |
| Inline constants | Simple tests with specific values | Repetitive across tests |

**NexTruzt.io recommendation:** Use **Builder Pattern** for domain entities (EscrowTransaction, User) and **Bogus** for DTOs and commands. Use **Object Mother** for common scenarios shared across test classes.
