namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for detailed Order view with items.
/// </summary>
/// <remarks>
/// Extended version of <see cref="OrderDTO"/> including the full item list and status details.
/// Used for order detail pages and full order inspection.
/// </remarks>
public class OrderDetailDTO
{
    /// <summary>
    /// Order identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; set; }

    /// <summary>
    /// Customer identifier who placed the order.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Customer name for display purposes.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Current order status identifier.
    /// </summary>
    public int OrderStatusId { get; set; }

    /// <summary>
    /// Current order status name.
    /// </summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// Current order status color code for UI rendering.
    /// </summary>
    public string? StatusColorCode { get; set; }

    /// <summary>
    /// Whether the order is in a terminal state.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Total amount of the order.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Date and time when the order was placed.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Special instructions for the entire order.
    /// </summary>
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Staff member identifier if assigned.
    /// </summary>
    public int? HandledByStaffId { get; set; }

    /// <summary>
    /// Staff member name if assigned.
    /// </summary>
    public string? HandledByStaffName { get; set; }

    /// <summary>
    /// List of order items with dish details.
    /// </summary>
    public List<OrderItemDTO> Items { get; set; } = [];

    /// <summary>
    /// Date and time when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the order was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
