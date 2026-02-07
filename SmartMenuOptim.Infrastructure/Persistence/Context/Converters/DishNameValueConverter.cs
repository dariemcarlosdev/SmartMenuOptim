using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Provides a value converter for mapping <see cref="DishName"/> objects to their string representations and vice versa
/// for use with Entity Framework Core.
/// </summary>
/// <remarks>This converter enables storing <see cref="DishName"/> value objects as strings in the database and
/// reconstructing them when reading from the database. It is typically used when configuring model properties of type
/// <see cref="DishName"/> in Entity Framework Core to ensure proper persistence and retrieval.</remarks>
public sealed class DishNameValueConverter : ValueConverter<DishName, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DishNameValueConverter"/> class.
    /// </summary>
    public DishNameValueConverter() 
        : base(
            // Convert DishName to string when saving to database
            dishName => dishName != null ? dishName.Value : null,
            // Convert string to DishName when reading from database
            value => value != null ? new DishName(value) : null)
    {
    }
}