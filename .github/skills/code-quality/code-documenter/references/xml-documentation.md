# C# XML Documentation Reference

Complete guide for generating XML doc comments for .NET public APIs.

## Required Tags for Public Members

### Class / Interface / Record
```csharp
/// <summary>
/// Manages the escrow lifecycle from creation through funding, release, and dispute resolution.
/// </summary>
/// <remarks>
/// Registered as <see cref="ServiceLifetime.Scoped"/>. Requires
/// <see cref="IEscrowRepository"/> and <see cref="IPaymentGateway"/> in DI.
/// Thread-safe for concurrent access within a single request scope.
/// </remarks>
public sealed class EscrowService : IEscrowService
```

### Method
```csharp
/// <summary>
/// Creates a new escrow transaction between buyer and seller with the specified terms.
/// </summary>
/// <param name="command">
/// The creation command containing buyer ID, seller ID, amount, currency, and deadline.
/// </param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the new <see cref="EscrowId"/> on success,
/// or validation errors on failure.
/// </returns>
/// <exception cref="DuplicateEscrowException">
/// Thrown when an escrow with identical terms already exists within the cooldown period.
/// </exception>
/// <example>
/// <code>
/// var result = await escrowService.CreateAsync(
///     new CreateEscrowCommand(buyerId, sellerId, Money.USD(500), deadline),
///     cancellationToken);
/// if (result.IsSuccess)
///     logger.LogInformation("Escrow {Id} created", result.Value);
/// </code>
/// </example>
public async Task<Result<EscrowId>> CreateAsync(
    CreateEscrowCommand command,
    CancellationToken cancellationToken = default)
```

### Property
```csharp
/// <summary>
/// Gets the current escrow status in the lifecycle state machine.
/// </summary>
/// <value>
/// One of <see cref="EscrowStatus.Draft"/>, <see cref="EscrowStatus.Funded"/>,
/// <see cref="EscrowStatus.Released"/>, or <see cref="EscrowStatus.Disputed"/>.
/// Defaults to <see cref="EscrowStatus.Draft"/> on creation.
/// </value>
public EscrowStatus Status { get; private set; }
```

### Enum
```csharp
/// <summary>
/// Represents the lifecycle stages of an escrow transaction.
/// </summary>
public enum EscrowStatus
{
    /// <summary>Escrow created but not yet funded by the buyer.</summary>
    Draft = 0,

    /// <summary>Buyer has deposited funds; awaiting seller fulfillment.</summary>
    Funded = 1,

    /// <summary>Funds released to seller after buyer confirmation.</summary>
    Released = 2,

    /// <summary>Transaction under dispute; funds held pending resolution.</summary>
    Disputed = 3,

    /// <summary>Escrow cancelled; funds returned to buyer.</summary>
    Cancelled = 99
}
```

### Constructor
```csharp
/// <summary>
/// Initializes a new <see cref="EscrowService"/> with required dependencies.
/// </summary>
/// <param name="repository">The escrow persistence store.</param>
/// <param name="paymentGateway">The payment processing gateway.</param>
/// <param name="logger">The structured logger instance.</param>
/// <exception cref="ArgumentNullException">
/// Any parameter is <see langword="null"/>.
/// </exception>
public EscrowService(
    IEscrowRepository repository,
    IPaymentGateway paymentGateway,
    ILogger<EscrowService> logger)
```

## Cross-Reference Patterns

```csharp
/// <see cref="EscrowService"/>                    — link to type
/// <see cref="EscrowService.CreateAsync"/>         — link to method
/// <see cref="Result{T}"/>                         — link to generic type
/// <see langword="null"/>                          — keyword reference
/// <see langword="true"/>                          — keyword reference
/// <paramref name="command"/>                      — reference to parameter
/// <typeparamref name="T"/>                        — reference to type parameter
/// <inheritdoc/>                                   — inherit from interface/base
/// <inheritdoc cref="IEscrowService.CreateAsync"/> — inherit from specific member
```

## Documentation Priority Matrix

| Priority | Target | Example |
|----------|--------|---------|
| P0 | Public API endpoints | Controller actions, Minimal API handlers |
| P0 | Public interfaces | `IEscrowService`, `IEscrowRepository` |
| P1 | Public classes | `EscrowService`, `EscrowValidator` |
| P1 | Public methods with params | `CreateAsync(command, ct)` |
| P2 | Public properties | Non-obvious computed or validated properties |
| P2 | Public enums | Domain status enums with business meaning |
| P3 | Complex private methods | Only when logic is genuinely non-obvious |

## Enabling XML Doc Warnings

```xml
<!-- In .csproj — treat missing docs as build warnings -->
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Remove to enforce -->
</PropertyGroup>
```
