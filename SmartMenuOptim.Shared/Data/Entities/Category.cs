using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities
{
    /// <summary>
    /// Represents a category of dishes (e.g., Italian, Salad) for a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Category is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of categories. This structure is a foundation for a multi-tenant architecture.
    /// </remarks>
    public class Category
    {
        // === Standalone Properties ===

        /// <summary>
        /// Primary key for the Category entity.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Restaurant entity. Each category belongs to a single restaurant.
        /// </summary>
        public int RestaurantId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property for the Restaurant that this category belongs to.
        /// </summary>
        public Restaurant? Restaurant { get; set; }

        /// <summary>
        /// Navigation property for all dishes in this category.
        /// </summary>
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
