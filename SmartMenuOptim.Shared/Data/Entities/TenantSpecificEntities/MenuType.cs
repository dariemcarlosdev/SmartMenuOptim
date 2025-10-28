/// <summary>
/// Represents a menu type within a tenant, such as Breakfast, Lunch, Dinner, or other custom categories. Provides
/// properties for naming, describing, scheduling, and organizing menu types, as well as navigation to associated menus.
/// </summary>
/// <remarks>MenuType is used to categorize menus in a restaurant or hospitality context, allowing for flexible
/// grouping and scheduling. The class includes properties for default start and end times, which can be used to define
/// when menus of this type are typically available. The DisplayOrder property enables custom sorting of menu types in
/// user interfaces. The IsActive property controls whether the menu type can be assigned to new menus. The Menus
/// navigation property provides access to all menus associated with this type.</remarks>
public class MenuType : TenantEntityBase
{
    /// <summary>
    /// Name of the menu type (e.g., Breakfast, Lunch, Dinner, Seasonal).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the menu type.
    /// </summary>
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
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates if this menu type is active and can be used for new menus.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property for all menus of this type.
    /// </summary>
    public ICollection<Menu> Menus { get; set; } = [];
}