using System.Text.RegularExpressions;

namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a dish name value object with validation and normalization.
/// </summary>
/// <remarks>
/// This value object ensures that dish names are always valid, normalized, and conform to business rules.
/// It is immutable and defined by its value rather than identity.
/// Provides additional functionality for search optimization and menu display.
/// </remarks>
public sealed record DishName
{
    private static readonly Regex ValidNameRegex = new(
        @"^[a-zA-Z0-9\s\-\.\&\'\(\)]+$",
        RegexOptions.Compiled);

    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled);

    /// <summary>
    /// Gets the display name value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Gets the normalized version of the dish name for searching and comparison.
    /// </summary>
    /// <remarks>
    /// Normalized format:
    /// - Lowercase
    /// - Trimmed whitespace
    /// - Multiple spaces collapsed to single spaces
    /// - Special characters normalized
    /// </remarks>
    public string NormalizedValue { get; init; }

    /// <summary>
    /// Gets the search-friendly version of the dish name for autocomplete and filtering.
    /// </summary>
    /// <remarks>
    /// Search format:
    /// - Normalized value
    /// - Punctuation removed
    /// - Suitable for fuzzy matching
    /// </remarks>
    public string SearchValue { get; init; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private DishName()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
        SearchValue = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DishName"/> class.
    /// </summary>
    /// <param name="Value">The dish name to validate and store.</param>
    /// <exception cref="ArgumentException">Thrown when the dish name is invalid.</exception>
    public DishName(string Value)
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Dish name cannot be empty.", nameof(Value));

        if (Value.Length < 3)
            throw new ArgumentException("Dish name must be at least 3 characters long.", nameof(Value));

        if (Value.Length > 100)
            throw new ArgumentException("Dish name cannot exceed 100 characters.", nameof(Value));

        var trimmedValue = Value.Trim();
        
        if (!ValidNameRegex.IsMatch(trimmedValue))
            throw new ArgumentException(
                "Dish name contains invalid characters. Only letters, numbers, spaces, hyphens, periods, ampersands, apostrophes, and parentheses are allowed.", 
                nameof(Value));

        // Normalize whitespace (collapse multiple spaces)
        var normalizedValue = WhitespaceRegex.Replace(trimmedValue, " ");

        this.Value = normalizedValue;
        NormalizedValue = normalizedValue.ToLowerInvariant();
        SearchValue = Regex.Replace(NormalizedValue, @"[^\w\s]", "").Trim();
    }

    /// <summary>
    /// Determines whether the dish name is suitable for special menu sections (e.g., chef's special).
    /// </summary>
    /// <returns>True if the name contains words indicating premium or special status.</returns>
    public bool IsSpecialtyDish()
    {
        var specialWords = new[] { "special", "chef", "signature", "premium", "deluxe", "supreme", "gourmet" };
        return specialWords.Any(word => NormalizedValue.Contains(word));
    }

    /// <summary>
    /// Gets the abbreviated version of the dish name for compact display.
    /// </summary>
    /// <param name="maxLength">Maximum length of the abbreviated name.</param>
    /// <returns>Abbreviated dish name if it exceeds the maximum length.</returns>
    public string GetAbbreviated(int maxLength = 20)
    {
        if (Value.Length <= maxLength)
            return Value;

        var words = Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return Value.Length > maxLength ? Value[..(maxLength - 3)] + "..." : Value;

        var abbreviated = string.Empty;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(abbreviated) ? word : $"{abbreviated} {word}";
            if (candidate.Length > maxLength - 3)
                break;
            abbreviated = candidate;
        }

        return abbreviated.Length < Value.Length ? abbreviated + "..." : abbreviated;
    }

    public override string ToString() => Value;

    public static implicit operator string(DishName dishName) => dishName.Value;

    public static explicit operator DishName(string value) => new(value);
}
