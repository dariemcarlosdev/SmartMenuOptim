using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a customer order in the restaurant system.
    /// </summary>
    [Table("Orders")]
    public class Order : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Foreign key to the global Customer entity (who placed the order).
        /// </summary>
        [Required]
        [ForeignKey(nameof(Customer))]
        public int CustomerId { get; set; }

        /// <summary>
        /// Foreign key to the OrderStatus entity, indicating the current status of the order.
        /// </summary>
        [Required]
        [ForeignKey(nameof(Status))]
        public int OrderStatusId { get; set; }

        /// <summary>
        /// Total amount of the order, computed from OrderItems.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be non-negative")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Date and time when the order was placed (UTC).
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Special instructions or notes for the entire order.
        /// </summary>
        [MaxLength(1000)]
        public string? SpecialInstructions { get; set; }

        /// <summary>
        /// Foreign key for the staff member who handled this order (optional).
        /// It could be optional since not all orders may be handled by a staff member (e.g., self-service, or automated orders).
        /// </summary>
        [ForeignKey(nameof(HandledBy))]
        public int? HandledByStaffId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// The current status of the order.
        /// </summary>
        public OrderStatus Status { get; set; } = default!;

        /// <summary>
        /// Navigation property to the customer who placed the order.
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// Navigation property for the order items. Initialized to avoid null references.
        /// </summary>
        [InverseProperty(nameof(OrderItem.Order))]
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Navigation property to the staff member who handled this order (optional).
        /// </summary>
        public StaffMember? HandledBy { get; set; }

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Order date must not be in the far future
            if (OrderDate > DateTime.UtcNow.AddHours(1))
            {
                yield return new ValidationResult("OrderDate cannot be in the future.", new[] { nameof(OrderDate) });
            }

            // TotalAmount should match sum of order items if items are present
            if (OrderItems != null && OrderItems.Any())
            {
                var sum = OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
                if (sum != TotalAmount)
                {
                    yield return new ValidationResult("TotalAmount does not match sum of OrderItems.", new[] { nameof(TotalAmount), nameof(OrderItems) });
                }
            }

            yield break;
        }
    }
}