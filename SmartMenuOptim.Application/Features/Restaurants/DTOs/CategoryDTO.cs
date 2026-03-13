namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// DTO for transferring category data between layers and for CRUD operations in Blazor user interfaces.
/// </summary>
public class CategoryDTO
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the category (e.g., "Appetizers", "Main Course", "Desserts").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the category.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting categories in the menu.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates whether the category is active and visible in menus.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Restaurant ID this category belongs to (tenant scope).
    /// </summary>
    public int RestaurantId { get; set; }
}
