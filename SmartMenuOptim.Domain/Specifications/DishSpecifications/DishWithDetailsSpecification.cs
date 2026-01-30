using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using System;

namespace SmartMenuOptim.Domain.Specifications.DishSpecifications
{
    /// <summary>
    /// Specification for retrieving dishes with their related category and restaurant information.
    /// Example of Clean Architecture specification pattern usage.
    /// </summary>
    /// <remarks>
    /// This encapsulates the query logic for getting dishes with full details
    /// without exposing EF Core Include() patterns to the domain layer.
    /// </remarks>
    public class DishWithDetailsSpecification : BaseSpecification<Dish>
    {
        /// <summary>
        /// Initializes a specification to get a specific dish by ID with all related data.
        /// </summary>
        /// <param name="dishId">The unique identifier of the dish.</param>
        public DishWithDetailsSpecification(int dishId) 
            : base(d => d.Id == dishId)
        {
            AddInclude(d => d.Category);
            AddInclude(d => d.Restaurant);
        }

        /// <summary>
        /// Initializes a specification to get all dishes for a restaurant with related data.
        /// </summary>
        /// <param name="restaurantId">The restaurant identifier.</param>
        /// <param name="activeOnly">Whether to include only active dishes (IsActive = true).</param>
        public DishWithDetailsSpecification(int restaurantId, bool activeOnly = true)
            : base(d => d.RestaurantId == restaurantId && (!activeOnly || d.IsActive))
        {
            AddInclude(d => d.Category);
            AddInclude(d => d.Restaurant);
            ApplyOrderBy(d => d.Name);
        }
    }
}

