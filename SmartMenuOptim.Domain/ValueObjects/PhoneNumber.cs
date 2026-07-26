using System.Text.RegularExpressions;

namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a phone number value object with validation and formatting.
/// </summary>
/// <remarks>
/// Supports international phone numbers and provides normalized formatting.
/// </remarks>
public sealed record PhoneNumber
{
    private static readonly Regex PhoneRegex = new(
        @"^\+?[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Gets the original phone number value.
    /// The init accessor indicates that the property can only be set during object initialization.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Gets the normalized phone number (digits only with country code).
    /// </summary>
    public string NormalizedValue { get; init; }

    /// <summary>
    /// Gets the country code if present.
    /// </summary>
    public string? CountryCode { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumber"/> class.
    /// </summary>
    /// <param name="value">The phone number to validate and store.</param>
    /// <exception cref="ArgumentException">Thrown when the phone number is invalid.</exception>
    /// <remarks>
    /// VALIDATION: Uses E.164 international phone number format validation.
    /// Accepts numbers like: +12025551234, +442071234567, etc.
    /// 
    /// FORMATTING: Removes common separators (spaces, dashes, parentheses, dots) before validation.
    /// The original formatting is preserved in the Value property.
    /// </remarks>
    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty.", nameof(value));

        // Remove common formatting characters
        var normalized = Regex.Replace(value, @"[\s\-\(\)\.]", "");

        if (!PhoneRegex.IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid phone number.", nameof(value));

        Value = value.Trim();
        NormalizedValue = normalized;

        // Extract country code if present
        if (normalized.StartsWith("+"))
        {
            var match = Regex.Match(normalized, @"^\+(\d{1,3})");
            CountryCode = match.Success ? match.Groups[1].Value : null;
        }
    }

    /// <summary>
    /// Parameterless constructor for EF Core and JSON deserialization.
    /// </summary>
    /// <remarks>
    /// EF CORE & JSON DESERIALIZATION PATTERN:
    /// 
    /// • The private parameterless constructor provides a safe entry point for EF Core and JSON deserializers
    ///   to create instances without going through the validation logic in the public constructor.
    /// 
    /// • The [JsonConstructor] attribute explicitly tells System.Text.Json to use this constructor when
    ///   deserializing JSON. Without this attribute, the deserializer might try to use the public constructor,
    ///   which would fail because it requires a 'value' parameter that doesn't exist as a JSON property.
    /// 
    /// • Deserialization Flow:
    ///   1. Deserializer invokes this parameterless constructor to create an instance
    ///   2. The instance is created with default values (empty strings, null)
    ///   3. After construction, the deserializer sets properties via the 'init' accessors
    ///   4. The final object has the correct values from the JSON/database without validation
    /// 
    /// • Why Skip Validation on Deserialization?
    ///   - Data coming from the database has already been validated when it was originally saved
    ///   - Re-validating on every load would be expensive and unnecessary
    ///   - The public constructor ensures validation happens on initial creation
    /// 
    /// DEFAULT VALUES: Provides safe defaults to avoid null reference issues during the brief moment
    /// between construction and property initialization. These values are immediately overwritten by
    /// the deserializer and should never be used in actual business logic.
    /// </remarks>
    [System.Text.Json.Serialization.JsonConstructor]
    private PhoneNumber()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
        CountryCode = null;
    }

    /// <summary>
    /// Formats the phone number in international format.
    /// </summary>
    public string ToInternationalFormat() => 
        NormalizedValue.StartsWith("+") ? NormalizedValue : $"+{NormalizedValue}";

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}