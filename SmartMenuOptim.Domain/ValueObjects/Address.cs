namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>
/// Represents a physical address value object.
/// </summary>
/// <remarks>
/// Used for restaurant locations, delivery addresses, etc.
/// </remarks>
public sealed record Address
{
    /// <summary>
    /// Gets the street address line 1.
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the street address line 2 (optional).
    /// </summary>
    public string? Street2 { get; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the state or province.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the postal or ZIP code.
    /// </summary>
    public string PostalCode { get; }

    /// <summary>
    /// Gets the ISO 3166-1 alpha-2 country code (e.g., "US", "CA", "GB").
    /// </summary>
    public string CountryCode { get; }

    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// For Entity Framework Core usage only. tHis constructor is required to create instances via reflection.
    /// Reflexion-based instantiation bypasses validation, so ensure that instances created this way are properly validated before use.
    /// Reflexion is a powerful feature of .NET that allows for dynamic type creation and manipulation at runtime. This allows frameworks like EF Core to create instances of classes without invoking their constructors directly.
    /// </summary>
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        CountryCode = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> class.
    /// </summary>
    public Address(
        string street,
        string city,
        string state,
        string postalCode,
        string countryCode,
        string? street2 = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street address is required.", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));

        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Country code is required.", nameof(countryCode));

        if (countryCode.Length != 2)
            throw new ArgumentException("Country code must be a 2-letter ISO 3166-1 alpha-2 code.", nameof(countryCode));

        Street = street.Trim();
        Street2 = string.IsNullOrWhiteSpace(street2) ? null : street2.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        CountryCode = countryCode.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the full address as a formatted string.
    /// </summary>
    public string ToFormattedString()
    {
        var parts = new List<string> { Street };

        if (!string.IsNullOrWhiteSpace(Street2))
            parts.Add(Street2);

        parts.Add($"{City}, {State} {PostalCode}");
        parts.Add(CountryCode);

        return string.Join(Environment.NewLine, parts);
    }

    /// <summary>
    /// Gets the address as a single-line string.
    /// </summary>
    public string ToSingleLine()
    {
        var street = string.IsNullOrWhiteSpace(Street2) ? Street : $"{Street}, {Street2}";
        return $"{street}, {City}, {State} {PostalCode}, {CountryCode}";
    }

    public override string ToString() => ToSingleLine();
}