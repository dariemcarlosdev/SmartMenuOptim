using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters
{
    /// <summary>
    /// Provides a value converter that ensures all <see cref="DateTime"/> values are stored and retrieved as UTC in the
    /// database.
    /// </summary>
    /// <remarks>Use this converter to guarantee that <see cref="DateTime"/> properties are consistently
    /// handled as UTC when persisting to and reading from the database. This helps prevent issues related to time zone
    /// ambiguity and ensures reliable date and time comparisons across different environments.</remarks>
    public class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeValueConverter()
            : base(
                // Convert to UTC before saving
                v => v.ToUniversalTime(),
                // Ensure the kind is UTC when reading from the database
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}