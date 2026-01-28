using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;
using System.Text.Json;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Value converter for Money value objects to enable EF Core serialization.
/// Converts Money to JSON string for database storage and vice versa.
/// </summary>
/// <remarks>
/// This converter stores both the amount and currency code as a JSON object in the database.
/// The Money value object encapsulates monetary values with proper currency validation
/// and arithmetic operations that enforce currency matching.
/// Example JSON: {"Amount":99.99,"Currency":"USD"}
/// </remarks>
public sealed class MoneyValueConverter : ValueConverter<Money, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValueConverter"/> class.
    /// </summary>
    public MoneyValueConverter() 
        : base(
            // Convert Money to JSON string when saving to database
            money => money != null ? JsonSerializer.Serialize(money, JsonOptions) : null,
            // Convert JSON string to Money when reading from database
            json => json != null ? JsonSerializer.Deserialize<Money>(json, JsonOptions)! : null)
    {
    }
}
