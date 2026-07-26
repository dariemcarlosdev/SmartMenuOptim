using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace SmartMenuOptim.Infrastructure.Persistence.Context.Converters
{
    /// <summary>
    /// Generic value converter for simple data type conversions in Entity Framework Core.
    /// </summary>
    /// <typeparam name="TModel">The model type (type used in your entity)</typeparam>
    /// <typeparam name="TProvider">The provider type (type stored in database)</typeparam>
    /// <remarks>
    /// <para><strong>USAGE GUIDELINES:</strong></para>
    /// <para>Reserve <see cref="GenericValueConverter{TModel, TProvider}"/> for:</para>
    /// <list type="bullet">
    ///   <item><description><strong>Primitive type conversions</strong> - DateTime, decimal, double, int, etc.</description></item>
    ///   <item><description><strong>Simple data transformations</strong> - Format adjustments, normalization</description></item>
    ///   <item><description><strong>Calculations and rounding</strong> - Precision control, mathematical operations</description></item>
    /// </list>
    /// 
    /// <para><strong>DO NOT USE FOR:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Domain value objects</strong> - Create dedicated converters (e.g., EmailValueConverter, MoneyValueConverter)</description></item>
    ///   <item><description><strong>Complex entities</strong> - Use owned entities or table splitting instead</description></item>
    ///   <item><description><strong>Types requiring validation</strong> - Validation should be in dedicated value object converters</description></item>
    /// </list>
    /// 
    /// <para><strong>EXAMPLES:</strong></para>
    /// <code>
    /// // Good: Simple primitive conversion
    /// var utcConverter = GenericValueConverter&lt;DateTime, DateTime&gt;.UtcDateTime;
    /// 
    /// // Good: Rounding/precision control
    /// var sentimentConverter = GenericValueConverter&lt;double, double&gt;.SentimentScore;
    /// 
    /// // Bad: Use dedicated EmailValueConverter instead
    /// // var emailConverter = new GenericValueConverter&lt;Email, string&gt;(...);
    /// </code>
    /// 
    /// <para><strong>NAMING CONVENTION:</strong></para>
    /// <para>The name "GenericValueConverter" is appropriate and follows .NET naming conventions.
    /// It clearly indicates that this is a general-purpose, reusable converter for simple scenarios.
    /// No name change is necessary.</para>
    /// </remarks>
    public class GenericValueConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>
    {
        public GenericValueConverter(
            Expression<Func<TModel, TProvider>> convertTo,
            Expression<Func<TProvider, TModel>> convertFrom,
            ConverterMappingHints? mappingHints = null)
            : base(convertTo, convertFrom, mappingHints)
        {
        }

        /// <summary>
        /// Creates a DateTime converter that ensures UTC dates.
        /// </summary>
        public static GenericValueConverter<DateTime, DateTime> UtcDateTime =>
            new(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );

        /// <summary>
        /// Creates a converter for sentiment scores that maintains one decimal place precision.
        /// </summary>
        public static GenericValueConverter<double, double> SentimentScore =>
            new(
                v => Math.Round(v, 1),
                v => v
            );

        /// <summary>
        /// Creates a converter for decimal values with specified decimal places.
        /// </summary>
        public static GenericValueConverter<decimal, decimal> DecimalPrecision(int decimals) =>
            new(
                v => Math.Round(v, decimals),
                v => v
            );
    }
}