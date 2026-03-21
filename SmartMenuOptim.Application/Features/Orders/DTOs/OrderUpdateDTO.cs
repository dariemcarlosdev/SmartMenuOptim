using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for updating an existing Order.
/// </summary>
/// <remarks>
/// Supports updating special instructions and status.
/// Item modifications go through dedicated aggregate methods.
/// </remarks>
public class OrderUpdateDTO
{
    /// <summary>
    /// Order identifier (required for update).
    /// </summary>
    [Required(ErrorMessage = "Order ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Order ID must be a positive number")]
    public int Id { get; set; }

    /// <summary>
    /// Updated special instructions for the order.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "Special instructions cannot exceed 1000 characters")]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Updated order status identifier.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Order status ID must be a positive number")]
    public int? OrderStatusId { get; set; }
}
