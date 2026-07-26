# Creational Patterns — Factory, Builder, Singleton

## When to Consider Creational Patterns

- Object construction is complex (many parameters, conditional logic)
- You need to decouple client code from concrete types
- Object creation logic is duplicated across multiple call sites

## Factory Method / Abstract Factory

**Intent:** Define an interface for creating objects; let subclasses or configuration decide which concrete class to instantiate.

**Use when:** You have 3+ concrete types selected at runtime (payment providers, notification channels, export formats).

### .NET Implementation — Escrow Payment Provider Factory

```csharp
// Application layer — factory interface
public interface IPaymentProviderFactory
{
    IPaymentProvider Create(PaymentMethod method);
}

// Infrastructure layer — factory implementation
internal sealed class PaymentProviderFactory(IServiceProvider sp) : IPaymentProviderFactory
{
    public IPaymentProvider Create(PaymentMethod method) => method switch
    {
        PaymentMethod.Stripe => sp.GetRequiredKeyedService<IPaymentProvider>("stripe"),
        PaymentMethod.PayPal => sp.GetRequiredKeyedService<IPaymentProvider>("paypal"),
        PaymentMethod.BankTransfer => sp.GetRequiredKeyedService<IPaymentProvider>("bank"),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}

// DI Registration
services.AddKeyedScoped<IPaymentProvider, StripeProvider>("stripe");
services.AddKeyedScoped<IPaymentProvider, PayPalProvider>("paypal");
services.AddKeyedScoped<IPaymentProvider, BankTransferProvider>("bank");
services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
```

**YAGNI gate:** Do you have ≥3 implementations now (not "might need later")? If only 2, use simple DI registration.

## Builder Pattern

**Intent:** Separate construction of a complex object from its representation.

**Use when:** An object has many optional parameters, or construction requires multiple steps.

### .NET Implementation — Escrow Transaction Builder

```csharp
public sealed class EscrowTransactionBuilder
{
    private Money? _amount;
    private UserId? _buyer;
    private UserId? _seller;
    private EscrowTerms? _terms;
    private TimeSpan _expiresIn = TimeSpan.FromDays(30);

    public EscrowTransactionBuilder WithAmount(Money amount)
    {
        _amount = amount;
        return this;
    }

    public EscrowTransactionBuilder WithParties(UserId buyer, UserId seller)
    {
        _buyer = buyer;
        _seller = seller;
        return this;
    }

    public EscrowTransactionBuilder WithTerms(EscrowTerms terms)
    {
        _terms = terms;
        return this;
    }

    public EscrowTransactionBuilder ExpiresIn(TimeSpan duration)
    {
        _expiresIn = duration;
        return this;
    }

    public EscrowTransaction Build()
    {
        ArgumentNullException.ThrowIfNull(_amount);
        ArgumentNullException.ThrowIfNull(_buyer);
        ArgumentNullException.ThrowIfNull(_seller);

        return new EscrowTransaction(_amount, _buyer, _seller, _terms, _expiresIn);
    }
}
```

**Prefer `record` with `required` properties when:** All parameters are known at construction and you don't need step-by-step building.

```csharp
// Simpler alternative for DTOs — no builder needed
public sealed record CreateEscrowRequest
{
    public required Money Amount { get; init; }
    public required UserId BuyerId { get; init; }
    public required UserId SellerId { get; init; }
}
```

## Singleton (Use Sparingly)

**Intent:** Ensure a class has exactly one instance.

**In .NET:** Prefer DI singleton registration over the GoF Singleton pattern:

```csharp
// ✅ Prefer: DI-managed singleton (testable, replaceable)
services.AddSingleton<ICurrencyRateCache, CurrencyRateCache>();

// ❌ Avoid: Classic Singleton (hard to test, hidden dependency)
public sealed class CurrencyRateCache
{
    public static CurrencyRateCache Instance { get; } = new();
    private CurrencyRateCache() { }
}
```

## Decision Matrix

| Problem | Pattern | Complexity | When to Use |
|---------|---------|-----------|-------------|
| Runtime type selection (3+ types) | Factory Method | Low-Medium | Payment providers, notification channels |
| Complex object construction | Builder | Medium | Entities with many optional fields |
| Single instance needed | DI Singleton | None (DI) | Caches, configuration, connection pools |
| Family of related objects | Abstract Factory | High | **Rarely justified** — prefer simple factories |
