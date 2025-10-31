using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a category of dishes (e.g., Italian, Salad) for a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Category is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of categories. This structure is a foundation for a multi-tenant architecture.
    ///
    /// NOTE: Indexes are defined centrally in `AppDbContext.OnModelCreating` to avoid duplication and to
    /// provide a single place to control index names and performance characteristics.
    /// </remarks>
    [Table("Categories")]
    public class Category : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the category. Must be unique within a restaurant.
        /// </summary>
        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        [MinLength(2, ErrorMessage = "Category name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the category
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Display order for sorting categories in UI
        /// </summary>
        [Range(0, int.MaxValue)]
        public int DisplayOrder { get; set; }


        /// === Navigation Properties ===

        /// <summary>
        /// Navigation property for all dishes in this category. Navigation properties are not serialized to avoid circular references.
        /// </summary>
        [InverseProperty(nameof(Dish.Category))]
        public ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    }
}
