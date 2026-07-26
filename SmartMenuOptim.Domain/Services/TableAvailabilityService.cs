using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for managing table availability and reservation logic.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service handles complex business rules for table availability, reservation scheduling,
/// and seating optimization for restaurant operations.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Availability calculations without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Table, Reservation, Order data</description></item>
///   <item><description><strong>Business Rules:</strong> Implements seating capacity and timing rules</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Table, Reservation, Capacity, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Logging Strategy:</strong></para>
/// <para>Uses ILogger for observability while maintaining Domain purity. Microsoft.Extensions.Logging.Abstractions
/// is a pure abstraction package with no infrastructure dependencies, making it acceptable in Domain Services.</para>
/// 
/// <para><strong>Availability Features:</strong></para>
/// <list type="bullet">
///   <item><description>Real-time table availability checking</description></item>
///   <item><description>Reservation conflict detection</description></item>
///   <item><description>Capacity-based table matching</description></item>
///   <item><description>Buffer time calculations (setup/cleanup)</description></item>
///   <item><description>Peak hours management</description></item>
///   <item><description>Table combination for large parties</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var availabilityService = new TableAvailabilityService(logger);
/// var isAvailable = availabilityService.IsTableAvailable(table, requestedDateTime, duration);
/// var availableTables = availabilityService.FindAvailableTables(allTables, partySize, dateTime);
/// </code>
/// </remarks>
public class TableAvailabilityService
{
    private readonly ILogger<TableAvailabilityService> _logger;

    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const int BufferTimeMinutes = 15; // Time between reservations for cleanup
    private const int DefaultReservationDurationMinutes = 120; // Default 2-hour reservation
    private const int MaxReservationDurationMinutes = 240; // Maximum 4-hour reservation
    private const int PeakHoursStartHour = 18; // 6 PM
    private const int PeakHoursEndHour = 21; // 9 PM
    private const decimal OverCapacityTolerance = 0.10m; // Allow 10% over capacity during non-peak

    // ===================================================================
    // CONSTRUCTOR
    // ===================================================================

    /// <summary>
    /// Initializes a new instance of the TableAvailabilityService with logging support.
    /// </summary>
    /// <param name="logger">Logger for tracking availability operations (optional for testing).</param>
    public TableAvailabilityService(ILogger<TableAvailabilityService>? logger = null)
    {
        _logger = logger ?? NullLogger<TableAvailabilityService>.Instance; // Use NullLogger if none provided
    }
    
    /// <summary>
    /// Represents table availability status and details.
    /// </summary>
    public class AvailabilityStatus
    {
        public bool IsAvailable { get; set; }
        public Table? Table { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime? NextAvailableTime { get; set; }
        public List<string> ConflictingReservations { get; set; } = new();
    }

    /// <summary>
    /// Checks if a specific table is available for a given time period.
    /// </summary>
    /// <param name="table">The table to check availability for.</param>
    /// <param name="requestedDateTime">Requested start date/time.</param>
    /// <param name="durationMinutes">Expected duration in minutes.</param>
    /// <param name="existingReservations">Current reservations for this table.</param>
    /// <returns>Availability status with details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when table is null.</exception>
    public AvailabilityStatus IsTableAvailable(
        Table table,
        DateTime requestedDateTime,
        int durationMinutes,
        IEnumerable<Reservation>? existingReservations = null)
    {
        _logger.LogDebug(
            "Checking availability for Table {TableId} ({TableNumber}) at {RequestedDateTime} for {Duration} minutes",
            table?.Id, table?.TableNumber, requestedDateTime, durationMinutes);

        if (table == null)
        {
            _logger.LogError("Table availability check failed: table parameter is null");
            throw new ArgumentNullException(nameof(table));
        }

        if (durationMinutes <= 0)
        {
            _logger.LogWarning("Invalid duration requested: {Duration} minutes for Table {TableId}", 
                durationMinutes, table.Id);
            throw new ArgumentException("Duration must be greater than zero", nameof(durationMinutes));
        }

        // Check if table is currently active
        if (!table.IsActive)
        {
            _logger.LogInformation(
                "Table {TableId} ({TableNumber}) is not active - unavailable for reservations",
                table.Id, table.TableNumber);

            return new AvailabilityStatus
            {
                IsAvailable = false,
                Table = table,
                Reason = "Table is not currently active or available for reservations"
            };
        }

        var requestedEndTime = requestedDateTime.AddMinutes(durationMinutes + BufferTimeMinutes);
        var reservationsList = existingReservations?.ToList() ?? new List<Reservation>();

        _logger.LogDebug(
            "Analyzing {ReservationCount} existing reservations for Table {TableId}",
            reservationsList.Count, table.Id);

        // Check for reservation conflicts
        var conflicts = reservationsList
            .Where(r => r.TableId == table.Id && r.Status != ReservationStatus.Cancelled)
            .Where(r =>
            {
                var reservationStart = r.ReservationTime;
                var reservationEnd = r.ReservationTime.AddMinutes(DefaultReservationDurationMinutes + BufferTimeMinutes);

                // Check for time overlap
                return (requestedDateTime < reservationEnd && requestedEndTime > reservationStart);
            })
            .ToList();

        if (conflicts.Any())
        {
            // Find next available time after the last conflict
            var lastConflictEnd = conflicts
                .Max(r => r.ReservationTime.AddMinutes(DefaultReservationDurationMinutes + BufferTimeMinutes));

            _logger.LogInformation(
                "Table {TableId} ({TableNumber}) has {ConflictCount} conflicting reservation(s). Next available: {NextAvailable}",
                table.Id, table.TableNumber, conflicts.Count, lastConflictEnd);

            return new AvailabilityStatus
            {
                IsAvailable = false,
                Table = table,
                Reason = $"Table has {conflicts.Count} conflicting reservation(s)",
                NextAvailableTime = lastConflictEnd,
                ConflictingReservations = conflicts.Select(r => r.Id.ToString()).ToList()
            };
        }

        _logger.LogInformation(
            "Table {TableId} ({TableNumber}) is AVAILABLE for {RequestedDateTime} ({Duration} minutes)",
            table.Id, table.TableNumber, requestedDateTime, durationMinutes);

        return new AvailabilityStatus
        {
            IsAvailable = true,
            Table = table,
            Reason = "Table is available for the requested time period"
        };
    }

    /// <summary>
    /// Finds all available tables for a given party size and time.
    /// </summary>
    /// <param name="allTables">All restaurant tables.</param>
    /// <param name="partySize">Number of guests.</param>
    /// <param name="requestedDateTime">Requested date/time.</param>
    /// <param name="durationMinutes">Expected duration (default: 120 minutes).</param>
    /// <param name="existingReservations">Current reservations.</param>
    /// <returns>List of available tables suitable for the party size.</returns>
    public List<Table> FindAvailableTables(
        IEnumerable<Table> allTables,
        int partySize,
        DateTime requestedDateTime,
        int durationMinutes = DefaultReservationDurationMinutes,
        IEnumerable<Reservation>? existingReservations = null)
    {
        _logger.LogInformation(
            "Searching for available tables: PartySize={PartySize}, DateTime={RequestedDateTime}, Duration={Duration}min",
            partySize, requestedDateTime, durationMinutes);

        if (allTables == null)
        {
            _logger.LogError("FindAvailableTables failed: allTables parameter is null");
            throw new ArgumentNullException(nameof(allTables));
        }

        if (partySize <= 0)
        {
            _logger.LogWarning("Invalid party size requested: {PartySize}", partySize);
            throw new ArgumentException("Party size must be greater than zero", nameof(partySize));
        }

        var tablesList = allTables.ToList();
        var reservationsList = existingReservations?.ToList() ?? new List<Reservation>();
        var isPeakHours = IsPeakHours(requestedDateTime);

        _logger.LogDebug(
            "Analyzing {TableCount} tables and {ReservationCount} reservations. Peak hours: {IsPeakHours}",
            tablesList.Count, reservationsList.Count, isPeakHours);

        var availableTables = new List<Table>();

        foreach (var table in tablesList)
        {
            // Check capacity with tolerance during non-peak hours
            var effectiveCapacity = isPeakHours
                ? table.Capacity
                : (int)(table.Capacity * (1 + OverCapacityTolerance));

            if (partySize > effectiveCapacity)
            {
                _logger.LogDebug(
                    "Table {TableId} ({TableNumber}) excluded: capacity {Capacity} insufficient for party of {PartySize}",
                    table.Id, table.TableNumber, effectiveCapacity, partySize);
                continue;
            }

            // Check availability
            var availability = IsTableAvailable(table, requestedDateTime, durationMinutes, reservationsList);
            
            if (availability.IsAvailable)
            {
                availableTables.Add(table);
            }
        }

        // Sort by capacity (prefer tables closest to party size)
        var sortedTables = availableTables
            .OrderBy(t => Math.Abs(t.Capacity - partySize))
            .ToList();

        _logger.LogInformation(
            "Found {AvailableCount} available tables for party of {PartySize} at {RequestedDateTime}",
            sortedTables.Count, partySize, requestedDateTime);

        return sortedTables;
    }

    /// <summary>
    /// Determines the optimal table for a party based on size and preferences.
    /// </summary>
    /// <param name="availableTables">List of available tables.</param>
    /// <param name="partySize">Number of guests.</param>
    /// <param name="preferredTableNumber">Preferred table number (optional).</param>
    /// <returns>The most suitable table, or null if none available.</returns>
    public Table? SelectOptimalTable(
        IEnumerable<Table> availableTables,
        int partySize,
        string? preferredTableNumber = null)
    {
        _logger.LogDebug(
            "Selecting optimal table for party of {PartySize}. Preferred: {PreferredTable}",
            partySize, preferredTableNumber ?? "None");

        if (availableTables == null)
        {
            _logger.LogError("SelectOptimalTable failed: availableTables parameter is null");
            throw new ArgumentNullException(nameof(availableTables));
        }

        var tablesList = availableTables.ToList();
        
        if (!tablesList.Any())
        {
            _logger.LogWarning("No available tables to select from for party of {PartySize}", partySize);
            return null;
        }

        // If table number preference is specified, try to match it first
        if (!string.IsNullOrWhiteSpace(preferredTableNumber))
        {
            var preferredTable = tablesList
                .Where(t => t.TableNumber?.Equals(preferredTableNumber, StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(t => Math.Abs(t.Capacity - partySize))
                .FirstOrDefault();

            if (preferredTable != null)
            {
                _logger.LogInformation(
                    "Optimal table selected: Preferred Table {TableNumber} (ID: {TableId}, Capacity: {Capacity}) for party of {PartySize}",
                    preferredTable.TableNumber, preferredTable.Id, preferredTable.Capacity, partySize);
                return preferredTable;
            }

            _logger.LogWarning(
                "Preferred table {PreferredTable} not available. Selecting alternative.",
                preferredTableNumber);
        }

        // Otherwise, select table with capacity closest to party size
        var optimalTable = tablesList
            .OrderBy(t => Math.Abs(t.Capacity - partySize))
            .FirstOrDefault();

        if (optimalTable != null)
        {
            _logger.LogInformation(
                "Optimal table selected: Table {TableNumber} (ID: {TableId}, Capacity: {Capacity}) for party of {PartySize}",
                optimalTable.TableNumber, optimalTable.Id, optimalTable.Capacity, partySize);
        }

        return optimalTable;
    }

    /// <summary>
    /// Checks if multiple tables can be combined for a large party.
    /// </summary>
    /// <param name="allTables">All available tables.</param>
    /// <param name="partySize">Large party size.</param>
    /// <param name="requestedDateTime">Requested date/time.</param>
    /// <param name="existingReservations">Current reservations.</param>
    /// <returns>List of table combinations that can accommodate the party.</returns>
    public List<List<Table>> FindTableCombinations(
        IEnumerable<Table> allTables,
        int partySize,
        DateTime requestedDateTime,
        IEnumerable<Reservation>? existingReservations = null)
    {
        _logger.LogInformation(
            "Finding table combinations for large party of {PartySize} at {RequestedDateTime}",
            partySize, requestedDateTime);

        if (allTables == null)
        {
            _logger.LogError("FindTableCombinations failed: allTables parameter is null");
            throw new ArgumentNullException(nameof(allTables));
        }

        var availableTables = FindAvailableTables(
            allTables,
            1, // Find all available tables regardless of size
            requestedDateTime,
            DefaultReservationDurationMinutes,
            existingReservations);

        _logger.LogDebug(
            "Analyzing combinations from {AvailableCount} available tables",
            availableTables.Count);

        var combinations = new List<List<Table>>();

        // Try combinations of 2 tables
        for (int i = 0; i < availableTables.Count; i++)
        {
            for (int j = i + 1; j < availableTables.Count; j++)
            {
                var combinedCapacity = availableTables[i].Capacity + availableTables[j].Capacity;
                if (combinedCapacity >= partySize)
                {
                    combinations.Add(new List<Table> { availableTables[i], availableTables[j] });
                    _logger.LogDebug(
                        "2-table combination found: Tables {Table1} + {Table2} = {Capacity} seats",
                        availableTables[i].TableNumber, availableTables[j].TableNumber, combinedCapacity);
                }
            }
        }

        // Try combinations of 3 tables for very large parties
        if (!combinations.Any() && partySize > 20)
        {
            _logger.LogDebug("No 2-table combinations found. Trying 3-table combinations for party of {PartySize}", partySize);

            for (int i = 0; i < availableTables.Count; i++)
            {
                for (int j = i + 1; j < availableTables.Count; j++)
                {
                    for (int k = j + 1; k < availableTables.Count; k++)
                    {
                        var combinedCapacity = availableTables[i].Capacity +
                                             availableTables[j].Capacity +
                                             availableTables[k].Capacity;
                        
                        if (combinedCapacity >= partySize)
                        {
                            combinations.Add(new List<Table>
                            {
                                availableTables[i],
                                availableTables[j],
                                availableTables[k]
                            });
                            _logger.LogDebug(
                                "3-table combination found: Tables {Table1} + {Table2} + {Table3} = {Capacity} seats",
                                availableTables[i].TableNumber, availableTables[j].TableNumber, 
                                availableTables[k].TableNumber, combinedCapacity);
                        }
                    }
                }
            }
        }

        _logger.LogInformation(
            "Found {CombinationCount} table combination(s) for party of {PartySize}",
            combinations.Count, partySize);

        return combinations;
    }

    /// <summary>
    /// Calculates the next available time slot for a table.
    /// </summary>
    /// <param name="table">The table to check.</param>
    /// <param name="fromDateTime">Start checking from this date/time.</param>
    /// <param name="existingReservations">Current reservations.</param>
    /// <returns>The next available date/time for the table.</returns>
    public DateTime FindNextAvailableSlot(
        Table table,
        DateTime fromDateTime,
        IEnumerable<Reservation>? existingReservations = null)
    {
        _logger.LogDebug(
            "Finding next available slot for Table {TableId} ({TableNumber}) from {FromDateTime}",
            table?.Id, table?.TableNumber, fromDateTime);

        if (table == null)
        {
            _logger.LogError("FindNextAvailableSlot failed: table parameter is null");
            throw new ArgumentNullException(nameof(table));
        }

        var reservationsList = existingReservations?
            .Where(r => r.TableId == table.Id)
            .OrderBy(r => r.ReservationTime)
            .ToList() ?? new List<Reservation>();

        _logger.LogDebug(
            "Analyzing {ReservationCount} future reservations for Table {TableId}",
            reservationsList.Count, table.Id);

        var checkTime = fromDateTime;

        foreach (var reservation in reservationsList)
        {
            var reservationEnd = reservation.ReservationTime
                .AddMinutes(DefaultReservationDurationMinutes + BufferTimeMinutes);

            if (checkTime < reservationEnd)
            {
                checkTime = reservationEnd;
            }
        }

        _logger.LogInformation(
            "Next available slot for Table {TableId} ({TableNumber}): {NextAvailableTime}",
            table.Id, table.TableNumber, checkTime);

        return checkTime;
    }

    /// <summary>
    /// Determines if the specified time falls within peak dining hours.
    /// </summary>
    /// <param name="dateTime">The date/time to check.</param>
    /// <returns>True if it's peak hours, false otherwise.</returns>
    public bool IsPeakHours(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        var isPeak = hour >= PeakHoursStartHour && hour < PeakHoursEndHour;

        _logger.LogDebug(
            "Peak hours check for {DateTime}: {IsPeakHours} (Peak: {StartHour}-{EndHour})",
            dateTime, isPeak, PeakHoursStartHour, PeakHoursEndHour);

        return isPeak;
    }

    /// <summary>
    /// Calculates total available seating capacity at a given time.
    /// </summary>
    /// <param name="allTables">All restaurant tables.</param>
    /// <param name="requestedDateTime">Date/time to check.</param>
    /// <param name="existingReservations">Current reservations.</param>
    /// <returns>Total number of available seats.</returns>
    public int CalculateAvailableCapacity(
        IEnumerable<Table> allTables,
        DateTime requestedDateTime,
        IEnumerable<Reservation>? existingReservations = null)
    {
        _logger.LogDebug(
            "Calculating total available capacity for {RequestedDateTime}",
            requestedDateTime);

        var availableTables = FindAvailableTables(
            allTables,
            1, // Find all available tables
            requestedDateTime,
            DefaultReservationDurationMinutes,
            existingReservations);

        var totalCapacity = availableTables.Sum(t => t.Capacity);

        _logger.LogInformation(
            "Total available capacity at {RequestedDateTime}: {TotalCapacity} seats across {TableCount} tables",
            requestedDateTime, totalCapacity, availableTables.Count);

        return totalCapacity;
    }

    /// <summary>
    /// Validates if a reservation duration is within acceptable limits.
    /// </summary>
    /// <param name="durationMinutes">Requested duration in minutes.</param>
    /// <returns>True if duration is valid, false otherwise.</returns>
    public bool IsValidReservationDuration(int durationMinutes)
    {
        var isValid = durationMinutes > 0 && durationMinutes <= MaxReservationDurationMinutes;

        if (!isValid)
        {
            _logger.LogWarning(
                "Invalid reservation duration requested: {Duration} minutes (Max: {MaxDuration})",
                durationMinutes, MaxReservationDurationMinutes);
        }

        return isValid;
    }
}
