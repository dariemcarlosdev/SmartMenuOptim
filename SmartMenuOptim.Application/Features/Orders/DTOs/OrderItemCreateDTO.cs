using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for creating a new order item (nested in <see cref="OrderCreateDTO"/>).
/// </summary>
public class OrderItemCreateDTO
{
    /// <summary>
    /// The dish to order.
    /// </summary>
    [Required(ErrorMessage = "Dish ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Dish ID must be a positive number")]
    public int DishId { get; set; }

    /// <summary>
    /// The quantity to order.
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Special instructions for this item.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Special instructions cannot exceed 500 characters")]
    public string? SpecialInstructions { get; set; }
}
