using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents a menu in a specific restaurant.
/// </summary>
/// <remarks>
/// Multi-Tenant Support: This entity is tenant-specific. Each Menu is linked to a Restaurant,
/// enabling the application to support multiple restaurants (tenants), each with their own unique set of menus.
/// </remarks>
public class Menu : TenantEntityBase
{
    // === Standalone Properties ===

    /// <summary>
    /// Name of the menu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of menu (e.g., Breakfast, Lunch, Dinner, Seasonal).
    /// </summary>
    public MenuType Type { get; set; }

    /// <summary>
    /// Description of the menu.
    /// </summary>
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
    /// Navigation property for all dishes in this menu.
    /// </summary>
    public ICollection<Dish> Dishes { get; set; } = [];
}