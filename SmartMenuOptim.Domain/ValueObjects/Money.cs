namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency.
/// </summary>
/// <remarks>
/// Ensures proper handling of currency amounts with precision and currency code validation.
/// </remarks>
public sealed record Money
{
    /// <summary>
    /// Gets the monetary amount associated with the transaction.
    /// init indicates that the property can only be set during object initialization.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the ISO 4217 currency code (e.g., "USD", "EUR", "GBP").
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> class.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The ISO 4217 currency code.</param>
    /// <exception cref="ArgumentException">Thrown when currency code is invalid.</exception>
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code cannot be empty.", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.", nameof(currency));

        Amount = Math.Round(amount, 2); // Round to 2 decimal places for currency
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Initializes a new instance of the Money class for deserialization purposes.
    /// </summary>
    /// <remarks>This constructor is intended for use by JSON serialization frameworks and should not be
    /// called directly in application code.</remarks>
    [System.Text.Json.Serialization.JsonConstructor]
    private Money()
    {
        Amount = 0;
        Currency = "USD";
    }

    /// <summary>
    /// Adds two money values. Both must have the same currency.
    /// </summary>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot add money with different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Subtracts two money values. Both must have the same currency.
    /// </summary>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot subtract money with different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>
    /// Multiplies money by a factor.
    /// </summary>
    /// <remarks>
    /// SUGGESTION: Add null check for the money parameter.
    /// CONSIDERATION: For percentage calculations, consider creating a dedicated method like
    /// ApplyPercentage(decimal percentage) to make business intent clearer.
    /// </remarks>
    public static Money operator *(Money money, decimal factor) =>
        new Money(money.Amount * factor, money.Currency);

    /// <summary>
    /// Divides money by a divisor.
    /// </summary>
    /// <remarks>
    /// SUGGESTION: Add null check for the money parameter.
    /// ENHANCEMENT: Consider adding an overload that returns a decimal for splitting bills:
    /// public static decimal operator /(Money left, Money right) // Returns ratio
    /// This would allow calculations like: var ratio = totalBill / myPortion;
    /// </remarks>
    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(money.Amount / divisor, money.Currency);
    }

    // MISSING FEATURE: Comparison operators
    // SUGGESTION: Add comparison operators for business logic like discounts, thresholds, etc.:
    // public static bool operator >(Money left, Money right) { ... }
    // public static bool operator <(Money left, Money right) { ... }
    // public static bool operator >=(Money left, Money right) { ... }
    // public static bool operator <=(Money left, Money right) { ... }
    // Don't forget to validate same currency before comparing amounts!

    /// <summary>
    /// Creates a zero money value for the specified currency.
    /// </summary>
    public static Money Zero(string currency) => new Money(0, currency);

    /// <summary>
    /// Checks if the amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Checks if the amount is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Checks if the amount is negative.
    /// </summary>
    public bool IsNegative => Amount < 0;

    public override string ToString() => $"{Amount:N2} {Currency}";
}