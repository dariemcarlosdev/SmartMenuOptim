using System.ComponentModel.DataAnnotations;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents a customer order in the restaurant system.
/// </summary>
public class Order : TenantEntityBase
{
    /// <summary>
    /// The customer who placed the order.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// The current status of the order.
    /// </summary>
    public required OrderStatus Status { get; set; }

    /// <summary>
    /// Total amount of the order, computed from OrderItems.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Date and time when the order was placed (UTC).
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Special instructions or notes for the entire order.
    /// </summary>
    [MaxLength(1000)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Navigation property to the customer who placed the order.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Navigation property for the order items.
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}