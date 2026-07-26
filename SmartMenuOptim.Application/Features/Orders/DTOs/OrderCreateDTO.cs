using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for creating a new Order.
/// </summary>
/// <remarks>
/// Contains all required data to create a new order through the aggregate root.
/// Validation attributes provide client-side validation in Blazor forms.
/// </remarks>
public class OrderCreateDTO
{
    /// <summary>
    /// Restaurant (tenant) identifier where the order is placed.
    /// </summary>
    [Required(ErrorMessage = "Restaurant ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Restaurant ID must be a positive number")]
    public int RestaurantId { get; set; }

    /// <summary>
    /// Customer identifier who is placing the order.
    /// </summary>
    [Required(ErrorMessage = "Customer ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be a positive number")]
    public int CustomerId { get; set; }

    /// <summary>
    /// Special instructions for the entire order.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Special instructions cannot exceed 1000 characters")]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// The items to include in the order. At least one item is required.
    /// </summary>
    [Required(ErrorMessage = "At least one item is required")]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<OrderItemCreateDTO> Items { get; set; } = [];
}
