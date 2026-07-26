using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;
using System.Text.Json;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Provides a value converter that serializes and deserializes Address objects to and from their JSON string
/// representation for use with Entity Framework Core.
/// </summary>
/// <remarks>This converter enables storing Address objects as JSON strings in the database and automatically
/// reconstructs Address instances when reading from the database. The conversion uses case-insensitive property names
/// and does not format the JSON output. This class is typically used when configuring model properties in Entity
/// Framework Core to persist complex types as JSON.</remarks>
public sealed class AddressValueConverter : ValueConverter<Address, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public AddressValueConverter() 
        : base(
            // Convert Address to JSON when saving to database
            address => address != null ? JsonSerializer.Serialize(address, JsonOptions) : null,
            // Convert JSON to Address when reading from database
            json => json != null ? JsonSerializer.Deserialize<Address>(json, JsonOptions)! : null)
    {
    }
}