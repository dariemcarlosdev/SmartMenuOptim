using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a reservation for a table at a restaurant.
    /// </summary>
    /// <remarks>
    /// - CustomerId is optional to support anonymous reservations.
    /// - TableId and ReservationTime are required.
    /// - Indexes for reservation queries are defined centrally in `AppDbContext`.
    /// </remarks>
    [Table("Reservations")]
    public class Reservation : TenantEntityBase, IValidatableObject
    {
        /// <summary>
        /// Foreign key for Customer (nullable to allow anonymous reservations).
        /// </summary>
        [ForeignKey(nameof(Customer))]
        public int? CustomerId { get; set; }

        /// <summary>
        /// Foreign key to the table being reserved.
        /// </summary>
        [Required(ErrorMessage = "TableId is required")]
        [ForeignKey(nameof(Table))]
        public int TableId { get; set; }

        /// <summary>
        /// Date and time when the reservation is scheduled (UTC).
        /// </summary>
        [Required(ErrorMessage = "ReservationTime is required")]
        [DataType(DataType.DateTime)]
        public DateTime ReservationTime { get; set; }

        // Navigation properties

        /// <summary>
        /// The table reserved.
        /// </summary>
        [InverseProperty(nameof(Table.Reservations))]
        public Table? Table { get; set; }

        /// <summary>
        /// The customer who made the reservation (optional).
        /// </summary>
        [InverseProperty(nameof(Customer.Reservations))]
        public Customer? Customer { get; set; }

        // === Validation ===
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // TableId must be a positive integer
            if (TableId <= 0)
            {
                yield return new ValidationResult("TableId must be a positive integer.", new[] { nameof(TableId) });
            }

            // ReservationTime must not be unreasonably old or too far in the future
            var now = DateTime.UtcNow;
            if (ReservationTime < now.AddDays(-1))
            {
                yield return new ValidationResult("ReservationTime cannot be older than 1 day.", new[] { nameof(ReservationTime) });
            }

            if (ReservationTime > now.AddYears(1))
            {
                yield return new ValidationResult("ReservationTime cannot be more than 1 year in the future.", new[] { nameof(ReservationTime) });
            }

            yield break;
        }
    }
}
