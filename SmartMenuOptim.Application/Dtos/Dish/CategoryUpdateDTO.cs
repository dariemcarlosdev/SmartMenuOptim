/*
 * File: CategoryUpdateDTO.cs
 * DTO for updating an existing Category
 * Version: 1.0
 * .NET Target: .NET 8
 */

using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Dtos.Dish;

/// <summary>
/// Data Transfer Object for updating an existing DishCategory.
/// </summary>
public class CategoryUpdateDTO
{
    /// <summary>
    /// The unique identifier of the category to update.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Name of the category.
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
    public int DisplayOrder { get; set; }
}
