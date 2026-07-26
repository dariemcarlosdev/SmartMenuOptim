# Modern C# 13 Reference

> **Load when:** Using records, pattern matching, nullable types, primary constructors, or collection expressions.

## Primary Constructors

Use for dependency injection and concise class definitions.

```csharp
// Service with DI — primary constructor captures dependencies
public sealed class EscrowService(
    IEscrowRepository repository,
    IUnitOfWork unitOfWork,
    IOptions<EscrowOptions> options,
    ILogger<EscrowService> logger)
{
    private readonly EscrowOptions _options = options.Value;

    public async Task<Result<Guid>> CreateAsync(
        string buyerId, string sellerId, Money amount, CancellationToken ct)
    {
        if (amount.Value > _options.MaxTransactionAmount)
            return Result<Guid>.Failure("Amount exceeds maximum");

        var escrow = Escrow.Create(buyerId, sellerId, amount);
        await repository.AddAsync(escrow, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Created escrow {EscrowId}", escrow.Id);
        return Result<Guid>.Success(escrow.Id.Value);
    }
}

// Primary constructor on a record (combines DI and data)
public sealed record CreateEscrowCommand(
    string BuyerId,
    string SellerId,
    decimal Amount,
    string Currency) : IRequest<Result<Guid>>;
```

## Records and Value Objects

```csharp
// Immutable DTO
public sealed record EscrowSummaryDto(
    Guid Id,
    decimal Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);

// Value object with behavior
public readonly record struct Money(decimal Value, string Currency)
{
    public static Money USD(decimal amount) => new(amount, "USD");
    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
        return this with { Value = Value + other.Value };
    }

    public override string ToString() => $"{Value:N2} {Currency}";
}

// Strongly-typed ID
public readonly record struct EscrowId(Guid Value)
{
    public static EscrowId New() => new(Guid.NewGuid());
    public override string ToString() => $"ESC-{Value:N}";
}
```

## Pattern Matching

### Switch Expressions

```csharp
// Exhaustive switch over enum
public static string GetStatusBadge(EscrowStatus status) => status switch
{
    EscrowStatus.Pending   => "badge-warning",
    EscrowStatus.Funded    => "badge-info",
    EscrowStatus.Released  => "badge-success",
    EscrowStatus.Disputed  => "badge-danger",
    EscrowStatus.Cancelled => "badge-secondary",
    _ => throw new UnreachableException($"Unknown status: {status}")
};

// Property pattern matching
public static decimal CalculateFee(Escrow escrow) => escrow switch
{
    { Amount.Value: < 100 }     => 1.00m,
    { Amount.Value: < 1000 }    => escrow.Amount.Value * 0.02m,
    { Amount.Value: < 10000 }   => escrow.Amount.Value * 0.015m,
    { Amount.Currency: "EUR" }  => escrow.Amount.Value * 0.01m,
    _                           => escrow.Amount.Value * 0.01m
};
```

### List Patterns

```csharp
// Validate command-line arguments
static string ParseArgs(string[] args) => args switch
{
    ["--help"]                  => ShowHelp(),
    ["--version"]               => ShowVersion(),
    ["create", var buyer, var seller, var amount] 
                                => CreateEscrow(buyer, seller, amount),
    ["release", var id]         => ReleaseEscrow(id),
    [var unknown, ..]           => $"Unknown command: {unknown}",
    []                          => ShowHelp()
};
```

## Collection Expressions

```csharp
// Inline collection creation
IReadOnlyList<string> currencies = ["USD", "EUR", "GBP", "CAD", "AUD"];

// Spread operator
int[] first = [1, 2, 3];
int[] second = [4, 5, 6];
int[] combined = [..first, ..second]; // [1, 2, 3, 4, 5, 6]

// Empty collection
List<Escrow> empty = [];

// In method returns
public static IReadOnlyList<ValidationError> Validate(CreateEscrowCommand cmd) =>
    [
        ..ValidateBuyerId(cmd.BuyerId),
        ..ValidateSellerId(cmd.SellerId),
        ..ValidateAmount(cmd.Amount),
    ];
```

## Nullable Reference Types

```csharp
// Non-nullable by default — must handle nulls explicitly
public sealed class EscrowRepository(EscrowDbContext context) : IEscrowRepository
{
    public async Task<Escrow?> FindByIdAsync(EscrowId id, CancellationToken ct)
    {
        return await context.Escrows.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Escrow> GetByIdAsync(EscrowId id, CancellationToken ct)
    {
        return await FindByIdAsync(id, ct) 
            ?? throw new EntityNotFoundException(nameof(Escrow), id);
    }
}

// Required modifier for mandatory properties
public sealed class EscrowCreateRequest
{
    public required string BuyerId { get; init; }
    public required string SellerId { get; init; }
    public required decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
}
```

## Guard Clauses

```csharp
public static class Guard
{
    public static void AgainstNullOrEmpty(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
    }

    public static void AgainstNegativeOrZero(decimal value, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
    }
}

// Usage
public static Escrow Create(string buyerId, string sellerId, Money amount)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(buyerId);
    ArgumentException.ThrowIfNullOrWhiteSpace(sellerId);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount.Value);

    return new Escrow
    {
        Id = EscrowId.New(),
        BuyerId = buyerId,
        SellerId = sellerId,
        Amount = amount,
        Status = EscrowStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
}
```

## File-Scoped Namespaces and Global Usings

```csharp
// GlobalUsings.cs
global using System.Diagnostics;
global using MediatR;
global using FluentValidation;
global using Microsoft.EntityFrameworkCore;

// All files use file-scoped namespace (single line, no nesting)
namespace EscrowApp.Domain.Entities;

public sealed class Escrow { ... }
```
