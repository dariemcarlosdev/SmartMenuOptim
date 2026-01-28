using System.Text.RegularExpressions;

namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents an email address value object with validation.
/// </summary>
/// <remarks>
/// This value object ensures that email addresses are always valid and normalized.
/// It is immutable and defined by its value rather than identity.
/// </remarks>
public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Gets the normalized (lowercase) version of the email.
    /// </summary>
    public string NormalizedValue { get; init; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private Email()
    {
        Value = string.Empty;
        NormalizedValue = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="Value">The email address to validate and store.</param>
    /// <exception cref="ArgumentException">Thrown when the email is invalid.</exception>
    public Email(string Value)
    {
        if (string.IsNullOrWhiteSpace(Value))
            throw new ArgumentException("Email address cannot be empty.", nameof(Value));

        if (Value.Length > 254)
            throw new ArgumentException("Email address cannot exceed 254 characters.", nameof(Value));

        var trimmedValue = Value.Trim();
        if (!EmailRegex.IsMatch(trimmedValue))
            throw new ArgumentException($"'{trimmedValue}' is not a valid email address.", nameof(Value));

        this.Value = trimmedValue;
        NormalizedValue = trimmedValue.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}