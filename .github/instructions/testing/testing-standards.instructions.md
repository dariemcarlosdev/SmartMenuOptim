---
applyTo: "**/*Tests*/**/*.cs, **/*Test*/**/*.cs"
---

# Testing Standards — NexTruzt.io Escrow Platform

## Framework & Tooling

- **Test framework:** xUnit — use `[Fact]` for single cases, `[Theory]` with `[InlineData]` or `[MemberData]` for parameterized tests.
- **Assertions:** FluentAssertions — prefer `.Should().Be()`, `.Should().Throw<T>()` over xUnit's `Assert.*`.
- **Mocking:** Moq or NSubstitute — pick one per project, do not mix.
- **Integration:** `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) for API-level tests.
- **Database:** Testcontainers for PostgreSQL — spin up a real database per test class for integration tests.

## Naming Convention

Use the pattern: **MethodName_Scenario_ExpectedResult**

```csharp
// ✅ Clear intent
public async Task HoldFunds_ValidTransaction_ReturnsSuccess()
public async Task HoldFunds_InsufficientBalance_ThrowsPaymentException()
public void RaiseDispute_TransactionNotInHeldState_ThrowsInvalidEscrowStateException()
public async Task CreateTransaction_DuplicateIdempotencyKey_ReturnsConflict()

// ❌ Vague or undescriptive
public void Test1()
public async Task TestHoldFunds()
```

## Arrange-Act-Assert (AAA)

Every test must have **clearly separated** AAA sections. Use blank lines and optional comments for readability.

```csharp
[Fact]
public async Task HoldFunds_ValidTransaction_ReturnsSuccess()
{
    // Arrange
    var transaction = new EscrowTransactionBuilder()
        .WithStatus(EscrowStatus.Created)
        .WithAmount(new Money(500m, Currency.USD))
        .Build();

    var mockRepo = new Mock<IEscrowTransactionRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(transaction);

    var mockStrategy = new Mock<IFundHoldable>();
    mockStrategy.Setup(s => s.HoldAsync(transaction, It.IsAny<CancellationToken>()))
        .ReturnsAsync(FundHoldResult.Success("pi_123"));

    var handler = new HoldFundsCommandHandler(mockRepo.Object, mockStrategy.Object);

    // Act
    var result = await handler.Handle(new HoldFundsCommand(transaction.Id), CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    mockRepo.Verify(r => r.UpdateAsync(transaction, It.IsAny<CancellationToken>()), Times.Once);
}
```

## Unit Tests

### MediatR Handler Tests

- Test each command/query handler **in isolation** — inject mocked dependencies.
- Mock `IEscrowTransactionRepository` and all strategy interfaces (`IFundHoldable`, `IFundReleasable`, `IFundCancellable`).
- Verify that the handler calls the correct repository/strategy methods with expected arguments.
- Test both success and failure paths — assert thrown exceptions with FluentAssertions.

### Domain Model Tests

- Test aggregate root methods directly — `EscrowTransaction.RaiseDispute()`, `EscrowTransaction.HoldFunds()`.
- Verify that **domain events** are raised correctly after state transitions.
- Verify that **invariant violations** throw the expected domain exceptions.
- Test Value Object validation: `Money` rejects negative amounts, `IdempotencyKey` rejects empty strings.

### Validation Rule Tests

- Test FluentValidation validators independently — call `validator.TestValidateAsync(model)`.
- Cover required fields, boundary values, format constraints, and cross-field rules.

## Integration Tests

### API / Endpoint Tests

- Use `WebApplicationFactory<Program>` to bootstrap the application.
- Override DI registrations to swap real infrastructure with test doubles where appropriate.
- Use **Testcontainers** for PostgreSQL so integration tests run against a real database engine.
- Test the full request pipeline: routing → model binding → validation → handler → persistence → response.

```csharp
public sealed class EscrowApiTests : IClassFixture<EscrowWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EscrowApiTests(EscrowWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTransaction_ValidPayload_Returns201()
    {
        // Arrange
        var payload = new CreateTransactionRequest(/* ... */);

        // Act
        var response = await _client.PostAsJsonAsync("/api/escrow/transactions", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### Database Integration Tests

- Verify EF Core mappings, constraints, and indexes against a real PostgreSQL instance.
- Test repository implementations end-to-end: persist, retrieve, verify.
- Each test class gets a **fresh database** (Testcontainers per fixture) — never share mutable state across tests.

## What to Test

| Layer | What to Test |
|---|---|
| Domain Models | Constructor validation, behavior methods, state transitions, domain event emission, Value Object equality |
| MediatR Handlers | Business logic orchestration, correct repository/strategy calls, error handling |
| Strategy Implementations | `StripeFundHoldStrategy` with mocked Stripe SDK, correct PaymentIntent parameters |
| FluentValidation Rules | Required fields, boundary values, format constraints |
| API Endpoints (integration) | Full HTTP request/response cycle, status codes, response bodies, error payloads |

## What NOT to Test

- **EF Core mappings directly** — these are validated by integration tests against a real database.
- **Private methods** — test through the public interface that exercises them.
- **Framework behavior** — do not test that ASP.NET Core routing works or that DI resolves correctly (unless custom logic is involved).
- **Third-party library internals** — mock the boundary, don't test Stripe SDK behavior.

## Test Data — Builder Pattern

Use builders for complex domain objects to keep tests readable and decoupled from constructor changes.

```csharp
public sealed class EscrowTransactionBuilder
{
    private Guid _id = Guid.NewGuid();
    private EscrowStatus _status = EscrowStatus.Created;
    private Money _amount = new(100m, Currency.USD);
    private Actor? _buyer;
    private Actor? _seller;

    public EscrowTransactionBuilder WithStatus(EscrowStatus status) { _status = status; return this; }
    public EscrowTransactionBuilder WithAmount(Money amount) { _amount = amount; return this; }
    public EscrowTransactionBuilder WithBuyer(Actor buyer) { _buyer = buyer; return this; }
    public EscrowTransactionBuilder WithSeller(Actor seller) { _seller = seller; return this; }

    public EscrowTransaction Build() => new(_id, _amount, _buyer!, _seller!, _status);
}
```

## Coverage Targets

- **Critical payment flows** (hold, release, cancel, dispute): **>90% line coverage**.
- **Domain model invariants**: **100%** — every state transition path must be tested.
- **API endpoints**: every documented status code (201, 400, 404, 409, 500) must have at least one test.
- Coverage is a guideline, not a goal — a well-tested critical path is more valuable than chasing a vanity metric across utility code.

## General Rules

- Tests must be **deterministic** — no dependency on wall-clock time, random data, or external services.
- Use `CancellationToken.None` in unit tests; integration tests should test cancellation behavior explicitly.
- Clean up resources in `Dispose` / `IAsyncDisposable` — especially Testcontainers and `HttpClient` instances.
- Run tests in parallel by default (xUnit's default) — ensure no shared mutable state between test classes.
