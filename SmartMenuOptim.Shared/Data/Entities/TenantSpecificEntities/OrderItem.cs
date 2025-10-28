using System.ComponentModel.DataAnnotations;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents an individual item within an order.
/// </summary>
public class OrderItem : TenantEntityBase
{
    /// <summary>
    /// The order this item belongs to.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// The dish that was ordered.
    /// </summary>
    public int DishId { get; set; }

    /// <summary>
    /// The quantity of this item ordered.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// The price of the dish at the time of ordering.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Any special instructions or notes for this item.
    /// </summary>
    [MaxLength(500)]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// The subtotal for this item (Quantity * UnitPrice).
    /// </summary>
    public decimal Subtotal => Quantity * UnitPrice;

    // Navigation Properties

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    public Order? Order { get; set; }

    /// <summary>
    /// Navigation property to the associated dish.
    /// </summary>
    public Dish? Dish { get; set; }
}