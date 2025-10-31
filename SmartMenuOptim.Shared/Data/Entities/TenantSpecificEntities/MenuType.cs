using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a menu type within a tenant, such as Breakfast, Lunch, Dinner, or other custom categories. Provides
    /// properties for naming, describing, scheduling, and organizing menu types, as well as navigation to associated menus.
    /// </summary>
    /// <remarks>
    /// MenuType is used to categorize menus in a restaurant or hospitality context, allowing for flexible
    /// grouping and scheduling. The class includes properties for default start and end times, which can be used to define
    /// when menus of this type are typically available. The DisplayOrder property enables custom sorting of menu types in
    /// user interfaces. The IsActive property controls whether the menu type can be assigned to new menus. The Menus
    /// navigation property provides access to all menus associated with this type.
    ///
    /// Indexing:
    /// - Do NOT add index attributes here. Indexes are centralized in `AppDbContext.OnModelCreating` to avoid
    ///   duplication and to allow consistent index naming and tuning across the application.
    /// </remarks>
    [Table("MenuTypes")]
    public class MenuType : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Name of the menu type (e.g., Breakfast, Lunch, Dinner, Seasonal).
        /// </summary>
        [Required(ErrorMessage = "MenuType name is required")]
        [MaxLength(100, ErrorMessage = "MenuType name cannot exceed 100 characters")]
        [MinLength(1, ErrorMessage = "MenuType name must contain at least 1 character")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the menu type.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Default start time for menus of this type (local restaurant time).
        /// </summary>
        public TimeSpan? DefaultStartTime { get; set; }

        /// <summary>
        /// Default end time for menus of this type (local restaurant time).
        /// </summary>
        public TimeSpan? DefaultEndTime { get; set; }

        /// <summary>
        /// Display order for sorting menu types.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "DisplayOrder must be a non-negative integer")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Indicates if this menu type is active and can be used for new menus.
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        // === Navigation Properties ===
        // In Entity Framework, navigation properties are used to define relationships between entities.
        // This property links to all menus of this type within the same tenant.

        /// <summary>
        /// Navigation property for all menus of this type.
        /// </summary>
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Name non-empty is enforced by [Required] and [MinLength], but check whitespace-only
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("MenuType name must not be empty or whitespace.", new[] { nameof(Name) });

            // If both default times are provided, ensure they are not equal (equal would be ambiguous)
            if (DefaultStartTime.HasValue && DefaultEndTime.HasValue)
            {
                if (DefaultStartTime.Value == DefaultEndTime.Value)
                {
                    yield return new ValidationResult("DefaultStartTime and DefaultEndTime cannot be equal.", new[] { nameof(DefaultStartTime), nameof(DefaultEndTime) });
                }
            }

            // DisplayOrder already validated by Range attribute
            yield break;
        }
    }
}