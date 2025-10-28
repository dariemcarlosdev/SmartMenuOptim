using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a category of dishes (e.g., Italian, Salad) for a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Category is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of categories. This structure is a foundation for a multi-tenant architecture.
    /// </remarks>
    public class Category : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property for all dishes in this category.
        /// </summary>
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
