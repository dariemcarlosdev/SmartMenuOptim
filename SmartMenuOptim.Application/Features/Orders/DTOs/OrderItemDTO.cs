namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for OrderItem (nested in detail views).
/// </summary>
/// <remarks>
/// Represents a single line item in an order with dish info, quantity, and pricing.
/// </remarks>
public class OrderItemDTO
{
    /// <summary>
    /// OrderItem identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The dish identifier.
    /// </summary>
    public int DishId { get; set; }

    /// <summary>
    /// The dish name at the time of ordering.
    /// </summary>
    public string DishName { get; set; } = string.Empty;

    /// <summary>
    /// The unit price at the time of ordering.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// The quantity ordered.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Computed subtotal (Quantity × UnitPrice).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Special instructions for this item.
    /// </summary>
    public string? SpecialInstructions { get; set; }
}
