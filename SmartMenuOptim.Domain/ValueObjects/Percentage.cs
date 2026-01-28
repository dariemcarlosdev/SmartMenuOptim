namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a percentage value.
/// </summary>
/// <remarks>
/// Useful for discounts, tax rates, service charges, etc.
/// Value is stored as a decimal (e.g., 0.15 for 15%).
/// </remarks>
public sealed record Percentage
{
    /// <summary>
    /// Gets the percentage value as a decimal (0.0 to 1.0).
    /// </summary>
    public decimal Value { get; }

    /// <summary>
    /// Gets the percentage value as a whole number (0 to 100).
    /// </summary>
    public decimal AsWholeNumber => Value * 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="Percentage"/> class.
    /// </summary>
    /// <param name="value">The percentage value as a decimal (0.0 to 1.0).</param>
    /// <param name="isWholeNumber">If true, treats value as 0-100 range; otherwise 0.0-1.0 range.</param>
    /// <exception cref="ArgumentException">Thrown when the percentage is out of valid range.</exception>
    public Percentage(decimal value, bool isWholeNumber = false)
    {
        if (isWholeNumber)
        {
            if (value < 0 || value > 100)
                throw new ArgumentException("Percentage must be between 0 and 100.", nameof(value));

            Value = value / 100;
        }
        else
        {
            if (value < 0 || value > 1)
                throw new ArgumentException("Percentage must be between 0.0 and 1.0.", nameof(value));

            Value = value;
        }
    }

    /// <summary>
    /// Creates a percentage from a whole number (0-100).
    /// </summary>
    public static Percentage FromWholeNumber(decimal value) => new Percentage(value, isWholeNumber: true);

    /// <summary>
    /// Creates a percentage from a decimal (0.0-1.0).
    /// </summary>
    public static Percentage FromDecimal(decimal value) => new Percentage(value, isWholeNumber: false);

    /// <summary>
    /// Calculates the percentage of a given amount.
    /// </summary>
    public decimal Of(decimal amount) => amount * Value;

    /// <summary>
    /// Applies the percentage as a discount to the given amount.
    /// </summary>
    public decimal ApplyDiscount(decimal amount) => amount * (1 - Value);

    /// <summary>
    /// Applies the percentage as a markup to the given amount.
    /// </summary>
    public decimal ApplyMarkup(decimal amount) => amount * (1 + Value);

    /// <summary>
    /// Zero percentage (0%).
    /// </summary>
    public static Percentage Zero => new Percentage(0);

    /// <summary>
    /// Full percentage (100%).
    /// </summary>
    public static Percentage Full => new Percentage(1);

    public override string ToString() => $"{AsWholeNumber:N2}%";

    public static implicit operator decimal(Percentage percentage) => percentage.Value;
}