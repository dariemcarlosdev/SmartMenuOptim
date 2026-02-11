namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of a menu validation operation, including the overall validity, summary message, and any
/// errors or warnings encountered during validation.
/// </summary>
/// <remarks>
/// <para>
/// This value object is returned by the <c>MenuCompositionValidatorService</c>, which validates that a menu meets 
/// certain business rules and standards, such as having a balanced variety of dishes, appropriate price points, 
/// and no duplicate items. It ensures that the menu is well-structured and appealing to customers while adhering 
/// to the restaurant's strategic goals.
/// </para>
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
/// <para>
/// Use this type to inspect the outcome of a menu validation process. The result indicates whether the
/// menu composition is valid and provides collections of error and warning messages to help diagnose issues. 
/// Static methods are available to create success or failure results with appropriate messages. 
/// This type is immutable and intended to be used as a value object in validation workflows.
/// </para>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var validator = new MenuCompositionValidatorService();
/// MenuValidationResult result = validator.ValidateMenuComposition(menu);
/// 
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///         Console.WriteLine($"❌ {error}");
/// }
/// 
/// foreach (var warning in result.Warnings)
///     Console.WriteLine($"⚠️ {warning}");
/// </code>
/// 
/// <para><strong>Clean Architecture - Domain Layer Placement:</strong></para>
/// <para>
/// This Value Object is located in the Domain Layer because it:
/// (1) represents a pure domain concept (menu validation outcome),
/// (2) is immutable with value-based equality (C# record),
/// (3) has no infrastructure dependencies,
/// (4) uses ubiquitous language from the restaurant domain,
/// (5) is returned by a Domain Service (MenuCompositionValidatorService).
/// Value Objects encapsulate domain concepts defined by their attributes, not identity.
/// </para>
/// </remarks>
public sealed record MenuValidationResult
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
    /// Gets a summary message describing the validation result.
    /// </summary>
    public string Summary { get; init; }

    /// <summary>
    /// Creates a successful validation result with optional warnings.
    /// </summary>
    public static MenuValidationResult Success(IEnumerable<string>? warnings = null)
    {
        var warningList = warnings?.ToList() ?? new List<string>();
        return new MenuValidationResult
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
    /// Creates a failed validation result with error messages.
    /// </summary>
    public static MenuValidationResult Failure(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
    {
        if (errors == null || !errors.Any())
            throw new ArgumentException("Failure result must have at least one error", nameof(errors));

        var errorList = errors.ToList();
        var warningList = warnings?.ToList() ?? new List<string>();

        return new MenuValidationResult
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
    private MenuValidationResult()
    {
        Errors = Array.Empty<string>();
        Warnings = Array.Empty<string>();
        Summary = string.Empty;
    }
}
