using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a dish offered by a restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Dish is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of dishes. This structure is a foundation for a multi-tenant architecture.
    /// </remarks>
    public class Dish
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the Dish entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the dish.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Price of the dish.
        /// </summary>
        public decimal DishPrice { get; set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Category entity. Each dish belongs to a single category.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Foreign key to the Restaurant entity. Each dish belongs to a single restaurant.
        /// </summary>
        public int RestaurantId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Category this dish belongs to.
        /// </summary>
        public Category? Category { get; set; }

        /// <summary>
        /// Navigation property to the Restaurant this dish is associated with.
        /// </summary>
        public Restaurant? Restaurant { get; set; }

        /// <summary>
        /// Navigation property for all reviews associated with this dish.
        /// </summary>
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Navigation property for all sales records associated with this dish.
        /// </summary>
        public ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();
    }
}
