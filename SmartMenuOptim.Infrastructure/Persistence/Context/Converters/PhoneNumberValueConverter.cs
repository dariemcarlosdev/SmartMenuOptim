using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

/// <summary>
/// Value converter for PhoneNumber value objects to enable EF Core serialization.
/// Converts PhoneNumber to string for database storage and vice versa.
/// </summary>
/// <remarks>
/// This converter stores the original phone number value (with formatting) in the database
/// while maintaining the full PhoneNumber value object semantics in the domain model.
/// The PhoneNumber value object handles validation and normalization internally.
/// </remarks>
public sealed class PhoneNumberValueConverter : ValueConverter<PhoneNumber, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberValueConverter"/> class.
    /// </summary>
    public PhoneNumberValueConverter() 
        : base(
            // Convert PhoneNumber to string when saving to database
            phoneNumber => phoneNumber != null ? phoneNumber.Value : null,
            // Convert string to PhoneNumber when reading from database
            value => value != null ? new PhoneNumber(value) : null)
    {
    }
}
