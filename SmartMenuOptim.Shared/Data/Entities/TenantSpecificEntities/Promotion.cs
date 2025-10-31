using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a promotion (discount) offered by a restaurant.
    /// </summary>
    /// <remarks>
    /// - Validation ensures a non-empty name, non-negative discount amount, and valid date range.
    /// - Indexes related to promotions (active/date range) are centralized in `AppDbContext.OnModelCreating`.
    /// </remarks>
    [Table("Promotions")]
    public class Promotion : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Human-friendly name for the promotion.
        /// </summary>
        [Required(ErrorMessage = "Promotion name is required")]
        [MaxLength(150, ErrorMessage = "Promotion name cannot exceed 150 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Fixed discount amount (in restaurant currency) applied by this promotion.
        /// Must be non-negative. Stored as decimal(18,2).
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "DiscountAmount must be non-negative and reasonable")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Start date (inclusive) when the promotion becomes valid.
        /// </summary>
        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "ValidFrom is required")]
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// End date (inclusive) when the promotion expires.
        /// </summary>
        [DataType(DataType.DateTime)]
        [Required(ErrorMessage = "ValidTo is required")]
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Optional notes or terms for the promotion.
        /// </summary>
        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ValidFrom > ValidTo)
            {
                yield return new ValidationResult("ValidFrom must be earlier than or equal to ValidTo.", new[] { nameof(ValidFrom), nameof(ValidTo) });
            }

            if (DiscountAmount < 0)
            {
                yield return new ValidationResult("DiscountAmount cannot be negative.", new[] { nameof(DiscountAmount) });
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
            }

            yield break;
        }
    }
}