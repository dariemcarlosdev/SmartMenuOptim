namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for Menu aggregate.
/// </summary>
/// <remarks>
/// <para>Maps to <see cref="Domain.Aggregates.MenuAggregate.Menu"/> aggregate root.</para>
/// <para>All properties are mutable for Blazor form binding and CRUD operations.</para>
/// </remarks>
public class MenuDTO
{
    /// <summary>
    /// Menu identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Restaurant identifier (tenant).
    /// </summary>
    public int RestaurantId { get; set; }

    /// <summary>
    /// Name of the menu (e.g., "Breakfast", "Lunch", "Dinner").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the menu.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Menu type identifier (optional).
    /// </summary>
    public int? MenuTypeId { get; set; }

    /// <summary>
    /// Menu type name for display.
    /// </summary>
    public string? MenuTypeName { get; set; }

    /// <summary>
    /// Time when this menu becomes available.
    /// </summary>
    public TimeSpan? AvailableFrom { get; set; }

    /// <summary>
    /// Time when this menu stops being available.
    /// </summary>
    public TimeSpan? AvailableTo { get; set; }

    /// <summary>
    /// Whether the menu is currently active/available.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of dishes in this menu.
    /// </summary>
    public int DishCount { get; set; }

    /// <summary>
    /// Date and time when the menu was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the menu was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets the formatted availability time range.
    /// </summary>
    public string AvailabilityDisplay
    {
        get
        {
            if (!AvailableFrom.HasValue || !AvailableTo.HasValue)
                return "All Day";
            
            return $"{AvailableFrom.Value:hh\\:mm} - {AvailableTo.Value:hh\\:mm}";
        }
    }

    /// <summary>
    /// Gets the status display text.
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}
