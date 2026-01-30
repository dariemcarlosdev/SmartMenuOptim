using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SmartMenuOptim.Domain.Specifications
{
    /// <summary>
    /// Base implementation of the Specification pattern providing a fluent API for building complex queries.
    /// This class encapsulates query logic in a domain-centric, testable, and reusable way.
    /// </summary>
    /// <typeparam name="T">The entity type this specification applies to.</typeparam>
    /// <remarks>
    /// Use this base class to create concrete specifications in the Domain layer.
    /// Example: public class ActiveProductsSpec : BaseSpecification&lt;Product&gt; { ... }
    /// </remarks>
    public abstract class BaseSpecification<T> : ISpecification<T> where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class.
        /// </summary>
        protected BaseSpecification()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class with filtering criteria.
        /// </summary>
        /// <param name="criteria">The filtering expression to apply.</param>
        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        /// <inheritdoc />
        public Expression<Func<T, bool>>? Criteria { get; private set; }

        /// <inheritdoc />
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();

        /// <inheritdoc />
        public List<string> IncludeStrings { get; } = new List<string>();

        /// <inheritdoc />
        public Expression<Func<T, object>>? OrderBy { get; private set; }

        /// <inheritdoc />
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }

        /// <inheritdoc />
        public int? Take { get; private set; }

        /// <inheritdoc />
        public int? Skip { get; private set; }

        /// <inheritdoc />
        public bool IsPagingEnabled { get; private set; } = false;

        /// <summary>
        /// Adds a navigation property to be included in the query.
        /// </summary>
        /// <param name="includeExpression">The navigation property expression.</param>
        /// <remarks>
        /// Use for eager loading related entities: AddInclude(x => x.Category)
        /// </remarks>
        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        /// <summary>
        /// Adds a string-based navigation property path to be included.
        /// </summary>
        /// <param name="includeString">The navigation property path (e.g., "Order.Customer").</param>
        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        /// <summary>
        /// Specifies an ascending order for the query results.
        /// </summary>
        /// <param name="orderByExpression">The ordering expression.</param>
        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        /// <summary>
        /// Specifies a descending order for the query results.
        /// </summary>
        /// <param name="orderByDescendingExpression">The ordering expression.</param>
        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }

        /// <summary>
        /// Applies pagination to the query.
        /// </summary>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="take">Number of records to take.</param>
        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}
