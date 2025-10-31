using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a sales record for a dish.
    /// </summary>
    /// <remarks>
    /// - Tenant-scoped entity: inherits from TenantEntityBase and therefore includes a RestaurantId for tenant isolation.
    /// - Indexes related to sales analysis are defined centrally in `AppDbContext.OnModelCreating` (see IX_SaleRecords_Restaurant_Dish_Date).
    /// - Validation is applied to ensure data integrity (non-negative quantities, sensible sale dates).
    /// </remarks>
    [Table("SaleRecords")]
    public class SaleRecord : TenantEntityBase, IValidatableObject
    {
        // === Standalone Properties ===

        /// <summary>
        /// Quantity of the dish sold in this record. Must be zero or positive.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "QuantitySold must be zero or a positive integer")]
        public int QuantitySold { get; set; }

        /// <summary>
        /// Date of the sale (UTC).
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        // === Relationship Properties (Foreign Keys) ===

        /// <summary>
        /// Foreign key to the Dish entity. Each sale record is for a single dish.
        /// </summary>
        [Required]
        [ForeignKey(nameof(Dish))]
        public int DishId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the Dish this sales record is for.
        /// </summary>
        [InverseProperty(nameof(Dish.SaleRecords))]
        public Dish? Dish { get; set; }

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DishId <= 0)
                yield return new ValidationResult("DishId must be a positive integer.", new[] { nameof(DishId) });

            if (QuantitySold < 0)
                yield return new ValidationResult("QuantitySold cannot be negative.", new[] { nameof(QuantitySold) });

            // SaleDate should not be in the future (allow slight clock skew: up to 1 minute)
            if (SaleDate > DateTime.UtcNow.AddMinutes(1))
                yield return new ValidationResult("SaleDate cannot be in the future.", new[] { nameof(SaleDate) });

            yield break;
        }
    }
}
