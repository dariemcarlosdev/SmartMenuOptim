using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Provides a value converter for mapping <see cref="Rating"/> objects to their integer representations and vice versa
/// for use with Entity Framework Core.
/// </summary>
/// <remarks>This converter enables storing <see cref="Rating"/> value objects as integers in the database and
/// reconstructing them when reading from the database. It is typically used when configuring model properties of type
/// <see cref="Rating"/> in Entity Framework Core to ensure proper persistence and retrieval.</remarks>
public sealed class RatingValueConverter : ValueConverter<Rating, int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RatingValueConverter"/> class.
    /// </summary>
    public RatingValueConverter() 
        : base(
            // Convert Rating to int when saving to database
            rating => rating != null ? rating.Value : 0,
            // Convert int to Rating when reading from database
            value => value > 0 ? new Rating(value) : null)
    {
    }
}