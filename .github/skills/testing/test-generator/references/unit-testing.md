# Unit Testing — xUnit, Moq, NSubstitute Patterns

## Purpose

Provide patterns and templates for unit testing .NET code with xUnit, Moq/NSubstitute, and FluentAssertions following the Arrange-Act-Assert pattern.

## Test Class Structure

```csharp
using FluentAssertions;
using Moq;
using Xunit;

namespace EscrowApp.Application.Tests.Escrow;

public sealed class FundEscrowHandlerTests
{
    // Dependencies
    private readonly Mock<IEscrowRepository> _repoMock = new();
    private readonly Mock<IPaymentGateway> _paymentMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    // System Under Test
    private readonly FundEscrowHandler _sut;

    public FundEscrowHandlerTests()
    {
        _sut = new FundEscrowHandler(
            _repoMock.Object,
            _paymentMock.Object,
            _uowMock.Object);
    }
}
```

## Happy Path Patterns

```csharp
[Fact]
public async Task Handle_WhenValidFundCommand_ShouldUpdateEscrowAndSave()
{
    // Arrange
    var escrowId = EscrowTransactionId.New();
    var amount = Money.From(5000m);
    var escrow = EscrowTransaction.Create(UserId.New(), UserId.New(), amount);

    _repoMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(escrow);
    _paymentMock.Setup(p => p.ProcessAsync(amount, It.IsAny<CancellationToken>()))
        .ReturnsAsync(PaymentResult.Success());

    var command = new FundEscrowCommand(escrowId, amount);

    // Act
    var result = await _sut.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

## Edge Case Patterns

### Null/Empty Input Testing

```csharp
[Fact]
public async Task Handle_WhenEscrowNotFound_ShouldReturnNotFound()
{
    // Arrange
    var escrowId = EscrowTransactionId.New();
    _repoMock.Setup(r => r.GetByIdAsync(escrowId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((EscrowTransaction?)null);

    // Act
    var result = await _sut.Handle(new FundEscrowCommand(escrowId, Money.From(100m)),
        CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be("Escrow not found");
}
```

### Parameterized Tests with Theory

```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(-100.50)]
public async Task Handle_WhenInvalidAmount_ShouldReturnValidationError(decimal amount)
{
    // Arrange
    var command = new FundEscrowCommand(EscrowTransactionId.New(), Money.From(amount));

    // Act
    var result = await _sut.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("amount");
}
```

### Class Data for Complex Scenarios

```csharp
public sealed class InvalidEscrowStatesData : TheoryData<EscrowStatus>
{
    public InvalidEscrowStatesData()
    {
        Add(EscrowStatus.Released);
        Add(EscrowStatus.Disputed);
        Add(EscrowStatus.Cancelled);
        Add(EscrowStatus.Expired);
    }
}

[Theory]
[ClassData(typeof(InvalidEscrowStatesData))]
public async Task Handle_WhenEscrowInInvalidState_ShouldReturnError(EscrowStatus status)
{
    // Arrange
    var escrow = CreateEscrowWithStatus(status);
    _repoMock.Setup(r => r.GetByIdAsync(escrow.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(escrow);

    // Act & Assert
    var result = await _sut.Handle(new FundEscrowCommand(escrow.Id, Money.From(100m)),
        CancellationToken.None);
    result.IsFailure.Should().BeTrue();
}
```

## Error Path Patterns

```csharp
[Fact]
public async Task Handle_WhenPaymentGatewayThrows_ShouldNotSaveAndReturnError()
{
    // Arrange
    var escrow = CreateValidEscrow();
    _repoMock.Setup(r => r.GetByIdAsync(escrow.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(escrow);
    _paymentMock.Setup(p => p.ProcessAsync(It.IsAny<Money>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new PaymentGatewayException("Connection refused"));

    // Act
    var act = () => _sut.Handle(new FundEscrowCommand(escrow.Id, Money.From(100m)),
        CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<PaymentGatewayException>();
    _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
}
```

## Moq vs. NSubstitute Quick Reference

| Operation | Moq | NSubstitute |
|-----------|-----|-------------|
| Create mock | `new Mock<IFoo>()` | `Substitute.For<IFoo>()` |
| Setup return | `.Setup(x => x.Method()).Returns(val)` | `.Method().Returns(val)` |
| Setup async | `.ReturnsAsync(val)` | `.Returns(Task.FromResult(val))` |
| Setup throws | `.Throws(new Exception())` | `.Throws(new Exception())` |
| Verify called | `.Verify(x => x.Method(), Times.Once)` | `.Received(1).Method()` |
| Verify not called | `.Verify(..., Times.Never)` | `.DidNotReceive().Method()` |
| Any argument | `It.IsAny<T>()` | `Arg.Any<T>()` |
