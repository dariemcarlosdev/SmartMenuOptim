using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents the status of an order in the restaurant system.
/// </summary>
public class OrderStatus : TenantEntityBase
{
    /// <summary>
    /// The name/title of the order status (e.g., "Pending", "Preparing", "Ready", etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description providing more details about what this status means.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// The display order for showing statuses in UI elements. Lower numbers appear first.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indicates if this is a terminal status (e.g., "Completed", "Cancelled") 
    /// that shouldn't transition to other statuses.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Color code for UI representation (e.g., "#FF0000" for red).
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Navigation property for orders with this status.
    /// </summary>
    public ICollection<Order> Orders { get; set; } = [];
}