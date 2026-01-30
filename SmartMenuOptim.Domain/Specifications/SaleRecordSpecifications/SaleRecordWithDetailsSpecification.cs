using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Domain.Specifications.SaleRecordSpecifications
{
    /// <summary>
    /// Specification for retrieving sale records with complete related data for reporting and analysis.
    /// Includes Dish with its Category, Reviews, and Restaurant for comprehensive data presentation.
    /// </summary>
    public class SaleRecordWithDetailsSpecification : BaseSpecification<SaleRecord>
    {
        /// <summary>
        /// Initializes a specification to get all sale records with complete related data.
        /// </summary>
        public SaleRecordWithDetailsSpecification()
        {
            AddInclude(s => s.Dish);
            // String-based includes for nested navigation properties
            AddInclude("Dish.Category");
            AddInclude("Dish.Reviews");
            AddInclude("Dish.Restaurant");
        }

        /// <summary>
        /// Initializes a specification to get a specific sale record by ID with all related data.
        /// </summary>
        /// <param name="saleRecordId">The unique identifier of the sale record.</param>
        public SaleRecordWithDetailsSpecification(int saleRecordId)
            : base(s => s.Id == saleRecordId)
        {
            AddInclude(s => s.Dish);
            AddInclude("Dish.Category");
            AddInclude("Dish.Reviews");
            AddInclude("Dish.Restaurant");
        }

        /// <summary>
        /// Initializes a specification to get sale records for a specific dish with all related data.
        /// </summary>
        /// <param name="dishId">The dish identifier to filter by.</param>
        /// <param name="includeDetails">Whether to include all nested details.</param>
        public SaleRecordWithDetailsSpecification(int dishId, bool includeDetails)
            : base(s => s.DishId == dishId)
        {
            AddInclude(s => s.Dish);
            if (includeDetails)
            {
                AddInclude("Dish.Category");
                AddInclude("Dish.Reviews");
                AddInclude("Dish.Restaurant");
            }
        }

        /// <summary>
        /// Initializes a specification to get sale records within a date range with all related data.
        /// </summary>
        /// <param name="startDate">The start date for filtering.</param>
        /// <param name="endDate">The end date for filtering.</param>
        public SaleRecordWithDetailsSpecification(DateTime startDate, DateTime endDate)
            : base(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
        {
            AddInclude(s => s.Dish);
            AddInclude("Dish.Category");
            AddInclude("Dish.Reviews");
            AddInclude("Dish.Restaurant");
            ApplyOrderByDescending(s => s.SaleDate);
        }
    }
}
