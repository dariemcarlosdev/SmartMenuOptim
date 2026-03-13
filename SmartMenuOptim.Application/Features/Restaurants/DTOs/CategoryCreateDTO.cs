using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for creating a new DishCategory.
/// </summary>
public class CategoryCreateDTO
{
    /// <summary>
    /// Name of the category (e.g., "Appetizers", "Main Course", "Desserts").
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the category.
    /// </summary>
    [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Display order for sorting categories in menus.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Display order must be between 0 and 1000")]
    public int DisplayOrder { get; set; } = 0;
}
