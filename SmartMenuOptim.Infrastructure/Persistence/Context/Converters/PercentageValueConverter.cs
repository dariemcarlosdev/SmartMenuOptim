using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Value converter for Percentage value objects to enable EF Core serialization.
/// Converts Percentage to decimal for database storage and vice versa.
/// </summary>
/// <remarks>
/// This converter stores the percentage as a decimal value (0.0 to 1.0) in the database.
/// The Percentage value object handles validation, formatting, and utility methods
/// for percentage calculations (discounts, markups, etc.).
/// Database value: 0.15 represents 15%
/// </remarks>
public sealed class PercentageValueConverter : ValueConverter<Percentage, decimal>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PercentageValueConverter"/> class.
    /// </summary>
    public PercentageValueConverter() 
        : base(
            // Convert Percentage to decimal when saving to database (0.0 to 1.0)
            percentage => percentage != null ? percentage.Value : 0m,
            // Convert decimal to Percentage when reading from database
            value => new Percentage(value, false)) // false = treat as decimal (0.0-1.0)
    {
    }
}
