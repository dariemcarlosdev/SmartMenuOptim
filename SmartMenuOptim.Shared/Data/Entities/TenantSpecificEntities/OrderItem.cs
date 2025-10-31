using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents an individual item within an order.
/// </summary>
/// <remarks>
/// Validation and indexing:
/// - Basic validation attributes are applied here (quantity, price, instruction length).
/// - Indexes for common query patterns (by OrderId, DishId, RestaurantId) are centralized in `AppDbContext.OnModelCreating`.
/// </remarks>
[Table("OrderItems")]
public class OrderItem : TenantEntityBase, IValidatableObject
{
    /// <summary>
    /// The order this item belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Order))]
    public int OrderId { get; set; }

    /// <summary>
    /// The dish that was ordered.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Dish))]
    public int DishId { get; set; }

    /// <summary>
    /// The quantity of this item ordered. Must be at least 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    /// <summary>
    /// The price of the dish at the time of ordering.
    /// Stored as decimal(18,2) in the DB.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Any special instructions or notes for this item.
    /// </summary>
    [MaxLength(500, ErrorMessage = "SpecialInstructions cannot exceed 500 characters")]
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// The subtotal for this item (Quantity * UnitPrice). Not mapped because it's computed.
    /// </summary>
    [NotMapped]
    public decimal Subtotal => Quantity * UnitPrice;

    // === Navigation Properties ===

    /// <summary>
    /// Navigation property to the parent order.
    /// </summary>
    [InverseProperty(nameof(Order.OrderItems))]
    public Order? Order { get; set; }

    /// <summary>
    /// Navigation property to the associated dish.
    /// </summary>
    [InverseProperty(nameof(Dish.OrderItems))]
    public Dish? Dish { get; set; }

    // === Validation ===
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity < 1)
            yield return new ValidationResult("Quantity must be at least 1.", new[] { nameof(Quantity) });

        if (UnitPrice < 0)
            yield return new ValidationResult("UnitPrice must be non-negative.", new[] { nameof(UnitPrice) });

        // Subtotal is derived; overflow is highly unlikely for realistic order values. If you expect extreme values,
        // add explicit checks here (e.g., max allowed unit price or quantity).

        yield break;
    }
}