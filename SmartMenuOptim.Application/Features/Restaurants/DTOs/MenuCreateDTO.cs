using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for creating a new Menu.
/// </summary>
public class MenuCreateDTO
{
    /// <summary>
    /// Name of the menu (e.g., "Breakfast", "Lunch", "Dinner").
    /// </summary>
    [Required(ErrorMessage = "Menu name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Menu name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the menu.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Optional menu type identifier.
    /// </summary>
    public int? MenuTypeId { get; set; }

    /// <summary>
    /// Time when the menu becomes available (e.g., 08:00 for breakfast).
    /// </summary>
    public TimeSpan? AvailableFrom { get; set; }

    /// <summary>
    /// Time when the menu stops being available (e.g., 11:00 for breakfast).
    /// </summary>
    public TimeSpan? AvailableTo { get; set; }

    /// <summary>
    /// Whether the menu is immediately active upon creation.
    /// </summary>
    public bool IsActive { get; set; } = false;
}
