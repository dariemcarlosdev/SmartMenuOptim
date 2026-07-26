using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for managing reservation lifecycle operations.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service handles reservation lifecycle management including automatic
/// cancellation of expired pending reservations.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Reservation lifecycle rules without infrastructure dependencies</description></item>
///   <item><description><strong>Business Rules:</strong> Implements time-based cancellation policies</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Reservation, Pending, Expired, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Logging Strategy:</strong></para>
/// <para>Uses ILogger for observability while maintaining Domain purity. Microsoft.Extensions.Logging.Abstractions
/// is a pure abstraction package with no infrastructure dependencies, making it acceptable in Domain Services.</para>
/// 
/// <para><strong>Cancellation Rules:</strong></para>
/// <list type="bullet">
///   <item><description>Pending reservations older than grace period are auto-cancelled</description></item>
///   <item><description>Confirmed reservations past their time with no-show become NoShow status</description></item>
///   <item><description>Terminal states (Completed, Cancelled, NoShow) are never modified</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var service = new ReservationManagementService(logger);
/// var expiredReservations = service.IdentifyExpiredPendingReservations(allReservations, hoursThreshold: 24);
/// service.CancelExpiredReservations(expiredReservations);
/// </code>
/// </remarks>
public class ReservationManagementService
{
    private readonly ILogger<ReservationManagementService> _logger;

    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================

    /// <summary>
    /// Default hours after which a pending reservation is considered expired.
    /// </summary>
    /// <remarks>
    /// Business Rule: Pending reservations not confirmed within 24 hours are auto-cancelled.
    /// This prevents table blocking and improves table availability.
    /// </remarks>
    private const int DefaultPendingExpirationHours = 24;

    /// <summary>
    /// Grace period in minutes after reservation time before marking as no-show.
    /// </summary>
    /// <remarks>
    /// Business Rule: Customers have 15 minutes after reservation time to arrive.
    /// After this period, confirmed reservations can be marked as no-show.
    /// </remarks>
    private const int NoShowGracePeriodMinutes = 15;

    // ===================================================================
    // CONSTRUCTOR
    // ===================================================================

    /// <summary>
    /// Initializes a new instance of the ReservationManagementService with logging support.
    /// </summary>
    /// <param name="logger">Logger for tracking reservation management operations (optional for testing).</param>
    public ReservationManagementService(ILogger<ReservationManagementService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ReservationManagementService>.Instance;
    }

    // ===================================================================
    // EXPIRED PENDING RESERVATION MANAGEMENT
    // ===================================================================

    /// <summary>
    /// Identifies pending reservations that have expired based on creation time.
    /// </summary>
    /// <param name="reservations">Collection of reservations to evaluate.</param>
    /// <param name="expirationHours">Hours after creation before reservation is considered expired (default: 24).</param>
    /// <returns>List of expired pending reservations that should be cancelled.</returns>
    /// <remarks>
    /// Business Rules:
    /// - Only Pending status reservations are considered
    /// - Expiration is based on CreatedAt timestamp
    /// - Does not modify reservations, only identifies them
    /// </remarks>
    public List<Reservation> IdentifyExpiredPendingReservations(
        IEnumerable<Reservation> reservations,
        int expirationHours = DefaultPendingExpirationHours)
    {
        _logger.LogDebug(
            "Identifying expired pending reservations (threshold: {ExpirationHours} hours)",
            expirationHours);

        if (reservations == null)
        {
            _logger.LogWarning("IdentifyExpiredPendingReservations called with null reservations collection");
            return new List<Reservation>();
        }

        var expirationThreshold = DateTime.UtcNow.AddHours(-expirationHours);
        
        var expiredReservations = reservations
            .Where(r => r.Status == ReservationStatus.Pending)
            .Where(r => r.CreatedAt < expirationThreshold)
            .ToList();

        _logger.LogInformation(
            "Found {ExpiredCount} expired pending reservations (created before {Threshold})",
            expiredReservations.Count, expirationThreshold);

        return expiredReservations;
    }

    /// <summary>
    /// Cancels a collection of expired pending reservations.
    /// </summary>
    /// <param name="reservations">Reservations to cancel.</param>
    /// <returns>Number of successfully cancelled reservations.</returns>
    /// <remarks>
    /// This method applies the domain Cancel() behavior to each reservation.
    /// Caller is responsible for persisting changes to the database.
    /// </remarks>
    public int CancelExpiredReservations(IEnumerable<Reservation> reservations)
    {
        if (reservations == null)
        {
            _logger.LogWarning("CancelExpiredReservations called with null reservations collection");
            return 0;
        }

        var reservationsList = reservations.ToList();
        var successCount = 0;
        var failCount = 0;

        _logger.LogInformation(
            "Attempting to cancel {Count} expired pending reservations",
            reservationsList.Count);

        foreach (var reservation in reservationsList)
        {
            try
            {
                _logger.LogDebug(
                    "Cancelling expired reservation {ReservationId} for Table {TableId} (created: {CreatedAt})",
                    reservation.Id, reservation.TableId, reservation.CreatedAt);

                // Use domain aggregate method to ensure business rules are enforced
                reservation.Cancel();
                successCount++;

                _logger.LogInformation(
                    "Successfully cancelled expired reservation {ReservationId}",
                    reservation.Id);
            }
            catch (InvalidOperationException ex)
            {
                // This can happen if reservation state changed between identification and cancellation
                failCount++;
                _logger.LogWarning(ex,
                    "Failed to cancel reservation {ReservationId}: {Message}",
                    reservation.Id, ex.Message);
            }
        }

        _logger.LogInformation(
            "Expired reservations cancellation complete: {SuccessCount} cancelled, {FailCount} failed",
            successCount, failCount);

        return successCount;
    }

    // ===================================================================
    // NO-SHOW MANAGEMENT
    // ===================================================================

    /// <summary>
    /// Identifies confirmed reservations that are past their time and should be marked as no-show.
    /// </summary>
    /// <param name="reservations">Collection of reservations to evaluate.</param>
    /// <returns>List of reservations that should be marked as no-show.</returns>
    /// <remarks>
    /// Business Rules:
    /// - Only Confirmed status reservations are considered
    /// - Reservation time must be in the past (plus grace period)
    /// - Grace period: 15 minutes after reservation time
    /// </remarks>
    public List<Reservation> IdentifyNoShowReservations(IEnumerable<Reservation> reservations)
    {
        _logger.LogDebug("Identifying no-show reservations");

        if (reservations == null)
        {
            _logger.LogWarning("IdentifyNoShowReservations called with null reservations collection");
            return new List<Reservation>();
        }

        var noShowThreshold = DateTime.UtcNow.AddMinutes(-NoShowGracePeriodMinutes);

        var noShowReservations = reservations
            .Where(r => r.Status == ReservationStatus.Confirmed)
            .Where(r => r.ReservationTime < noShowThreshold)
            .ToList();

        _logger.LogInformation(
            "Found {NoShowCount} potential no-show reservations (reservation time before {Threshold})",
            noShowReservations.Count, noShowThreshold);

        return noShowReservations;
    }

    /// <summary>
    /// Marks confirmed reservations as no-show when customers don't arrive.
    /// </summary>
    /// <param name="reservations">Reservations to mark as no-show.</param>
    /// <returns>Number of successfully marked no-show reservations.</returns>
    /// <remarks>
    /// Caller is responsible for persisting changes to the database.
    /// </remarks>
    public int MarkReservationsAsNoShow(IEnumerable<Reservation> reservations)
    {
        if (reservations == null)
        {
            _logger.LogWarning("MarkReservationsAsNoShow called with null reservations collection");
            return 0;
        }

        var reservationsList = reservations.ToList();
        var successCount = 0;
        var failCount = 0;

        _logger.LogInformation(
            "Attempting to mark {Count} reservations as no-show",
            reservationsList.Count);

        foreach (var reservation in reservationsList)
        {
            try
            {
                _logger.LogDebug(
                    "Marking reservation {ReservationId} as no-show (reservation time: {ReservationTime})",
                    reservation.Id, reservation.ReservationTime);

                reservation.MarkNoShow();
                successCount++;

                _logger.LogInformation(
                    "Successfully marked reservation {ReservationId} as no-show",
                    reservation.Id);
            }
            catch (InvalidOperationException ex)
            {
                failCount++;
                _logger.LogWarning(ex,
                    "Failed to mark reservation {ReservationId} as no-show: {Message}",
                    reservation.Id, ex.Message);
            }
        }

        _logger.LogInformation(
            "No-show marking complete: {SuccessCount} marked, {FailCount} failed",
            successCount, failCount);

        return successCount;
    }

    // ===================================================================
    // VALIDATION AND REPORTING
    // ===================================================================

    /// <summary>
    /// Validates reservation cleanup configuration.
    /// </summary>
    /// <param name="expirationHours">Hours threshold for pending expiration.</param>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    public bool IsValidCleanupConfiguration(int expirationHours)
    {
        if (expirationHours <= 0)
        {
            _logger.LogError(
                "Invalid cleanup configuration: expirationHours must be positive (provided: {Hours})",
                expirationHours);
            return false;
        }

        if (expirationHours > 720) // 30 days
        {
            _logger.LogWarning(
                "Cleanup configuration warning: expirationHours {Hours} exceeds recommended maximum (720 hours / 30 days)",
                expirationHours);
        }

        return true;
    }

    /// <summary>
    /// Gets statistics about reservations by status.
    /// </summary>
    /// <param name="reservations">Reservations to analyze.</param>
    /// <returns>Dictionary with status counts.</returns>
    public Dictionary<ReservationStatus, int> GetReservationStatistics(IEnumerable<Reservation> reservations)
    {
        if (reservations == null)
        {
            _logger.LogWarning("GetReservationStatistics called with null reservations collection");
            return new Dictionary<ReservationStatus, int>();
        }

        var stats = reservations
            .GroupBy(r => r.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogDebug(
            "Reservation statistics: {Stats}",
            string.Join(", ", stats.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        return stats;
    }
}
