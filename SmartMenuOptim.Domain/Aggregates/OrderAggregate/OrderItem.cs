using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;

namespace SmartMenuOptim.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Order line item representing a dish selection with quantity, pricing, and instructions.
/// Child entity of Order aggregate - mutable with rich behavior.
/// </summary>
/// <remarks>
/// 🧩 CHILD ENTITY - Order Aggregate (Tier 1)
/// 
/// Key Characteristics:
/// • Mutable: Quantity can be updated via Order.UpdateItemQuantity()
/// • Created only via Order.AddItem()
/// • Private setters enforce invariants
/// • Auto-calculates Subtotal (Quantity × UnitPrice)
/// • Stores price snapshot at order time (not current dish price)
/// 
/// Business Rules:
/// • Quantity must be at least 1
/// • UnitPrice must be non-negative
/// • References Dish aggregate (read-only)
/// • SpecialInstructions max 500 chars
/// 
/// <code>
/// // ✅ CORRECT - Through parent aggregate
/// order.AddItem(dishId, 12.99m, 2, "No onions");
/// order.UpdateItemQuantity(orderItemId, 3);
/// 
/// // ❌ WRONG - Direct instantiation
/// var item = new OrderItem(...);
/// </code>
/// </remarks>
[Table("OrderItems")]
public class OrderItem : TenantEntityBase, IValidatableObject
{
    // === Properties with Private Setters (Aggregate Pattern) ===
    
    /// <summary>
    /// The order this item belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Order))]
    public int OrderId { get; set; } // EF Core needs public setter for FK

    /// <summary>
    /// The dish that was ordered.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Dish))]
    public int DishId { get; set; } // EF Core needs public setter for FK

    /// <summary>
    /// The quantity of this item ordered. Must be at least 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; private set; }

    /// <summary>
    /// The price of the dish at the time of ordering.
    /// Stored as decimal(18,2) in the DB.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be non-negative")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Any special instructions or notes for this item.
    /// </summary>
    [MaxLength(500, ErrorMessage = "SpecialInstructions cannot exceed 500 characters")]
    public string? SpecialInstructions { get; set; } // Public setter for EF Core

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
    
    // === Constructors ===
    
    /// <summary>
    /// Protected constructor for EF Core.
    /// </summary>
    protected OrderItem() { }
    
    /// <summary>
    /// Internal constructor for creating order items within Order aggregate.
    /// </summary>
    internal OrderItem(int dishId, decimal unitPrice, int quantity)
    {
        DishId = dishId;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
    
    // === Business Methods ===
    
    /// <summary>
    /// Updates the quantity for this order item.
    /// Called by Order aggregate root.
    /// </summary>
    internal void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(newQuantity));
        
        Quantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // === Validation ===
    
    /// <summary>
    /// Validates the order item ensuring data consistency and business rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Restaurant/Tenant validation
        if (RestaurantId <= 0)
        {
            yield return new ValidationResult(
                "OrderItem must be associated with a restaurant",
                new[] { nameof(RestaurantId) }
            );
        }

        // Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            yield return new ValidationResult(
                "Restaurant navigation property is inconsistent with RestaurantId",
                new[] { nameof(Restaurant), nameof(RestaurantId) }
            );
        }

        // Order tenant consistency
        if (Order != null && Order.RestaurantId != RestaurantId)
        {
            yield return new ValidationResult(
                "OrderItem must belong to same restaurant as Order",
                new[] { nameof(Order), nameof(RestaurantId) }
            );
        }

        // Dish tenant consistency
        if (Dish != null && Dish.RestaurantId != RestaurantId)
        {
            yield return new ValidationResult(
                "Referenced Dish must belong to same restaurant",
                new[] { nameof(Dish), nameof(RestaurantId) }
            );
        }

        // Quantity validation
        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                "Quantity must be positive",
                new[] { nameof(Quantity) }
            );
        }

        // UnitPrice validation
        if (UnitPrice < 0)
        {
            yield return new ValidationResult(
                "UnitPrice cannot be negative",
                new[] { nameof(UnitPrice) }
            );
        }

        // Price consistency with Dish (if dish is loaded)
        if (Dish != null)
        {
            const decimal tolerance = 0.01m;
            if (Math.Abs(UnitPrice - Dish.DishPrice) > tolerance)
            {
                // Check if price matches a special menu price
                var hasValidPrice = Dish.MenuDishes
                    .Any(md => md.SpecialPrice.HasValue && 
                              Math.Abs(UnitPrice - md.SpecialPrice.Value) < tolerance);
                
                if (!hasValidPrice)
                {
                    yield return new ValidationResult(
                        $"Unit price {UnitPrice:C} does not match dish price {Dish.DishPrice:C} or any menu special price",
                        new[] { nameof(UnitPrice) }
                    );
                }
            }
        }
    }
}
