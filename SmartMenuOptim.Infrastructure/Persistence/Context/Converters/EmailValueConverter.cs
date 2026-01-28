using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Provides a value converter for mapping <see cref="Email"/> objects to their string representations and vice versa
/// for use with Entity Framework Core.
/// </summary>
/// <remarks>This converter enables storing <see cref="Email"/> value objects as strings in the database and
/// reconstructing them when reading from the database. It is typically used when configuring model properties of type
/// <see cref="Email"/> in Entity Framework Core to ensure proper persistence and retrieval.</remarks>
public sealed class EmailValueConverter : ValueConverter<Email, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailValueConverter"/> class.
    /// </summary>
    public EmailValueConverter() 
        : base(
            // Convert Email to string when saving to database
            email => email != null ? email.Value : null,
            // Convert string to Email when reading from database
            value => value != null ? new Email(value) : null)
    {
    }
}
