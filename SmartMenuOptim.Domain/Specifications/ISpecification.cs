using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SmartMenuOptim.Domain.Specifications
{
    /// <summary>
    /// Defines a specification pattern interface for encapsulating query logic in a domain-centric way.
    /// This allows complex query logic to be defined in the Domain layer without coupling to infrastructure concerns.
    /// </summary>
    /// <typeparam name="T">The entity type this specification applies to.</typeparam>
    public interface ISpecification<T> where T : class
    {
        /// <summary>
        /// Gets the criteria expression that defines the filtering logic.
        /// </summary>
        /// <remarks>
        /// This expression will be used to filter entities in LINQ queries.
        /// Example: x => x.IsActive && x.CreatedDate > DateTime.UtcNow.AddDays(-30)
        /// </remarks>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Gets the collection of navigation properties to include in the query.
        /// </summary>
        /// <remarks>
        /// Each expression defines a navigation property path to eagerly load.
        /// This is a domain-level abstraction that the infrastructure layer will translate appropriately.
        /// </remarks>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Gets the collection of string-based navigation property paths to include.
        /// </summary>
        /// <remarks>
        /// Useful for nested includes like "Order.Customer.Address".
        /// </remarks>
        List<string> IncludeStrings { get; }

        /// <summary>
        /// Gets the ordering expression for ascending sort.
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Gets the ordering expression for descending sort.
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Gets the number of records to take (for pagination).
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Gets the number of records to skip (for pagination).
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Gets a value indicating whether query tracking is enabled.
        /// </summary>
        /// <remarks>
        /// When false, the infrastructure layer should use AsNoTracking() for read-only queries.
        /// </remarks>
        bool IsPagingEnabled { get; }
    }
}
