namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a star rating value object with validation and utility methods.
/// </summary>
/// <remarks>
/// This value object ensures that ratings are always within the valid 1-5 star range
/// and provides utility methods for rating analysis and display.
/// It is immutable and defined by its value rather than identity.
/// </remarks>
public sealed record Rating
{
    /// <summary>
    /// Minimum allowed rating value (1 star).
    /// </summary>
    public const int MinValue = 1;

    /// <summary>
    /// Maximum allowed rating value (5 stars).
    /// </summary>
    public const int MaxValue = 5;

    /// <summary>
    /// Gets the star rating value (1-5).
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    /// Gets the percentage representation of the rating (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Useful for progress bars and graphical representations:
    /// - 1 star = 0.0 (0%)
    /// - 2 stars = 0.25 (25%)
    /// - 3 stars = 0.5 (50%)
    /// - 4 stars = 0.75 (75%)
    /// - 5 stars = 1.0 (100%)
    /// </remarks>
    public double Percentage => (Value - MinValue) / (double)(MaxValue - MinValue);

    /// <summary>
    /// Gets the descriptive text for the rating.
    /// </summary>
    public string Description => Value switch
    {
        1 => "Poor",
        2 => "Fair",
        3 => "Good",
        4 => "Very Good",
        5 => "Excellent",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets whether this is considered a positive rating (4-5 stars).
    /// </summary>
    public bool IsPositive => Value >= 4;

    /// <summary>
    /// Gets whether this is considered a negative rating (1-2 stars).
    /// </summary>
    public bool IsNegative => Value <= 2;

    /// <summary>
    /// Gets whether this is considered a neutral rating (3 stars).
    /// </summary>
    public bool IsNeutral => Value == 3;

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private Rating()
    {
        Value = MinValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rating"/> class.
    /// </summary>
    /// <param name="Value">The star rating value (1-5).</param>
    /// <exception cref="ArgumentException">Thrown when the rating is outside the valid range.</exception>
    public Rating(int Value)
    {
        if (Value < MinValue || Value > MaxValue)
            throw new ArgumentException($"Rating must be between {MinValue} and {MaxValue} stars.", nameof(Value));

        this.Value = Value;
    }

    /// <summary>
    /// Creates a rating from a percentage value (0.0 to 1.0).
    /// </summary>
    /// <param name="percentage">The percentage value to convert.</param>
    /// <returns>A Rating object representing the closest star rating.</returns>
    /// <exception cref="ArgumentException">Thrown when percentage is outside 0.0-1.0 range.</exception>
    public static Rating FromPercentage(double percentage)
    {
        if (percentage < 0.0 || percentage > 1.0)
            throw new ArgumentException("Percentage must be between 0.0 and 1.0.", nameof(percentage));

        // Convert percentage to star rating with proper rounding
        var value = (int)Math.Round(percentage * (MaxValue - MinValue) + MinValue);
        
        // Ensure we're within bounds (handle edge cases)
        value = Math.Max(MinValue, Math.Min(MaxValue, value));
        
        return new Rating(value);
    }

    /// <summary>
    /// Creates a rating from a decimal rating system (e.g., 4.2 out of 5).
    /// </summary>
    /// <param name="decimalRating">The decimal rating to convert.</param>
    /// <param name="maxScale">The maximum value of the decimal scale (default: 5.0).</param>
    /// <returns>A Rating object representing the closest star rating.</returns>
    /// <exception cref="ArgumentException">Thrown when decimal rating is invalid.</exception>
    public static Rating FromDecimal(double decimalRating, double maxScale = 5.0)
    {
        if (decimalRating < 0 || decimalRating > maxScale)
            throw new ArgumentException($"Decimal rating must be between 0 and {maxScale}.", nameof(decimalRating));

        if (maxScale <= 0)
            throw new ArgumentException("Max scale must be positive.", nameof(maxScale));

        // Convert to 1-5 scale and round
        var normalizedRating = (decimalRating / maxScale) * (MaxValue - MinValue) + MinValue;
        var value = (int)Math.Round(normalizedRating);
        
        // Ensure we're within bounds
        value = Math.Max(MinValue, Math.Min(MaxValue, value));
        
        return new Rating(value);
    }

    /// <summary>
    /// Gets the star rating as a visual representation using Unicode stars.
    /// </summary>
    /// <param name="useEmoji">Whether to use emoji stars (★) or text representation.</param>
    /// <returns>String representation of the star rating.</returns>
    public string ToStarString(bool useEmoji = true)
    {
        if (useEmoji)
        {
            var filledStars = new string('★', Value);
            var emptyStars = new string('☆', MaxValue - Value);
            return filledStars + emptyStars;
        }

        return $"{Value}/{MaxValue} stars";
    }

    /// <summary>
    /// Calculates the average rating from a collection of ratings.
    /// </summary>
    /// <param name="ratings">The collection of ratings to average.</param>
    /// <returns>The average rating, or null if the collection is empty.</returns>
    public static Rating? CalculateAverage(IEnumerable<Rating> ratings)
    {
        var ratingsList = ratings.ToList();
        if (!ratingsList.Any())
            return null;

        var average = ratingsList.Average(r => r.Value);
        return new Rating((int)Math.Round(average));
    }

    /// <summary>
    /// Determines if this rating is better than another rating.
    /// </summary>
    /// <param name="other">The rating to compare against.</param>
    /// <returns>True if this rating is higher than the other rating.</returns>
    public bool IsBetterThan(Rating other) => Value > other.Value;

    /// <summary>
    /// Determines if this rating is worse than another rating.
    /// </summary>
    /// <param name="other">The rating to compare against.</param>
    /// <returns>True if this rating is lower than the other rating.</returns>
    public bool IsWorseThan(Rating other) => Value < other.Value;

    public override string ToString() => $"{Value} stars";

    public static implicit operator int(Rating rating) => rating.Value;

    public static explicit operator Rating(int value) => new(value);

    // Comparison operators
    public static bool operator >(Rating left, Rating right) => left.Value > right.Value;
    public static bool operator <(Rating left, Rating right) => left.Value < right.Value;
    public static bool operator >=(Rating left, Rating right) => left.Value >= right.Value;
    public static bool operator <=(Rating left, Rating right) => left.Value <= right.Value;
}
