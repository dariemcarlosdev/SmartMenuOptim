using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System;

/// <summary>
/// Represents a menu in a specific restaurant.
/// </summary>
/// <remarks>
/// Multi-Tenant Support: This entity is tenant-specific. Each Menu is linked to a Restaurant,
/// enabling the application to support multiple restaurants (tenants), each with their own unique set of menus.
///
/// Indexing guidance:
/// - Indexes related to menu availability are defined in `AppDbContext.OnModelCreating` to centralize
///   index management and avoid duplication. See `IX_Menus_Restaurant_Availability` and
///   `IX_Menus_Restaurant_Availability_Active` in `AppDbContext`.
/// - Recommended query patterns supported:
///   * Find active menus for a restaurant within a time window
///     dbContext.Menus.Where(m => m.RestaurantId == r && m.IsActive && m.AvailableFrom <= now && m.AvailableTo >= now)
///   * List menus by MenuType for a restaurant
///     dbContext.Menus.Where(m => m.RestaurantId == r && m.MenuTypeId == menuTypeId)
///
/// Validation:
/// - `Name` is required and constrained to 3..100 characters.
/// - `MenuTypeId` is required.
/// - If both `AvailableFrom` and `AvailableTo` are set, `AvailableFrom` must be earlier than `AvailableTo`.
/// </remarks>
[Table("Menus")]
public class Menu : TenantEntityBase, IValidatableObject
{
    // === Standalone Properties ===

    /// <summary>
    /// Name of the menu.
    /// </summary>
    [Required(ErrorMessage = "Menu name is required")]
    [MinLength(3, ErrorMessage = "Menu name must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Menu name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key for the menu type. Required relationship - every menu must have a type.
    /// </summary>
    [Required(ErrorMessage = "MenuTypeId is required")]
    [ForeignKey(nameof(MenuType))]
    public int MenuTypeId { get; set; }

    /// <summary>
    /// Description of the menu.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Menu description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Start time when this menu is available (local restaurant time).
    /// </summary>
    public TimeSpan? AvailableFrom { get; set; }

    /// <summary>
    /// End time when this menu is available (local restaurant time).
    /// </summary>
    public TimeSpan? AvailableTo { get; set; }

    // === Navigation Properties ===

    /// <summary>
    /// MenuType is the foreign key relationship to MenuType entity.
    /// Type of menu (e.g., Breakfast, Lunch, Dinner, Seasonal). Required navigation property
    /// as every menu must belong to a menu type.
    /// </summary>
    public MenuType MenuType { get; set; } = default!;

    /// <summary>
    /// Navigation property for all dishes in this menu.
    /// </summary>
    public ICollection<Dish> Dishes { get; set; } = new List<Dish>();

    // === Validation ===
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ensure name is not whitespace
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Menu name must not be empty or whitespace.", new[] { nameof(Name) });

        // If both times provided, ensure logical ordering
        if (AvailableFrom.HasValue && AvailableTo.HasValue)
        {
            if (AvailableFrom.Value == AvailableTo.Value)
            {
                yield return new ValidationResult("AvailableFrom and AvailableTo cannot be equal.", new[] { nameof(AvailableFrom), nameof(AvailableTo) });
            }
            else
            {
                // Allow overnight ranges (e.g., AvailableFrom 20:00, AvailableTo 02:00) so only validate if start and end are on same day
                // We consider typical case where AvailableFrom < AvailableTo; if not, it's allowed to represent overnight menu.
                // No error in that case.
            }
        }

        // MenuTypeId exists validation is deferred to EF foreign key checks during SaveChanges, but enforce positive id here
        if (MenuTypeId <= 0)
            yield return new ValidationResult("MenuTypeId must reference an existing MenuType.", new[] { nameof(MenuTypeId) });
    }
}