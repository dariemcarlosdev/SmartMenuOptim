namespace SmartMenuOptim.Application.Features.Orders.DTOs;

/// <summary>
/// Data Transfer Object for OrderStatus lookup entity.
/// </summary>
/// <remarks>
/// Used in status dropdowns, order cards, and workflow displays.
/// </remarks>
public class OrderStatusDTO
{
    /// <summary>
    /// OrderStatus identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Status name (e.g., "Pending", "Preparing", "Completed").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this status means.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Display order for UI sorting. Lower numbers appear first.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is a terminal/end state (e.g., Completed, Cancelled).
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Hex color code for UI rendering (e.g., "#28A745").
    /// </summary>
    public string? ColorCode { get; set; }
}
