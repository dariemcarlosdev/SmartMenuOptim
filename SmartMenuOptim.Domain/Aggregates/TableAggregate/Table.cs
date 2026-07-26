using SmartMenuOptim.Domain.Aggregates.TableAggregate.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartMenuOptim.Domain.Exceptions;

namespace SmartMenuOptim.Domain.Aggregates.TableAggregate;

/// <summary>
/// Represents a physical table aggregate root managing seating capacity, status, and reservation lifecycle for a restaurant tenant.
/// </summary>
/// <remarks>
/// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
/// <para>This class implements a full DDD aggregate root pattern with child entities (Reservation) and complex state management.
/// It serves as the consistency boundary for all table-related operations including status transitions and reservation booking.</para>
/// 
/// <para><strong>Tier 1 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods</description></item>
///   <item><description><strong>Child Entity Management:</strong> Manages Reservation child entities through encapsulated collection with controlled access</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to table and reservations happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for status transitions, capacity checks, reservation management, availability validation</description></item>
///   <item><description><strong>Invariant Protection:</strong> Maintains invariants (valid capacity range, proper status transitions, reservation consistency, no double-booking)</description></item>
///   <item><description><strong>Collection Encapsulation:</strong> Private backing field (_reservations) with read-only public access (Reservations property)</description></item>
///   <item><description><strong>State Machine:</strong> Implements table status state machine (Available ↔ Occupied ↔ Reserved) with transition validation</description></item>
/// </list>
/// 
/// <para><strong>Entity Overview:</strong></para>
/// <para>A Table represents a physical seating location in a restaurant with specific capacity, unique identification (table number),
/// and dynamic status tracking. It manages reservations (time-based booking commitments), handles status transitions between
/// Available/Occupied/Reserved/OutOfService states, and enforces capacity constraints. Tables form the foundation of restaurant
/// seating management, reservation systems, and dining room capacity planning.</para>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each table is scoped to a specific
/// restaurant (RestaurantId), ensuring proper data isolation. All reservations associated with a table must belong to
/// the same restaurant. Table numbers are unique within a restaurant but can be duplicated across different tenants.</para>
/// 
/// <para><strong>Aggregate Composition:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Root Entity:</strong> Table (this class)</description></item>
///   <item><description><strong>Child Entities:</strong> Reservation collection - time-based booking commitments with customer information</description></item>
///   <item><description><strong>Referenced Entities:</strong> Customer (global, optional through reservations)</description></item>
/// </list>
/// 
/// <para><strong>Consistency Boundary:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Invariants Protected:</strong> Capacity 1-100 seats, valid table number format, no overlapping reservations, status transitions validated, reservations belong to same restaurant</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state modified only through behavioral methods (MarkOccupied, MarkAvailable, Reserve, MakeReservation, CancelReservation)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to table and child reservations saved atomically through repository</description></item>
///   <item><description><strong>Business Rules:</strong> Cannot occupy reserved table without clearing reservation first, cannot reserve occupied table, capacity must accommodate party size</description></item>
///   <item><description><strong>Child Collection:</strong> Reservations can only be created/cancelled through aggregate root methods</description></item>
/// </list>
/// 
/// <para><strong>Domain Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for logical deletion (retired tables)</description></item>
///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
///   <item><description><strong>Status State Machine:</strong> Validated transitions between Available, Occupied, Reserved, OutOfService</description></item>
///   <item><description><strong>Capacity Validation:</strong> Enforces reasonable capacity limits (1-100) and party size accommodation checks</description></item>
///   <item><description><strong>Reservation Management:</strong> Supports both registered customer and anonymous (walk-in) reservations</description></item>
/// </list>
/// 
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Reservations (One-to-Many Children):</strong> Managed exclusively through aggregate root methods</description></item>
///   <item><description><strong>Customer (Optional via Reservations):</strong> Links to global Customer entity for registered users</description></item>
///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
/// </list>
/// 
/// <para><strong>Status State Machine:</strong></para>
/// <code>
///        ┌─────────────┐
///        │  Available  │ ←─────┐
///        └──────┬──────┘       │
///               │              │
///          ┌────┴─────┐        │
///          │          │        │
///     ┌────▼───┐  ┌──▼────┐   │
///     │Reserved│  │Occupied│───┘
///     └────┬───┘  └───────┘
///          │
///          └──────────────────→ (Clear Reservation)
/// 
/// OutOfService ←→ Any State (Maintenance)
/// </code>
/// <list type="bullet">
///   <item><description><strong>Available:</strong> Table is ready for walk-in customers</description></item>
///   <item><description><strong>Occupied:</strong> Customers currently seated at the table</description></item>
///   <item><description><strong>Reserved:</strong> Table has active future reservation</description></item>
///   <item><description><strong>OutOfService:</strong> Table unavailable due to maintenance or closure</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new table
/// var table = new Table(
///     restaurantId: 123,
///     tableNumber: "A1",
///     capacity: 4,
///     description: "Window seat with garden view"
/// );
/// 
/// // Making a reservation for registered customer
/// var reservation = table.MakeReservation(
///     reservationTime: DateTime.UtcNow.AddDays(1).Date.AddHours(19), // Tomorrow at 7 PM
///     customerId: 456,
///     partySize: 4,
///     notes: "Birthday celebration - please prepare dessert"
/// );
/// // Table status automatically changed to Reserved
/// 
/// // Making a walk-in/anonymous reservation
/// var walkInReservation = table.MakeReservation(
///     reservationTime: DateTime.UtcNow.AddDays(2).Date.AddHours(20), // Day after tomorrow at 8 PM
///     customerName: "John Doe",
///     customerPhone: "+1-555-1234",
///     partySize: 2
/// );
/// 
/// // Checking if table can accommodate party size
/// if (table.CanAccommodate(6))
/// {
///     // Table capacity is sufficient for party of 6
/// }
/// 
/// // Marking table as reserved (manual status change)
/// table.Reserve();
/// 
/// // When guests arrive, mark table as occupied
/// table.MarkOccupied();
/// 
/// // When guests leave, mark table as available
/// table.MarkAvailable();
/// 
/// // Checking table availability
/// if (table.IsAvailable())
/// {
///     Console.WriteLine($"Table {table.TableNumber} is available");
/// }
/// 
/// // Checking for active reservations
/// if (table.HasActiveReservations())
/// {
///     var nextReservation = table.Reservations
///         .Where(r => r.ReservationTime > DateTime.UtcNow)
///         .OrderBy(r => r.ReservationTime)
///         .FirstOrDefault();
///     Console.WriteLine($"Next reservation at {nextReservation?.ReservationTime}");
/// }
/// 
/// // Cancelling a reservation
/// table.CancelReservation(reservationId: reservation.Id);
/// 
/// // Updating table capacity
/// table.UpdateCapacity(newCapacity: 6); // Expanded to 6 seats
/// 
/// // Updating table information
/// table.UpdateBasicInfo(
///     tableNumber: "A1-Premium",
///     description: "Premium window seat with garden view and privacy"
/// );
/// 
/// // Taking table out of service for maintenance
/// table.MarkOutOfService();
/// 
/// // Returning table to service
/// table.MarkAvailable();
/// 
/// // Validating tenant consistency after loading from database
/// table.ValidateTenantConsistency();
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through repository pattern. Private setters and the _reservations collection are accessible to
/// EF Core through reflection-based field mapping in entity configuration. Child Reservation entities are automatically
/// persisted through cascade operations. Reservations have internal constructors, ensuring they can only be created through
/// the Table aggregate root.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Aggregate Boundary:</strong> Table and Reservations must be loaded and saved together as a unit</description></item>
///   <item><description><strong>Capacity Range:</strong> Validated to be between 1 and 100 seats (reasonable restaurant table sizes)</description></item>
///   <item><description><strong>Table Number Format:</strong> Alphanumeric with hyphens, unique within restaurant</description></item>
///   <item><description><strong>Status Transitions:</strong> Enforces valid state machine transitions with business rule validation</description></item>
///   <item><description><strong>Reservation Timing:</strong> Must be future date (15-minute grace period), max 6 months in advance</description></item>
///   <item><description><strong>No Double-Booking:</strong> Cannot create overlapping reservations for same table</description></item>
///   <item><description><strong>Party Size:</strong> Optional in reservation but validated against table capacity if provided</description></item>
///   <item><description><strong>Tenant Isolation:</strong> All reservations must belong to same restaurant as table</description></item>
/// </list>
/// 
/// <para><strong>Indexing Strategy:</strong></para>
/// <para>Database indexes for efficient querying are defined in AppDbContext.OnModelCreating:</para>
/// <list type="bullet">
///   <item><description>IX_Tables_Restaurant_TableNumber: Unique index ensuring table number uniqueness per restaurant</description></item>
///   <item><description>IX_Tables_Restaurant_Status: For filtering available/occupied tables</description></item>
///   <item><description>IX_Tables_Capacity: For finding tables matching party size requirements</description></item>
///   <item><description>IX_Reservations_Table_ReservationTime: For checking reservation conflicts</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Reservation Booking:</strong> Customers book tables for future dining</description></item>
///   <item><description><strong>Seating Management:</strong> Host assigns walk-in customers to available tables</description></item>
///   <item><description><strong>Capacity Planning:</strong> Determine if restaurant can accommodate party size</description></item>
///   <item><description><strong>Table Status Tracking:</strong> Monitor which tables are available, occupied, or reserved</description></item>
///   <item><description><strong>Reservation Calendar:</strong> View upcoming reservations by date and time</description></item>
///   <item><description><strong>Customer Service:</strong> Manage reservation modifications and cancellations</description></item>
///   <item><description><strong>Layout Management:</strong> Track table configurations and capacity changes</description></item>
///   <item><description><strong>Maintenance Scheduling:</strong> Take tables out of service for cleaning or repairs</description></item>
/// </list>
/// </remarks>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new table
/// var table = new Table(
///     restaurantId: 123,
///     tableNumber: "A1",
///     capacity: 4,
///     description: "Window seat with garden view"
/// );
/// 
/// // Making a reservation (registered customer)
/// var reservation = table.MakeReservation(
///     reservationTime: DateTime.UtcNow.AddDays(1).Date.AddHours(19),
///     customerId: 456,
///     partySize: 4,
///     notes: "Birthday celebration"
/// );
/// 
/// // Making a reservation (walk-in/anonymous)
/// var walkInReservation = table.MakeReservation(
///     reservationTime: DateTime.UtcNow.AddDays(2).Date.AddHours(20),
///     customerName: "John Doe",
///     customerPhone: "+1-555-1234",
///     partySize: 2
/// );
/// 
/// // Checking capacity
/// if (table.CanAccommodate(6))
/// {
///     // Party of 6 can be seated
/// }
/// 
/// // Reserving the table (changes status)
/// table.Reserve();
/// 
/// // Marking table as occupied when guests arrive
/// table.MarkOccupied();
/// 
/// // Checking if table is available
/// if (table.IsAvailable())
/// {
///     // Table can be assigned
/// }
/// 
/// // Cancelling a reservation
/// table.CancelReservation(reservationId);
/// 
/// // Updating table details
/// table.UpdateDetails(
///     tableNumber: "A2",
///     capacity: 6,
///     description: "Corner booth with privacy"
/// );
/// 
/// // Releasing table when guests leave
/// table.MarkAvailable();
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through a repository pattern. Private setters and the _reservations collection are accessible
/// to EF Core through reflection-based field mapping in the entity configuration.</para>
/// 
/// <para><strong>Repository Access:</strong></para>
/// <para>Should be accessed only through ITableRepository. Direct instantiation should be limited to factories or
/// application services. Changes should be persisted through Unit of Work pattern to maintain transactional integrity
/// across the aggregate boundary, ensuring that all table and reservation changes are committed atomically.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Capacity Range:</strong> Tables must have capacity between 1 and 100 seats</description></item>
///   <item><description><strong>Table Number Format:</strong> Alphanumeric with hyphens only, max 20 characters</description></item>
///   <item><description><strong>Status Transitions:</strong> Cannot occupy a reserved table - must unreserve first</description></item>
///   <item><description><strong>Reservation Management:</strong> Aggregate validates overlapping reservations, ensures tenant consistency</description></item>
///   <item><description><strong>Tenant Isolation:</strong> All reservations must belong to the same restaurant as the table</description></item>
///   <item><description><strong>Immutable Reservations:</strong> Reservations cannot be modified after creation, only cancelled</description></item>
/// </list>
/// </remarks>
public class Table : TenantEntityBase
{
    // === Private Setters (Encapsulated State) ===
    /// <summary>
    /// Human-friendly table identifier (e.g., "1", "A1", "VIP-1").
    /// Must be alphanumeric with optional hyphens, maximum 20 characters.
    /// </summary>
    public string TableNumber { get; private set; }
    
    /// <summary>
    /// Number of seats at the table. Must be between 1 and 100.
    /// </summary>
    public int Capacity { get; private set; }
    
    /// <summary>
    /// Current status of the table (Available, Occupied, or Reserved).
    /// Controlled through MarkAvailable(), MarkOccupied(), and Reserve() methods.
    /// </summary>
    public TableStatus Status { get; private set; }
    
    /// <summary>
    /// Optional description providing details about the table location, features, or special characteristics.
    /// Maximum 500 characters.
    /// </summary>
    public string? Description { get; private set; }
    
    // === Encapsulated Collections ===
    private readonly List<Reservation> _reservations = new();
    
    /// <summary>
    /// Read-only collection of reservations associated with this table.
    /// </summary>
    /// <remarks>
    /// This is a fully encapsulated collection of child Reservation entities.
    /// Reservations can only be added through MakeReservation() and removed through CancelReservation().
    /// External code has read-only access to maintain aggregate boundary.
    /// </remarks>
    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();
    
    // === Constructors ===
    /// <summary>
    /// Protected parameterless constructor for Entity Framework Core.
    /// </summary>
    protected Table() { /* EF Core */ }
    
    /// <summary>
    /// Creates a new table with specified number, capacity, and optional description.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier this table belongs to.</param>
    /// <param name="tableNumber">Human-friendly table identifier (required, alphanumeric with hyphens, max 20 characters).</param>
    /// <param name="capacity">Number of seats at the table (must be between 1 and 100).</param>
    /// <param name="description">Optional description providing table details (max 500 characters).</param>
    /// <exception cref="ArgumentException">Thrown when table number is invalid, capacity is out of range, or description exceeds maximum length.</exception>
    public Table(int restaurantId, string tableNumber, int capacity, string? description = null)
    {
        // ---------------------------------------------------------------
        // PARAMETER GUARD CLAUSES — ArgumentException
        //
        // Guard clauses validate constructor preconditions (caller contract),
        // NOT domain business rules.
        //
        // • ArgumentException → 400 Bad Request (programming error)
        // • TableDomainException → 422 Unprocessable Entity
        //   (valid input, but violates a table state rule)
        // • ReservationDomainException → 422 Unprocessable Entity
        //   (valid input, but violates a reservation booking rule)
        //
        // EXAMPLES:
        // • tableNumber = null      → ArgumentException  (caller bug)
        // • capacity = -1           → ArgumentOutOfRangeException (caller bug)
        // • MarkOccupied() Reserved → TableDomainException (business rule)
        // • MakeReservation() conflict → ReservationDomainException (business rule)
        // ---------------------------------------------------------------

        ArgumentException.ThrowIfNullOrWhiteSpace(tableNumber, nameof(tableNumber));

        if (tableNumber.Length > 20)
            throw new ArgumentException("Table number cannot exceed 20 characters.", nameof(tableNumber));

        if (!System.Text.RegularExpressions.Regex.IsMatch(tableNumber, @"^[a-zA-Z0-9\-]+$"))
            throw new ArgumentException("Table number can only contain letters, numbers, and hyphens.", nameof(tableNumber));

        if (capacity < 1 || capacity > 100)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be between 1 and 100.");

        if (!string.IsNullOrEmpty(description) && description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        
        RestaurantId = restaurantId;
        TableNumber = tableNumber;
        Capacity = capacity;
        Status = TableStatus.Available;
        Description = description;
    }
    
    // === Behavioral Methods ===
    /// <summary>
    /// Marks the table as occupied, indicating guests are currently seated.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to occupy a reserved table.</exception>
    /// <remarks>
    /// A table can only be marked as occupied if it's currently Available.
    /// If the table is Reserved, it must be unreserved first (by marking it available) before occupying.
    /// This ensures proper workflow: Reserve → (Guest arrives) → Mark Available → Mark Occupied.
    /// </remarks>
    public void MarkOccupied()
    {
        if (Status == TableStatus.Reserved)
            throw new TableDomainException("Cannot occupy a reserved table. Mark it as available first to cancel the reservation.");

        Status = TableStatus.Occupied;
    }
    
    /// <summary>
    /// Marks the table as available for immediate seating or reservation.
    /// </summary>
    /// <remarks>
    /// This method can be called from any status:
    /// - From Occupied: Releases the table when guests leave
    /// - From Reserved: Cancels the reservation
    /// - From Available: No-op, but allowed for idempotency
    /// </remarks>
    public void MarkAvailable()
    {
        Status = TableStatus.Available;
    }
    
    /// <summary>
    /// Reserves the table for a future guest, preventing it from being occupied immediately.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to reserve an occupied table.</exception>
    /// <remarks>
    /// A table can only be reserved if it's currently Available.
    /// Attempting to reserve an Occupied table will throw an exception.
    /// </remarks>
    public void Reserve()
    {
        if (Status == TableStatus.Occupied)
            throw new TableDomainException("Cannot reserve an occupied table. Wait until it becomes available.");

        Status = TableStatus.Reserved;
    }
    
    /// <summary>
    /// Checks if the table is currently available for immediate seating.
    /// </summary>
    /// <returns>True if the table status is Available; otherwise, false.</returns>
    public bool IsAvailable()
    {
        return Status == TableStatus.Available;
    }
    
    /// <summary>
    /// Checks if the table can accommodate a party of the specified size.
    /// </summary>
    /// <param name="partySize">The number of guests in the party.</param>
    /// <returns>True if the table capacity is greater than or equal to the party size; otherwise, false.</returns>
    public bool CanAccommodate(int partySize)
    {
        return Capacity >= partySize;
    }
    
    /// <summary>
    /// Checks if the table has any active reservations.
    /// </summary>
    /// <returns>True if the table has one or more reservations; otherwise, false.</returns>
    /// <remarks>
    /// Use this method to determine if the table has booking commitments.
    /// Active reservations are those scheduled for future times.
    /// </remarks>
    public bool HasActiveReservations()
    {
        return _reservations.Count > 0;
    }
    
    /// <summary>
    /// Checks if the table is available at a specific time.
    /// </summary>
    /// <param name="requestedTime">The time to check availability for.</param>
    /// <param name="reservationDurationHours">Duration of the reservation in hours (default: 2).</param>
    /// <returns>True if no overlapping reservations exist; otherwise, false.</returns>
    /// <remarks>
    /// Standard reservation duration is 2 hours.
    /// Checks for overlapping time windows with existing reservations.
    /// </remarks>
    public bool IsAvailableAt(DateTime requestedTime, int reservationDurationHours = 2)
    {
        var requestedEnd = requestedTime.AddHours(reservationDurationHours);
        
        return !_reservations.Any(r => 
            r.ReservationTime < requestedEnd && 
            r.ReservationTime.AddHours(reservationDurationHours) > requestedTime);
    }
    
    /// <summary>
    /// Makes a reservation for a registered customer.
    /// This method is a factory for creating Reservation child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <param name="reservationTime">The date/time for the reservation.</param>
    /// <param name="customerId">The registered customer ID.</param>
    /// <param name="partySize">Optional number of people in the party.</param>
    /// <param name="notes">Optional special requests or notes.</param>
    /// <param name="reservationDurationHours">Duration of the reservation in hours (default: 2).</param>
    /// <returns>The created Reservation entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when table is not available at the requested time.</exception>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add Reservation child entities. Direct manipulation of the collection
    /// is prevented through encapsulation.
    /// 
    /// This method:
    /// - Validates table availability at the requested time
    /// - Creates a new Reservation child entity through internal constructor
    /// - Adds the reservation to the encapsulated collection
    /// - Maintains aggregate consistency
    /// </remarks>
    public Reservation MakeReservation(
        DateTime reservationTime,
        int customerId,
        int? partySize = null,
        string? notes = null,
        int reservationDurationHours = 2)
    {
        // Validate table is available
        if (!IsAvailableAt(reservationTime, reservationDurationHours))
        {
            //booking reservation at a time that conflicts with an existing reservation violates a business rule, so we throw a domain exception
            throw new ReservationDomainException(
                $"Table {TableNumber} is not available at {reservationTime}. Another reservation already exists during this time.");
        }

        // Create reservation through internal constructor
        var reservation = new Reservation(
            tableId: Id,
            restaurantId: RestaurantId,
            reservationTime: reservationTime,
            customerId: customerId,
            partySize: partySize,
            notes: notes
        );

        _reservations.Add(reservation);
        return reservation;
    }

    /// <summary>
    /// Makes a reservation for a walk-in/anonymous customer.
    /// This method is a factory for creating Reservation child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <param name="reservationTime">The date/time for the reservation.</param>
    /// <param name="customerName">Name of the customer.</param>
    /// <param name="customerPhone">Phone number for contact.</param>
    /// <param name="partySize">Optional number of people in the party.</param>
    /// <param name="notes">Optional special requests or notes.</param>
    /// <param name="reservationDurationHours">Duration of the reservation in hours (default: 2).</param>
    /// <returns>The created Reservation entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when table is not available at the requested time.</exception>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add Reservation child entities for anonymous customers. Direct manipulation of the collection
    /// is prevented through encapsulation.
    /// 
    /// This method:
    /// - Validates table availability at the requested time
    /// - Creates a new anonymous Reservation child entity through internal constructor
    /// - Adds the reservation to the encapsulated collection
    /// - Maintains aggregate consistency
    /// </remarks>
    public Reservation MakeReservation(
        DateTime reservationTime,
        string customerName,
        string customerPhone,
        int? partySize = null,
        string? notes = null,
        int reservationDurationHours = 2)
    {
        // Validate table is available
        if (!IsAvailableAt(reservationTime, reservationDurationHours))
        {
            throw new ReservationDomainException(
                $"Table {TableNumber} is not available at {reservationTime}. Another reservation already exists during this time.");
        }

        // Create anonymous reservation through internal constructor
        var reservation = new Reservation(
            tableId: Id,
            restaurantId: RestaurantId,
            reservationTime: reservationTime,
            customerName: customerName,
            customerPhone: customerPhone,
            partySize: partySize,
            notes: notes
        );
        
        _reservations.Add(reservation);
        return reservation;
    }
    
    /// <summary>
    /// Cancels a reservation.
    /// </summary>
    /// <param name="reservationId">The ID of the reservation to cancel.</param>
    /// <exception cref="InvalidOperationException">Thrown when reservation is not found.</exception>
    /// <remarks>
    /// This method:
    /// - Finds the reservation in the encapsulated collection
    /// - Removes it from the table's reservations
    /// - Maintains aggregate consistency
    /// Note: This does not delete the customer record, only the reservation.
    /// </remarks>
    public void CancelReservation(int reservationId)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Id == reservationId);
        
        if (reservation == null)
        {
            // Attempting to cancel a non-existent reservation violates a business rule, so we throw a domain exception
            throw new ReservationDomainException(
                $"Reservation with ID {reservationId} not found for table {TableNumber}.");
        }
        
        _reservations.Remove(reservation);
    }
    
    /// <summary>
    /// Updates the table details including number, capacity, and description.
    /// </summary>
    /// <param name="tableNumber">New table number (required, alphanumeric with hyphens, max 20 characters).</param>
    /// <param name="capacity">New capacity (must be between 1 and 100).</param>
    /// <param name="description">New description (max 500 characters, or null to clear).</param>
    /// <exception cref="InvalidOperationException">Thrown when attempting to update an occupied table.</exception>
    /// <exception cref="ArgumentException">Thrown when validation rules are violated.</exception>
    /// <remarks>
    /// Table details cannot be updated while the table is occupied to prevent confusion.
    /// The table can be updated when Available or Reserved.
    /// </remarks>
    public void UpdateDetails(string tableNumber, int capacity, string? description = null)
    {
        // Domain rule: cannot update while guests are seated.
        if (Status == TableStatus.Occupied)
            throw new TableDomainException("Cannot update table details while occupied. Wait until table is available.");

        // Guard clauses: invalid parameters are programming errors, not business rules.
        ArgumentException.ThrowIfNullOrWhiteSpace(tableNumber, nameof(tableNumber));

        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be empty if provided.", nameof(description));

        if (tableNumber.Length > 20)
            throw new ArgumentException("Table number cannot exceed 20 characters.", nameof(tableNumber));

        if (!System.Text.RegularExpressions.Regex.IsMatch(tableNumber, @"^[a-zA-Z0-9\-]+$"))
            throw new ArgumentException("Table number can only contain letters, numbers, and hyphens.", nameof(tableNumber));

        if (capacity < 1 || capacity > 100)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be between 1 and 100.");

        if (!string.IsNullOrEmpty(description) && description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        
        TableNumber = tableNumber;
        Capacity = capacity;
        Description = description;
    }
    
    /// <summary>
    /// Updates the table description.
    /// </summary>
    /// <param name="description">New description (max 500 characters, or null to clear).</param>
    /// <exception cref="ArgumentException">Thrown when description exceeds maximum length.</exception>
    /// <remarks>
    /// Description can be updated independently of other table details and even while the table is occupied.
    /// </remarks>
    public void UpdateDescription(string? description)
    {
        // Guard clause: exceeding max length is a programming error, not a business rule.
        if (!string.IsNullOrEmpty(description) && description.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(description));
        
        Description = description;
    }
    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the table maintains multi-tenant boundaries and consistency across all relationships.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - RestaurantId is valid and positive
    /// - Restaurant navigation property matches RestaurantId
    /// - All reservations belong to the same restaurant as the table
    /// - Table status and reservation data are consistent with tenant boundaries
    /// 
    /// Tenant Consistency Rules:
    /// 1. Table must belong to exactly one restaurant (RestaurantId must be positive)
    /// 2. All reservations must belong to the same restaurant as the table
    /// 3. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 4. All child entities maintain proper tenant isolation
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant table access and reservation viewing
    /// - Reservations from one restaurant appearing on another restaurant's tables
    /// - Table status manipulation across tenant boundaries
    /// - Capacity and availability data leakage between restaurants
    /// - Reporting inaccuracies in multi-tenant table management dashboards
    /// - Double-booking across different restaurant tenants
    /// 
    /// When to Call:
    /// - After loading tables with navigation properties from database
    /// - Before processing reservation requests in multi-tenant contexts
    /// - Before displaying table availability and status
    /// - In data import/migration processes to ensure data integrity
    /// - As part of scheduled data integrity audits
    /// - When validating table assignments and reservations
    /// - In admin interfaces before displaying table and reservation details
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// For tables with many reservations, validation is still efficient as it uses LINQ queries.
    /// Consider calling this during batch operations or scheduled maintenance windows for large datasets.
    /// 
    /// Design Note:
    /// As an aggregate root with child entities (Reservations), this validation ensures the entire
    /// aggregate boundary maintains tenant consistency. The table aggregate enforces that all
    /// reservations are properly scoped to the restaurant tenant.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate RestaurantId is valid
        if (RestaurantId <= 0)
        {
            throw new TableDomainException(
                $"Table has invalid RestaurantId: {RestaurantId}. " +
                $"RestaurantId must be a positive integer. " +
                $"Table: '{TableNumber}' (ID: {Id}, Capacity: {Capacity})");
        }

        // Validate Restaurant navigation property consistency
        if (Restaurant != null)
        {
            if (Restaurant.Id != RestaurantId)
            {
                throw new TableDomainException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}). " +
                    $"Table: '{TableNumber}' (ID: {Id}, Capacity: {Capacity}), " +
                    $"Restaurant: '{Restaurant.Name}' (ID: {Restaurant.Id})");
            }

            // Additional validation: Ensure restaurant is active and not deleted
            if (Restaurant.IsDeleted)
            {
                throw new TableDomainException(
                    $"Table '{TableNumber}' (ID: {Id}) is associated with a deleted restaurant " +
                    $"'{Restaurant.Name}' (ID: {Restaurant.Id}). " +
                    $"Tables cannot belong to deleted restaurants.");
            }
        }

        // Validate all reservations belong to same restaurant
        if (_reservations != null && _reservations.Any())
        {
            var inconsistentReservations = _reservations
                .Where(r => r.RestaurantId != RestaurantId)
                .Select(r => new 
                { 
                    r.Id, 
                    r.RestaurantId, 
                    r.ReservationTime, 
                    r.CustomerName,
                    r.CustomerId,
                    r.PartySize 
                })
                .ToList();

            if (inconsistentReservations.Any())
            {
                var reservationInfo = string.Join(", ", inconsistentReservations.Select(r => 
                    $"Reservation ID: {r.Id}, Customer: {r.CustomerName ?? $"ID:{r.CustomerId}"}, " +
                    $"Time: {r.ReservationTime:yyyy-MM-dd HH:mm}, Party: {r.PartySize ?? 0}, " +
                    $"RestaurantId: {r.RestaurantId}"));

                throw new TableDomainException(
                    $"Table '{TableNumber}' (ID: {Id}) contains reservations from different restaurants. " +
                    $"Table RestaurantId: {RestaurantId}, " +
                    $"Total Inconsistent Reservations: {inconsistentReservations.Count}, " +
                    $"Details: [{reservationInfo}]");
            }

            // Additional validation: Check for reservations with null/invalid data
            var invalidReservations = _reservations
                .Where(r => r.TableId != Id)
                .Select(r => new { r.Id, r.TableId })
                .ToList();

            if (invalidReservations.Any())
            {
                var invalidInfo = string.Join(", ", invalidReservations.Select(r => 
                    $"Reservation ID: {r.Id}, TableId: {r.TableId}"));

                throw new TableDomainException(
                    $"Table '{TableNumber}' (ID: {Id}) contains reservations with mismatched TableId. " +
                    $"Expected TableId: {Id}, " +
                    $"Invalid Reservations: [{invalidInfo}]");
            }
        }

        // Validate table state consistency with reservations
        if (Status == TableStatus.Reserved && !HasActiveReservations())
        {
            throw new TableDomainException(
                $"Table '{TableNumber}' (ID: {Id}) has Reserved status but no active reservations. " +
                $"Table status and reservation collection are inconsistent.");
        }
    }
    
}


/// <summary>
/// Represents the possible status states for a restaurant table.
/// </summary>
/// <remarks>
/// <para><strong>Status Definitions:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Available:</strong> Table is ready for immediate seating or can be reserved</description></item>
///   <item><description><strong>Occupied:</strong> Table is currently in use by guests</description></item>
///   <item><description><strong>Reserved:</strong> Table is reserved for future guests and cannot be occupied immediately</description></item>
/// </list>
/// 
/// <para><strong>Valid Transitions:</strong></para>
/// <list type="bullet">
///   <item><description>Available → Occupied (direct seating)</description></item>
///   <item><description>Available → Reserved (booking made)</description></item>
///   <item><description>Occupied → Available (guests leave)</description></item>
///   <item><description>Reserved → Available (reservation cancelled)</description></item>
///   <item><description>Reserved → Occupied (reserved guests arrive, must mark available first)</description></item>
/// </list>
/// </remarks>
public enum TableStatus 
{ 
    /// <summary>
    /// Table is ready for immediate seating or reservation.
    /// </summary>
    Available, 
    
    /// <summary>
    /// Table is currently occupied by guests.
    /// </summary>
    Occupied, 
    
    /// <summary>
    /// Table is reserved for future guests.
    /// </summary>
    Reserved 
}