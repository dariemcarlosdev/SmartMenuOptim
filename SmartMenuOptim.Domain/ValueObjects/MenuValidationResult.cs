namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents the result of menu composition validation.
/// </summary>
/// <remarks>
/// Value object that encapsulates validation outcomes including success/failure status,
/// validation messages, and specific rule violations. Immutable by design.
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
