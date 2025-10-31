using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a dish offered by a restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each Dish is linked to a Restaurant, enabling the application to support multiple restaurants (tenants), each with their own unique set of dishes. This structure is a foundation for a multi-tenant architecture.
    ///
    /// NOTE: Indexes are defined centrally in `AppDbContext.OnModelCreating` to avoid duplication and to
    /// provide a single place to control index names and performance characteristics.
    /// </remarks>
    [Table("Dishes")]
    public class Dish : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Name of the dish.
        /// </summary>
        [Required(ErrorMessage = "Dish name is required")]
        [MaxLength(100, ErrorMessage = "Dish name cannot exceed 100 characters")]
        [MinLength(3, ErrorMessage = "Dish name must be at least 3 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Price of the dish.
        /// </summary>
        [Required(ErrorMessage = "Dish price is required")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be between 0.01 and 10,000.00")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DishPrice { get; set; }

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Category entity. Each dish belongs to a single category.
        /// </summary>
        [Required(ErrorMessage = "Category is required")]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Category this dish belongs to.
        /// </summary>
        public Category Category { get; set; } = default!;

        /// <summary>
        /// Navigation property for all reviews associated with this dish.
        /// </summary>
        [InverseProperty(nameof(Review.Dish))]
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Navigation property for all sales records associated with this dish.
        /// </summary>
        [InverseProperty(nameof(SaleRecord.Dish))]
        public ICollection<SaleRecord> SaleRecords { get; set; } = new List<SaleRecord>();

        /// <summary>
        /// Navigation property for all order items associated with this dish.
        /// </summary>
        [InverseProperty(nameof(OrderItem.Dish))]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Navigation property for menus that include this dish.
        /// Many-to-many relationship: a dish can appear on multiple menus and a menu can contain multiple dishes.
        /// This navigation is configured in AppDbContext to create the join table `MenuDishes`.
        /// </summary>
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
    }
}
