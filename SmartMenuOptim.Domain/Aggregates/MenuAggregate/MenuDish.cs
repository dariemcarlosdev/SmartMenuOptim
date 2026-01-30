using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Aggregates.MenuAggregate
{
    /// <summary>
    /// Many-to-many join entity linking Menu and Dish with presentation metadata.
    /// Child entity of Menu aggregate - manages dish ordering and special pricing.
    /// </summary>
    /// <remarks>
    /// 🧩 JOIN ENTITY / CHILD - Menu Aggregate (Tier 1)
    /// 
    /// Key Characteristics:
    /// • Composite PK: MenuId + DishId
    /// • Stores relationship metadata: DisplayOrder, SpecialPrice, Notes, IsAvailable
    /// • Created only via Menu.AddDish()
    /// • Tenant-scoped: Menu and Dish must share RestaurantId
    /// • Mutable: Can update pricing, order, availability
    /// 
    /// Business Rules:
    /// • SpecialPrice must be positive (if provided)
    /// • Cannot exceed 5× base dish price
    /// • DisplayOrder must be non-negative
    /// • Menu and Dish must belong to same restaurant
    /// 
    /// <code>
    /// // ✅ CORRECT - Through parent aggregate
    /// menu.AddDish(dishId, displayOrder: 1, specialPrice: 14.99m, notes: "Chef's special");
    /// menu.UpdateDishPrice(dishId, newSpecialPrice: 12.99m);
    /// 
    /// // ❌ WRONG - Direct instantiation
    /// var menuDish = new MenuDish { MenuId = 1, DishId = 5 };
    /// </code>
    /// </remarks>
    public class MenuDish : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Foreign key for the Menu entity
        /// </summary>
        [Required(ErrorMessage = "Menu reference is required")]
        public int MenuId { get; set; }

        /// <summary>
        /// Foreign key for the Dish entity
        /// </summary>
        [Required(ErrorMessage = "Dish reference is required")]
        public int DishId { get; set; }

        /// <summary>
        /// Navigation property to the Menu
        /// </summary>
        public virtual Menu Menu { get; set; } = null!;

        /// <summary>
        /// Navigation property to the Dish
        /// </summary>
        public virtual Dish Dish { get; set; } = null!;

        /// <summary>
        /// Display order of the dish within the menu.
        /// Used for customizing the presentation order.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Display order must be a non-negative number")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Optional special price for this dish when it appears on this specific menu.
        /// If null, the dish's standard price is used.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 10000.00, ErrorMessage = "Special price must be between 0.01 and 10,000.00")]
        public decimal? SpecialPrice { get; set; }

        /// <summary>
        /// Any special notes or preparation instructions for this dish when served on this menu.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        /// <summary>
        /// Navigation property to the associated Restaurant (from TenantEntityBase)
        /// </summary>
        public virtual Restaurant Restaurant { get; set; } = null!;

        /// <summary>
        /// Validates the MenuDish entity ensuring data consistency and business rules.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>Collection of validation results.</returns>
        /// <remarks>
        /// Validation Rules:
        /// 1. Price Validation:
        ///    - Special price must be reasonable compared to base price
        ///    - Prevents excessive markups
        /// 2. Tenant Boundary:
        ///    - Menu and Dish must be from same restaurant
        ///    - Maintains data isolation
        /// 3. Relationship Validation:
        ///    - Required entities must exist
        ///    - References must be valid
        /// </remarks>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Special price validation
            if (SpecialPrice.HasValue && Dish != null)
            {
                if (SpecialPrice.Value <= 0)
                {
                    yield return new ValidationResult(
                        "Special price must be greater than zero",
                        new[] { nameof(SpecialPrice) }
                    );
                }

                const decimal maxMarkupMultiplier = 5.0m;
                if (SpecialPrice.Value > Dish.DishPrice * maxMarkupMultiplier)
                {
                    yield return new ValidationResult(
                        $"Special price cannot exceed {maxMarkupMultiplier}x the base price of the dish",
                        new[] { nameof(SpecialPrice) }
                    );
                }
            }

            // Tenant boundary validation
            if (Menu?.RestaurantId != null && Dish?.RestaurantId != null)
            {
                if (Menu.RestaurantId != Dish.RestaurantId)
                {
                    yield return new ValidationResult(
                        "Menu and Dish must belong to the same restaurant",
                        new[] { nameof(MenuId), nameof(DishId) }
                    );
                }

                // Ensure the MenuDish's RestaurantId matches both Menu and Dish
                if (RestaurantId != Menu.RestaurantId || RestaurantId != Dish.RestaurantId)
                {
                    yield return new ValidationResult(
                        "MenuDish must belong to the same restaurant as its Menu and Dish",
                        new[] { nameof(RestaurantId) }
                    );
                }
            }

            // Display order validation (if restaurant has a max limit)
            const int maxDisplayOrder = 1000; // Reasonable upper limit
            if (DisplayOrder > maxDisplayOrder)
            {
                yield return new ValidationResult(
                    $"Display order cannot exceed {maxDisplayOrder}",
                    new[] { nameof(DisplayOrder) }
                );
            }

            // Notes length validation (in addition to MaxLength attribute)
            if (!string.IsNullOrEmpty(Notes) && Notes.Trim().Length == 0)
            {
                yield return new ValidationResult(
                    "Notes cannot consist only of whitespace",
                    new[] { nameof(Notes) }
                );
            }
        }
    }
}









