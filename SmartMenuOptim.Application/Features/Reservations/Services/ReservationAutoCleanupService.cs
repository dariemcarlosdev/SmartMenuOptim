using SmartMenuOptim.Domain.Aggregates.TableAggregate.Specifications;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services;
using SmartMenuOptim.Domain.Services.Abstractions;
using SmartMenuOptim.Domain.Specifications;

namespace SmartMenuOptim.Application.Features.Reservations.Services;

/// <summary>
/// Application service for automatic reservation cleanup operations.
/// </summary>
/// <remarks>
/// <para><strong>Application Service - Orchestration Layer</strong></para>
/// 
/// This service orchestrates the cleanup of expired and no-show reservations
/// by coordinating between domain services and repositories.
/// 
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
///   <item><description>Fetch reservations using Domain specifications</description></item>
///   <item><description>Delegate business logic to domain services</description></item>
///   <item><description>Persist changes through generic repository</description></item>
///   <item><description>Handle transactions via Unit of Work</description></item>
///   <item><description>Log application-level operations</description></item>
/// </list>
/// 
/// <para><strong>Clean Architecture Principles:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Depends on:</strong> Domain layer (services, aggregates, specifications, repository interfaces)</description></item>
///   <item><description><strong>Uses:</strong> Specification Pattern for query logic</description></item>
///   <item><description><strong>Orchestrates:</strong> Transaction boundaries and error handling</description></item>
///   <item><description><strong>No Infrastructure Coupling:</strong> Uses abstractions only</description></item>
/// </list>
/// </remarks>
public class ReservationAutoCleanupService : IReservationCleanupService
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IUnityOfWork _unitOfWork;
    private readonly ReservationManagementService _reservationManagement;
    private readonly ILogger<ReservationAutoCleanupService> _logger;

    public ReservationAutoCleanupService(
        IRepository<Reservation> reservationRepository,
        IUnityOfWork unitOfWork,
        ReservationManagementService reservationManagement,
        ILogger<ReservationAutoCleanupService> logger)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reservationManagement = reservationManagement ?? throw new ArgumentNullException(nameof(reservationManagement));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the complete reservation cleanup process.
    /// </summary>
    /// <param name="pendingExpirationHours">Hours after which pending reservations expire (default: 24).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cleanup result with statistics.</returns>
    /// <remarks>
    /// This method performs the following cleanup operations:
    /// 1. Cancel expired pending reservations (not confirmed within threshold)
    /// 2. Mark confirmed reservations as no-show (customer didn't arrive)
    /// 
    /// All operations are performed in a transaction to ensure data consistency.
    /// </remarks>
    public async Task<CleanupResult> ExecuteCleanupAsync(
        int pendingExpirationHours = 24,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting reservation auto-cleanup (pending expiration: {Hours} hours)",
            pendingExpirationHours);

        var result = new CleanupResult();
        var startTime = DateTime.UtcNow;

        try
        {
            // Validate configuration
            if (!_reservationManagement.IsValidCleanupConfiguration(pendingExpirationHours))
            {
                _logger.LogError("Invalid cleanup configuration. Aborting cleanup operation.");
                result.Success = false;
                result.ErrorMessage = "Invalid configuration";
                return result;
            }

            // Fetch active reservations using Domain specification
            var activeReservationsSpec = new ActiveReservationsSpecification();
            var activeReservations = (await _reservationRepository.FindAsync(activeReservationsSpec)).ToList();

            _logger.LogInformation(
                "Retrieved {Count} active reservations for cleanup evaluation",
                activeReservations.Count);

            // Get statistics before cleanup
            result.InitialStatistics = _reservationManagement.GetReservationStatistics(activeReservations);

            try
            {
                // Step 1: Cancel expired pending reservations
                var expiredPending = _reservationManagement.IdentifyExpiredPendingReservations(
                    activeReservations, 
                    pendingExpirationHours);

                result.ExpiredPendingCount = expiredPending.Count;

                if (expiredPending.Any())
                {
                    result.CancelledPendingCount = _reservationManagement.CancelExpiredReservations(expiredPending);
                    _logger.LogInformation(
                        "Cancelled {Count} expired pending reservations",
                        result.CancelledPendingCount);
                }

                // Step 2: Mark confirmed reservations as no-show
                var noShowReservations = _reservationManagement.IdentifyNoShowReservations(activeReservations);
                result.NoShowIdentifiedCount = noShowReservations.Count;

                if (noShowReservations.Any())
                {
                    result.MarkedAsNoShowCount = _reservationManagement.MarkReservationsAsNoShow(noShowReservations);
                    _logger.LogInformation(
                        "Marked {Count} reservations as no-show",
                        result.MarkedAsNoShowCount);
                }

                // Persist all changes via Unit of Work
                var changesSaved = await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation(
                    "Saved {Changes} changes to database",
                    changesSaved);

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Reservation cleanup completed successfully in {Duration}ms. " +
                    "Cancelled: {Cancelled}, No-Show: {NoShow}",
                    result.Duration.TotalMilliseconds,
                    result.CancelledPendingCount,
                    result.MarkedAsNoShowCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup operation.");
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogError(ex,
                "Reservation cleanup failed after {Duration}ms: {Error}",
                result.Duration.TotalMilliseconds,
                ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Gets the current count of reservations by status for monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary with reservation counts by status.</returns>
    public async Task<Dictionary<ReservationStatus, int>> GetReservationStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        var nonDeletedSpec = new NonDeletedReservationsSpecification();
        var reservations = (await _reservationRepository.FindAsync(nonDeletedSpec)).ToList();

        return _reservationManagement.GetReservationStatistics(reservations);
    }
}

