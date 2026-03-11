namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Value Object representing the outcome of a menu composition validation operation.
/// </summary>
/// <remarks>
/// <para>
/// This value object is returned by the <c>MenuCompositionValidatorService</c>, which validates that a menu meets 
/// certain business rules and standards, such as having a balanced variety of dishes, appropriate price points, 
/// and no duplicate items.
/// </para>
/// 
/// <para><strong>Note:</strong> This is a domain Value Object, not to be confused with generic result patterns
/// like <see cref="Common.DomainResult{T}"/>. It represents a specific domain concept (menu validation outcome)
/// with domain-specific properties (Errors, Warnings, Summary).</para>
/// 
/// <para><strong>Business Rules Validated:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Menu has minimum dish variety:</strong> At least 3 active dishes required</description></item>
///   <item><description><strong>Categories are balanced:</strong> No single category exceeds 70% of menu items</description></item>
///   <item><description><strong>Price ranges are appropriate:</strong> At least 2 distinct price levels (10% tolerance)</description></item>
///   <item><description><strong>No duplicate dishes:</strong> Each dish appears only once on the menu</description></item>
///   <item><description><strong>Seasonal items are current:</strong> Seasonal dishes match the current season</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var validator = new MenuCompositionValidatorService();
/// MenuValidationOutcome outcome = validator.ValidateMenuComposition(menu);
/// 
/// if (!outcome.IsValid)
/// {
///     foreach (var error in outcome.Errors)
///         Console.WriteLine($"❌ {error}");
/// }
/// </code>
/// </remarks>
public sealed record MenuValidationOutcome
{
    /// <summary>
    /// Gets whether the menu composition is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the collection of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Gets the collection of validation warning messages.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Gets a summary message describing the validation outcome.
    /// </summary>
    public string Summary { get; init; }

    /// <summary>
    /// Creates a successful validation outcome with optional warnings.
    /// </summary>
    public static MenuValidationOutcome Success(IEnumerable<string>? warnings = null)
    {
        var warningList = warnings?.ToList() ?? new List<string>();
        return new MenuValidationOutcome
        {
            IsValid = true,
            Errors = Array.Empty<string>(),
            Warnings = warningList,
            Summary = warningList.Any()
                ? $"Validation passed with {warningList.Count} warning(s)"
                : "Menu composition is valid"
        };
    }

    /// <summary>
    /// Creates a failed validation outcome with error messages.
    /// </summary>
    public static MenuValidationOutcome Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
    {
        if (errors == null || !errors.Any())
            throw new ArgumentException("Failure outcome must have at least one error", nameof(errors));

        var errorList = errors.ToList();
        var warningList = warnings?.ToList() ?? new List<string>();

        return new MenuValidationOutcome
        {
            IsValid = false,
            Errors = errorList,
            Warnings = warningList,
            Summary = $"Validation failed with {errorList.Count} error(s)" +
                     (warningList.Any() ? $" and {warningList.Count} warning(s)" : "")
        };
    }

    /// <summary>
    /// Private constructor for record initialization.
    /// </summary>
    private MenuValidationOutcome()
    {
        Errors = Array.Empty<string>();
        Warnings = Array.Empty<string>();
        Summary = string.Empty;
    }
}
