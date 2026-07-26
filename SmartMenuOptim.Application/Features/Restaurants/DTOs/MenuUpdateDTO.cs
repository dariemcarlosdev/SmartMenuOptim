using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for updating an existing Menu.
/// </summary>
public class MenuUpdateDTO
{
    /// <summary>
    /// The unique identifier of the menu to update.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Name of the menu.
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
    /// Time when the menu becomes available.
    /// </summary>
    public TimeSpan? AvailableFrom { get; set; }

    /// <summary>
    /// Time when the menu stops being available.
    /// </summary>
    public TimeSpan? AvailableTo { get; set; }

    /// <summary>
    /// Whether the menu is active/available.
    /// </summary>
    public bool IsActive { get; set; }
}
