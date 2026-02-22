using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Exceptions;

namespace SmartMenuOptim.Domain.Aggregates.TableAggregate
{
    /// <summary>
    /// Represents the lifecycle status of a reservation.
    /// </summary>
    /// <remarks>
    /// <para><strong>DDD Design Decision - Enum Co-location with Aggregate</strong></para>
    /// 
    /// <para>This enum is intentionally placed within Reservation.cs rather than in a separate file because:</para>
    /// <list type="number">
    ///   <item><description><strong>High Cohesion:</strong> The enum is tightly coupled to the Reservation aggregate and represents its core state machine</description></item>
    ///   <item><description><strong>Single Responsibility:</strong> Only the Reservation entity uses this status - it's not shared across multiple aggregates</description></item>
    ///   <item><description><strong>Encapsulation:</strong> The enum is an integral part of Reservation's invariants and business rules</description></item>
    ///   <item><description><strong>Discoverability:</strong> Developers find everything related to Reservation lifecycle in one place</description></item>
    /// </list>
    /// 
    /// <para><strong>DDD Best Practice:</strong> Keep enums WITH their aggregate when they represent aggregate-specific state.</para>
    /// 
    /// <para><strong>Alternative Considered:</strong> Moving to separate file would only be justified if:</para>
    /// <list type="bullet">
    ///   <item><description>The enum grows beyond 20 values</description></item>
    ///   <item><description>Multiple classes in the same aggregate need this enum (e.g., ReservationHistory)</description></item>
    ///   <item><description>The Reservation.cs file exceeds 1000 lines</description></item>
    /// </list>
    /// 
    /// <para><strong>State Transitions:</strong></para>
    /// <code>
    ///        Pending
    ///       /       \
    ///  Confirmed   Cancelled
    ///    /  |  \
    /// Seated  |  NoShow
    ///    |    |
    /// Completed Cancelled
    /// </code>
    /// </remarks>
    public enum ReservationStatus
    {
        /// <summary>
        /// Reservation has been created but not yet confirmed.
        /// Awaiting confirmation from customer or restaurant.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Reservation has been confirmed by the restaurant.
        /// Customer is expected to arrive at the scheduled time.
        /// </summary>
        Confirmed = 1,

        /// <summary>
        /// Customer has arrived and been seated at the table.
        /// Reservation is currently active.
        /// </summary>
        Seated = 2,

        /// <summary>
        /// Reservation has been fulfilled and customers have left.
        /// Table is available for new reservations.
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Reservation was cancelled by customer or restaurant.
        /// Table is available for other reservations.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// Customer did not show up for their reservation.
        /// Table remained empty during the reserved time slot.
        /// </summary>
        NoShow = 5
    }

    /// <summary>
    /// Time-based table booking linking customers to tables with reservation details.
    /// Child entity of Table aggregate - semi-mutable with complex cross-aggregate references.
    /// </summary>
    /// <remarks>
    /// 🧩 CHILD ENTITY (⚠️ Debatable) - Table Aggregate (Tier 1)
    /// 
    /// Key Characteristics:
    /// • Semi-mutable: Core fields immutable, but status-like updates possible
    /// • Created only via Table.MakeReservation()
    /// • Internal constructor prevents external instantiation
    /// • Inherits from TenantEntityBase (unusual for child entity)
    /// • References multiple aggregates (Table, Customer, Restaurant)
    /// 
    /// Why Semi-Mutable:
    /// • ReservationTime, TableId, CustomerId are immutable (set at creation)
    /// • CustomerName/Phone can be updated for corrections (semi-mutable)
    /// • PartySize may be adjustable before arrival (semi-mutable)
    /// • No behavioral methods - modifications handled by Table aggregate
    /// • Represents a commitment that shouldn't change fundamentally
    /// 
    /// ⚠️ Design Debate:
    /// This entity shows characteristics that question its classification as a pure child:
    /// • Has TenantEntityBase inheritance (child entities typically don't)
    /// • Coordinates between multiple aggregates (Table, Customer, Restaurant)
    /// • Complex multi-tenant validation across entity boundaries
    /// • Could arguably be Tier 2 Simple Aggregate with its own repository
    /// 
    /// Business Rules:
    /// • Must be in future (15-minute grace period)
    /// • Cannot be > 6 months in advance
    /// • Requires CustomerId OR (CustomerName + CustomerPhone)
    /// • Must match parent Table's RestaurantId
    /// • PartySize must be positive (if specified)
    /// 
    /// Reservation Types:
    /// • Registered: Has CustomerId, links to Customer entity
    /// • Walk-in/Anonymous: No CustomerId, uses CustomerName + CustomerPhone
    /// 
    /// <code>
    /// // ✅ CORRECT - Through parent aggregate
    /// // Registered customer
    /// table.MakeReservation(DateTime.UtcNow.AddDays(1), customerId: 123, partySize: 4);
    /// 
    /// // Anonymous/walk-in
    /// table.MakeReservation(DateTime.UtcNow.AddDays(2), 
    ///     customerName: "John Doe", 
    ///     customerPhone: "+1-555-1234", 
    ///     partySize: 2);
    /// 
    /// // Cancel reservation
    /// table.CancelReservation(reservationId);
    /// 
    /// // ❌ WRONG - Direct instantiation
    /// var reservation = new Reservation(...); // Won't compile - internal constructor
    /// </code>
    /// </remarks>
    [Table("Reservations")]
    public class Reservation : TenantEntityBase
    {
        // ===================================================================
        // PROPERTIES WITH ENCAPSULATION (Private Setters)
        // ===================================================================

        /// <summary>
        /// Foreign key to the Table being reserved (required).
        /// </summary>
        /// <remarks>
        /// - Set at creation by parent Table aggregate
        /// - Immutable after creation
        /// - Must reference a valid table in the same restaurant
        /// </remarks>
        [ForeignKey(nameof(Table))]
        public int TableId { get; private set; }

        /// <summary>
        /// Date and time when the reservation is scheduled (UTC).
        /// </summary>
        /// <remarks>
        /// - Must be in the future (15-minute grace period allowed)
        /// - Cannot be more than 6 months in advance
        /// - Used for overlap detection by Table aggregate
        /// - Immutable after creation
        /// </remarks>
        public DateTime ReservationTime { get; private set; }

        /// <summary>
        /// Foreign key for Customer (nullable to support anonymous/walk-in reservations).
        /// </summary>
        /// <remarks>
        /// - Null for walk-in reservations
        /// - When null, CustomerName and CustomerPhone are required
        /// - When set, links to registered customer account
        /// - Customer must be active and not deleted
        /// </remarks>
        [ForeignKey(nameof(Customer))]
        public int? CustomerId { get; private set; }

        /// <summary>
        /// Name of the customer making the reservation.
        /// </summary>
        /// <remarks>
        /// - Required for anonymous reservations (when CustomerId is null)
        /// - Optional for registered customers (can be populated from Customer entity)
        /// - Maximum length: 100 characters
        /// - Used for identification when customer arrives
        /// </remarks>
        public string? CustomerName { get; private set; }

        /// <summary>
        /// Phone number for contacting the customer about the reservation.
        /// </summary>
        /// <remarks>
        /// - Required for anonymous reservations (when CustomerId is null)
        /// - Optional for registered customers (can be populated from Customer entity)
        /// - Used for confirmation calls or cancellation notifications
        /// - Should be in valid phone format
        /// </remarks>
        public string? CustomerPhone { get; private set; }

        /// <summary>
        /// Number of people in the party (optional).
        /// </summary>
        /// <remarks>
        /// - Used for capacity planning
        /// - Helps staff prepare appropriate table setup
        /// - Must be positive if specified
        /// - If not specified, defaults to table capacity
        /// </remarks>
        public int? PartySize { get; private set; }

        /// <summary>
        /// Optional notes or special requests for the reservation.
        /// </summary>
        /// <remarks>
        /// - Examples: "Birthday celebration", "Need high chair", "Allergies: peanuts"
        /// - Maximum length: 500 characters
        /// - Displayed to restaurant staff
        /// </remarks>
        public string? Notes { get; private set; }

        /// <summary>
        /// Current status of the reservation in its lifecycle.
        /// </summary>
        /// <remarks>
        /// Status Lifecycle:
        /// - Pending → Confirmed → Seated → Completed (normal flow)
        /// - Pending → Cancelled (customer/restaurant cancellation)
        /// - Confirmed → Cancelled (late cancellation)
        /// - Confirmed → NoShow (customer didn't arrive)
        /// - Confirmed → Seated → Completed (arrived and completed)
        /// 
        /// State Transitions:
        /// - Pending can transition to: Confirmed, Cancelled
        /// - Confirmed can transition to: Seated, Cancelled, NoShow
        /// - Seated can transition to: Completed
        /// - Cancelled, NoShow, Completed are terminal states
        /// </remarks>
        public ReservationStatus Status { get; private set; }

        // ===================================================================
        // NAVIGATION PROPERTIES
        // ===================================================================

        /// <summary>
        /// Navigation property to the parent Table aggregate root.
        /// </summary>
        /// <remarks>
        /// - Required for tenant consistency validation
        /// - May be null if not loaded (lazy loading)
        /// - Used to validate reservation belongs to same restaurant as table
        /// </remarks>
        public Table? Table { get; private set; }

        /// <summary>
        /// Navigation property to the Customer who made the reservation (optional).
        /// </summary>
        /// <remarks>
        /// - Null for anonymous/walk-in reservations
        /// - When loaded, used for tenant consistency validation
        /// - Provides access to customer details for registered customers
        /// </remarks>
        [InverseProperty(nameof(Customer.Reservations))]
        public Customer? Customer { get; private set; }

        // ===================================================================
        // CONSTRUCTORS
        // ===================================================================

        /// <summary>
        /// Protected parameterless constructor for EF Core.
        /// Required for entity materialization from database.
        /// </summary>
        /// <remarks>
        /// Not for direct use. EF Core uses this via reflection.
        /// </remarks>
        protected Reservation() { }

        /// <summary>
        /// Creates a new reservation for a registered customer.
        /// Internal constructor - can only be called from within the domain assembly (by Table aggregate).
        /// </summary>
        /// <param name="tableId">The table identifier being reserved.</param>
        /// <param name="restaurantId">The restaurant (tenant) identifier.</param>
        /// <param name="reservationTime">The date/time of the reservation (must be in future).</param>
        /// <param name="customerId">The registered customer identifier.</param>
        /// <param name="partySize">Optional number of people in the party.</param>
        /// <param name="notes">Optional special requests or notes.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        /// <remarks>
        /// This constructor is for reservations by registered customers.
        /// For walk-in/anonymous reservations, use the overload with customerName and customerPhone.
        /// </remarks>
        internal Reservation(
            int tableId,
            int restaurantId,
            DateTime reservationTime,
            int customerId,
            int? partySize = null,
            string? notes = null)
        {
            // ---------------------------------------------------------------
            // PARAMETER GUARD CLAUSES — ArgumentException
            //
            // These validate constructor preconditions (caller contract),
            // NOT domain business rules.
            //
            // • ArgumentException → 400 Bad Request (programming error)
            // • ReservationDomainException → 422 Unprocessable Entity
            //   (valid input, but violates a reservation lifecycle rule)
            //
            // EXAMPLES:
            // • tableId = 0         → ArgumentException  (caller bug)
            // • partySize = -1      → ArgumentException  (caller bug)
            // • Confirm() on Seated → ReservationDomainException (business rule)
            // • Cancel() on NoShow  → ReservationDomainException (business rule)
            // ---------------------------------------------------------------

            // Validate invariants
            ValidateTableId(tableId);
            ValidateRestaurantId(restaurantId);
            ValidateReservationTime(reservationTime);
            ValidateCustomerId(customerId);
            if (partySize.HasValue)
                ValidatePartySize(partySize.Value);
            if (!string.IsNullOrEmpty(notes))
                ValidateNotes(notes);

            // Set properties
            TableId = tableId;
            RestaurantId = restaurantId;
            ReservationTime = reservationTime;
            CustomerId = customerId;
            PartySize = partySize;
            Notes = notes;
            Status = ReservationStatus.Pending; // New reservations start as Pending
        }

        /// <summary>
        /// Creates a new reservation for a walk-in/anonymous customer.
        /// Internal constructor - can only be called from within the domain assembly (by Table aggregate).
        /// </summary>
        /// <param name="tableId">The table identifier being reserved.</param>
        /// <param name="restaurantId">The restaurant (tenant) identifier.</param>
        /// <param name="reservationTime">The date/time of the reservation (must be in future).</param>
        /// <param name="customerName">Name of the customer (required for anonymous reservations).</param>
        /// <param name="customerPhone">Phone number for contact (required for anonymous reservations).</param>
        /// <param name="partySize">Optional number of people in the party.</param>
        /// <param name="notes">Optional special requests or notes.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        /// <remarks>
        /// This constructor is for walk-in/anonymous reservations without a registered customer account.
        /// CustomerName and CustomerPhone are required for contact purposes.
        /// </remarks>
        internal Reservation(
            int tableId,
            int restaurantId,
            DateTime reservationTime,
            string customerName,
            string customerPhone,
            int? partySize = null,
            string? notes = null)
        {
            // Validate invariants
            ValidateTableId(tableId);
            ValidateRestaurantId(restaurantId);
            ValidateReservationTime(reservationTime);
            ValidateCustomerName(customerName);
            ValidateCustomerPhone(customerPhone);
            if (partySize.HasValue)
                ValidatePartySize(partySize.Value);
            if (!string.IsNullOrEmpty(notes))
                ValidateNotes(notes);

            // Set properties
            TableId = tableId;
            RestaurantId = restaurantId;
            ReservationTime = reservationTime;
            CustomerId = null; // Anonymous reservation
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            PartySize = partySize;
            Notes = notes;
            Status = ReservationStatus.Pending; // New reservations start as Pending
        }

        // ===================================================================
        // BEHAVIORAL METHODS - STATUS TRANSITIONS
        // ===================================================================

        /// <summary>
        /// Confirms the reservation, transitioning from Pending to Confirmed status.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown if reservation is not in Pending status.</exception>
        /// <remarks>
        /// This method should be called when the restaurant confirms the reservation.
        /// Only pending reservations can be confirmed.
        /// </remarks>
        public void Confirm()
        {
            if (Status != ReservationStatus.Pending)
            {
                throw new ReservationDomainException(
                    $"Can only confirm pending reservations. Current status: {Status}");
            }

            Status = ReservationStatus.Confirmed;
        }

        /// <summary>
        /// Marks the customer as seated, transitioning from Confirmed to Seated status.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown if reservation is not in Confirmed status.</exception>
        /// <remarks>
        /// This method should be called when the customer arrives and is seated at the table.
        /// Only confirmed reservations can be marked as seated.
        /// </remarks>
        public void MarkSeated()
        {
            if (Status != ReservationStatus.Confirmed)
            {
                throw new ReservationDomainException(
                    $"Can only seat confirmed reservations. Current status: {Status}");
            }

            Status = ReservationStatus.Seated;
        }

        /// <summary>
        /// Completes the reservation, transitioning from Seated to Completed status.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown if reservation is not in Seated status.</exception>
        /// <remarks>
        /// This method should be called when the customers finish their meal and leave.
        /// Only seated reservations can be completed.
        /// After completion, the table becomes available for new reservations.
        /// </remarks>
        public void Complete()
        {
            if (Status != ReservationStatus.Seated)
            {
                throw new ReservationDomainException(
                    $"Can only complete seated reservations. Current status: {Status}");
            }

            Status = ReservationStatus.Completed;
        }

        /// <summary>
        /// Cancels the reservation, transitioning to Cancelled status.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown if reservation is already in a terminal state.</exception>
        /// <remarks>
        /// This method can be called by either the customer or restaurant to cancel the reservation.
        /// Can only cancel reservations in Pending or Confirmed status.
        /// Cannot cancel reservations that are already Seated, Completed, NoShow, or Cancelled.
        /// </remarks>
        public void Cancel()
        {
            if (Status == ReservationStatus.Seated)
            {
                throw new ReservationDomainException(
                    "Cannot cancel a reservation where customers are already seated.");
            }

            if (Status == ReservationStatus.Completed)
            {
                throw new ReservationDomainException(
                    "Cannot cancel an already completed reservation.");
            }

            if (Status == ReservationStatus.NoShow)
            {
                throw new ReservationDomainException(
                    "Cannot cancel a no-show reservation.");
            }

            if (Status == ReservationStatus.Cancelled)
            {
                throw new ReservationDomainException(
                    "Reservation is already cancelled.");
            }

            Status = ReservationStatus.Cancelled;
        }

        /// <summary>
        /// Marks the reservation as a no-show, indicating the customer did not arrive.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown if reservation is not in Confirmed status.</exception>
        /// <remarks>
        /// This method should be called when the customer fails to arrive within a reasonable time
        /// after their reservation time (e.g., 15-30 minutes).
        /// Only confirmed reservations can be marked as no-show.
        /// After marking as no-show, the table becomes available for walk-in customers.
        /// </remarks>
        public void MarkNoShow()
        {
            if (Status != ReservationStatus.Confirmed)
            {
                throw new ReservationDomainException(
                    $"Can only mark confirmed reservations as no-show. Current status: {Status}");
            }

            Status = ReservationStatus.NoShow;
        }

        /// <summary>
        /// Checks if the reservation is in an active state (not cancelled, completed, or no-show).
        /// </summary>
        /// <returns>True if the reservation is active (Pending, Confirmed, or Seated), false otherwise.</returns>
        public bool IsActive()
        {
            return Status == ReservationStatus.Pending ||
                   Status == ReservationStatus.Confirmed ||
                   Status == ReservationStatus.Seated;
        }

        /// <summary>
        /// Checks if the reservation is in a terminal state (cannot be modified further).
        /// </summary>
        /// <returns>True if the reservation is in Completed, Cancelled, or NoShow status.</returns>
        public bool IsTerminal()
        {
            return Status == ReservationStatus.Completed ||
                   Status == ReservationStatus.Cancelled ||
                   Status == ReservationStatus.NoShow;
        }

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the reservation maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="ReservationDomainException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - Table parent belongs to the same restaurant
        /// - Customer (if registered) belongs to the same restaurant
        /// 
        /// Tenant Consistency Rules:
        /// 1. Reservation must belong to exactly one restaurant
        /// 2. Table must belong to the same restaurant
        /// 3. Customer (if registered) must belong to the same restaurant
        /// 4. Restaurant navigation (if loaded) must match RestaurantId
        /// 5. Customer must be active and not deleted (if registered)
        /// 
        /// This is a critical security and data integrity check for multi-tenant systems.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new ReservationDomainException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate Table parent tenant consistency
            if (Table != null)
            {
                if (Table.Id != TableId)
                {
                    throw new ReservationDomainException(
                        $"Table navigation property ID ({Table.Id}) does not match TableId ({TableId}).");
                }

                if (Table.RestaurantId != RestaurantId)
                {
                    throw new ReservationDomainException(
                        $"Reservation must belong to same restaurant as Table. " +
                        $"Reservation RestaurantId: {RestaurantId}, Table RestaurantId: {Table.RestaurantId}");
                }
            }

            // Validate Customer tenant consistency (if registered reservation)
            if (Customer != null)
            {
                if (CustomerId.HasValue && Customer.Id != CustomerId.Value)
                {
                    throw new ReservationDomainException(
                        $"Customer navigation property ID ({Customer.Id}) does not match CustomerId ({CustomerId}).");
                }

                if (Customer.IsDeleted)
                {
                    throw new ReservationDomainException(
                        $"Cannot have reservation for deleted customer (CustomerId: {CustomerId}).");
                }

                if (!Customer.IsActive)
                {
                    throw new ReservationDomainException(
                        $"Cannot have reservation for inactive customer (CustomerId: {CustomerId}).");
                }
            }
        }

        // ===================================================================
        // PRIVATE VALIDATION METHODS (Guard Clauses)
        // ===================================================================

        private static void ValidateTableId(int tableId)
        {
            if (tableId <= 0)
            {
                throw new ArgumentException(
                    "TableId must be a positive integer.",
                    nameof(tableId));
            }
        }

        private static void ValidateRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                throw new ArgumentException(
                    "RestaurantId must be a positive integer.",
                    nameof(restaurantId));
            }
        }

        private static void ValidateReservationTime(DateTime reservationTime)
        {
            var now = DateTime.UtcNow;
            
            // Allow 15-minute grace period for reservations
            if (reservationTime < now.AddMinutes(-15))
            {
                throw new ArgumentException(
                    "Reservation time cannot be in the past.",
                    nameof(reservationTime));
            }

            // Cannot be more than 6 months in advance
            if (reservationTime > now.AddMonths(6))
            {
                throw new ArgumentException(
                    "Reservation cannot be made more than 6 months in advance.",
                    nameof(reservationTime));
            }
        }

        private static void ValidateCustomerId(int customerId)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException(
                    "CustomerId must be a positive integer.",
                    nameof(customerId));
            }
        }

        private static void ValidateCustomerName(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new ArgumentException(
                    "Customer name is required for anonymous reservations.",
                    nameof(customerName));
            }

            if (customerName.Length > 100)
            {
                throw new ArgumentException(
                    "Customer name cannot exceed 100 characters.",
                    nameof(customerName));
            }
        }

        private static void ValidateCustomerPhone(string customerPhone)
        {
            if (string.IsNullOrWhiteSpace(customerPhone))
            {
                throw new ArgumentException(
                    "Customer phone is required for anonymous reservations.",
                    nameof(customerPhone));
            }

            if (customerPhone.Length > 20)
            {
                throw new ArgumentException(
                    "Customer phone cannot exceed 20 characters.",
                    nameof(customerPhone));
            }
        }

        private static void ValidatePartySize(int partySize)
        {
            if (partySize <= 0)
            {
                throw new ArgumentException(
                    "Party size must be a positive integer.",
                    nameof(partySize));
            }

            if (partySize > 100)
            {
                throw new ArgumentException(
                    "Party size cannot exceed 100 people.",
                    nameof(partySize));
            }
        }

        private static void ValidateNotes(string notes)
        {
            if (notes.Length > 500)
            {
                throw new ArgumentException(
                    "Notes cannot exceed 500 characters.",
                    nameof(notes));
            }
        }
    }
}
