using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a physical table in a restaurant (tenant-scoped).
    /// Validation attributes ensure table number and capacity meet expected constraints.
    /// Indexing for table availability is centralized in `AppDbContext.OnModelCreating`.
    /// </summary>
    [Table("RestaurantTables")]
    public class Table : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Human-friendly table identifier (e.g., "1", "A1").
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TableNumber { get; set; } = string.Empty;

        /// <summary>
        /// Number of seats at the table. Must be at least 1.
        /// </summary>
        [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100")]
        public int Capacity { get; set; } = 1;

        /// <summary>
        /// Indicates if the table is currently available for immediate seating.
        /// Separate from reservations which are scheduled.
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Reservations associated with this table. Initialized to avoid null references.
        /// Navigation property; indexes for reservation queries are centralized in AppDbContext.
        /// </summary>
        [InverseProperty(nameof(Reservation.Table))]
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(TableNumber))
            {
                yield return new ValidationResult("TableNumber is required.", new[] { nameof(TableNumber) });
            }

            if (Capacity < 1 || Capacity > 100)
            {
                yield return new ValidationResult("Capacity must be between 1 and 100.", new[] { nameof(Capacity) });
            }

            yield break;
        }
    }
}
