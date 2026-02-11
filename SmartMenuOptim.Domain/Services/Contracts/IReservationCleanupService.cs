using SmartMenuOptim.Domain.Aggregates.TableAggregate;

namespace SmartMenuOptim.Domain.Services.Abstraction;

/// <summary>
/// Service contract for automated reservation cleanup operations.
/// </summary>
/// <remarks>
/// <para><strong>Domain Layer - Service Contract</strong></para>
/// 
/// This interface defines the contract for automated reservation cleanup.
/// Implementation resides in the Application layer.
/// 
/// <para><strong>Clean Architecture:</strong></para>
/// <list type="bullet">
///   <item><description>Domain layer defines the contract (interface)</description></item>
///   <item><description>Application layer provides implementation</description></item>
///   <item><description>Infrastructure layer can depend on this abstraction</description></item>
/// </list>
/// </remarks>
public interface IReservationCleanupService
{
    /// <summary>
    /// Executes the automated reservation cleanup process.
    /// </summary>
    /// <param name="pendingExpirationHours">Hours after which pending reservations expire.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cleanup result with statistics.</returns>
    Task<CleanupResult> ExecuteCleanupAsync(
        int pendingExpirationHours = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets reservation status counts for monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary with reservation counts by status.</returns>
    Task<Dictionary<ReservationStatus, int>> GetReservationStatusCountsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a reservation cleanup operation.
/// </summary>
public class CleanupResult
{
    /// <summary>
    /// Gets or sets whether the cleanup operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if cleanup failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the duration of the cleanup operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the number of expired pending reservations identified.
    /// </summary>
    public int ExpiredPendingCount { get; set; }

    /// <summary>
    /// Gets or sets the number of expired pending reservations successfully cancelled.
    /// </summary>
    public int CancelledPendingCount { get; set; }

    /// <summary>
    /// Gets or sets the number of potential no-show reservations identified.
    /// </summary>
    public int NoShowIdentifiedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of reservations successfully marked as no-show.
    /// </summary>
    public int MarkedAsNoShowCount { get; set; }

    /// <summary>
    /// Gets or sets the initial reservation statistics before cleanup.
    /// </summary>
    public Dictionary<ReservationStatus, int> InitialStatistics { get; set; } = new();

    /// <summary>
    /// Gets a summary string of the cleanup operation.
    /// </summary>
    public override string ToString()
    {
        if (!Success)
            return $"Cleanup FAILED: {ErrorMessage} (Duration: {Duration.TotalSeconds:F2}s)";

        return $"Cleanup SUCCESS: " +
               $"Cancelled {CancelledPendingCount}/{ExpiredPendingCount} expired pending, " +
               $"NoShow {MarkedAsNoShowCount}/{NoShowIdentifiedCount} confirmed " +
               $"(Duration: {Duration.TotalSeconds:F2}s)";
    }
}
