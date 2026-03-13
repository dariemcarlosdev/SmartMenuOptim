using SmartMenuOptim.Domain.Aggregates.TableAggregate.Specifications;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services;
using SmartMenuOptim.Domain.Specifications;

namespace SmartMenuOptim.Application.Features.Reservations.Services;

/// <summary>
/// Application service for reservation reporting and statistics.
/// </summary>
/// <remarks>
/// <para><strong>Application Service - Reporting Layer</strong></para>
/// 
/// This service provides comprehensive reporting and analytics for reservations,
/// including status distribution, time-based analysis, and operational metrics.
/// 
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
///   <item><description>Generate reservation statistics by status</description></item>
///   <item><description>Provide time-based reservation analysis</description></item>
///   <item><description>Calculate operational KPIs (no-show rate, cancellation rate, etc.)</description></item>
///   <item><description>Support dashboard and reporting features</description></item>
/// </list>
/// 
/// <para><strong>Clean Architecture:</strong></para>
/// <list type="bullet">
///   <item><description>Uses Domain specifications for complex queries</description></item>
///   <item><description>Delegates calculations to Domain services when appropriate</description></item>
///   <item><description>Returns DTOs suitable for presentation layer</description></item>
/// </list>
/// </remarks>
public class ReservationReportingService
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly ReservationManagementService _reservationManagement;
    private readonly ILogger<ReservationReportingService> _logger;

    public ReservationReportingService(
        IRepository<Reservation> reservationRepository,
        ReservationManagementService reservationManagement,
        ILogger<ReservationReportingService> logger)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _reservationManagement = reservationManagement ?? throw new ArgumentNullException(nameof(reservationManagement));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive reservation statistics including status distribution and KPIs.
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID to filter by tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive statistics report.</returns>
    public async Task<ReservationStatisticsReport> GetStatisticsAsync(
        int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating reservation statistics report{Restaurant}",
            restaurantId.HasValue ? $" for restaurant {restaurantId}" : "");

        try
        {
            // Use specification to get all non-deleted reservations
            var spec = new NonDeletedReservationsSpecification();
            var reservations = await _reservationRepository.FindAsync(spec);
            var reservationsList = reservations.ToList();

            // Filter by restaurant if specified
            if (restaurantId.HasValue)
            {
                reservationsList = reservationsList.Where(r => r.RestaurantId == restaurantId.Value).ToList();
            }

            _logger.LogDebug("Retrieved {Count} reservations for statistics", reservationsList.Count);

            var report = new ReservationStatisticsReport
            {
                GeneratedAt = DateTime.UtcNow,
                RestaurantId = restaurantId,
                TotalReservations = reservationsList.Count
            };

            // Get status distribution from domain service
            report.StatusDistribution = _reservationManagement.GetReservationStatistics(reservationsList);

            // Calculate derived metrics
            CalculateStatusMetrics(report, reservationsList);
            CalculateTimeBasedMetrics(report, reservationsList);
            CalculateOperationalKPIs(report, reservationsList);

            _logger.LogInformation(
                "Statistics report generated: {Total} total, {Active} active, {NoShowRate:P} no-show rate",
                report.TotalReservations,
                report.ActiveReservationsCount,
                report.NoShowRate);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reservation statistics report");
            throw;
        }
    }

    /// <summary>
    /// Gets reservation status counts grouped by status.
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary with counts by status.</returns>
    public async Task<Dictionary<ReservationStatus, int>> GetStatusCountsAsync(
        int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting reservation status counts{Restaurant}",
            restaurantId.HasValue ? $" for restaurant {restaurantId}" : "");

        var spec = new NonDeletedReservationsSpecification();
        var reservations = (await _reservationRepository.FindAsync(spec)).ToList();

        if (restaurantId.HasValue)
        {
            reservations = reservations.Where(r => r.RestaurantId == restaurantId.Value).ToList();
        }

        return _reservationManagement.GetReservationStatistics(reservations);
    }

    /// <summary>
    /// Gets time-based reservation statistics for a specific date range.
    /// </summary>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="restaurantId">Optional restaurant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Time-based statistics.</returns>
    public async Task<TimeBasedStatistics> GetTimeBasedStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting time-based statistics from {Start} to {End}{Restaurant}",
            startDate, endDate,
            restaurantId.HasValue ? $" for restaurant {restaurantId}" : "");

        var spec = new NonDeletedReservationsSpecification();
        var allReservations = (await _reservationRepository.FindAsync(spec)).ToList();

        // Apply filters
        var filteredReservations = allReservations
            .Where(r => r.ReservationTime >= startDate && r.ReservationTime <= endDate);

        if (restaurantId.HasValue)
        {
            filteredReservations = filteredReservations.Where(r => r.RestaurantId == restaurantId.Value);
        }

        var reservations = filteredReservations.ToList();

        return new TimeBasedStatistics
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalReservations = reservations.Count,
            ReservationsByDay = reservations
                .GroupBy(r => r.ReservationTime.Date)
                .ToDictionary(g => g.Key, g => g.Count()),
            ReservationsByHour = reservations
                .GroupBy(r => r.ReservationTime.Hour)
                .ToDictionary(g => g.Key, g => g.Count()),
            PeakDay = reservations
                .GroupBy(r => r.ReservationTime.Date)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key,
            PeakHour = reservations
                .GroupBy(r => r.ReservationTime.Hour)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? 0
        };
    }

    /// <summary>
    /// Gets active reservations count (Pending + Confirmed).
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of active reservations.</returns>
    public async Task<int> GetActiveReservationsCountAsync(
        int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new ActiveReservationsSpecification();
        var reservations = (await _reservationRepository.FindAsync(spec)).ToList();

        if (restaurantId.HasValue)
        {
            reservations = reservations.Where(r => r.RestaurantId == restaurantId.Value).ToList();
        }

        return reservations.Count;
    }

    // ===================================================================
    // PRIVATE HELPER METHODS
    // ===================================================================

    private void CalculateStatusMetrics(ReservationStatisticsReport report, List<Reservation> reservations)
    {
        report.PendingCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.Pending, 0);
        report.ConfirmedCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.Confirmed, 0);
        report.SeatedCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.Seated, 0);
        report.CompletedCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.Completed, 0);
        report.CancelledCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.Cancelled, 0);
        report.NoShowCount = report.StatusDistribution.GetValueOrDefault(ReservationStatus.NoShow, 0);

        report.ActiveReservationsCount = report.PendingCount + report.ConfirmedCount + report.SeatedCount;
    }

    private void CalculateTimeBasedMetrics(ReservationStatisticsReport report, List<Reservation> reservations)
    {
        var now = DateTime.UtcNow;

        report.UpcomingReservationsCount = reservations
            .Count(r => r.ReservationTime > now && r.IsActive());

        report.PastReservationsCount = reservations
            .Count(r => r.ReservationTime <= now);

        // Calculate average lead time (days between creation and reservation time)
        var reservationsWithLeadTime = reservations
            .Where(r => r.ReservationTime > r.CreatedAt)
            .Select(r => (r.ReservationTime - r.CreatedAt).TotalDays)
            .ToList();

        report.AverageLeadTimeDays = reservationsWithLeadTime.Any()
            ? reservationsWithLeadTime.Average()
            : 0;
    }

    private void CalculateOperationalKPIs(ReservationStatisticsReport report, List<Reservation> reservations)
    {
        var completedOrFinal = report.CompletedCount + report.CancelledCount + report.NoShowCount;

        if (completedOrFinal > 0)
        {
            report.CompletionRate = (double)report.CompletedCount / completedOrFinal;
            report.CancellationRate = (double)report.CancelledCount / completedOrFinal;
            report.NoShowRate = (double)report.NoShowCount / completedOrFinal;
        }

        // Calculate customer type distribution
        var registeredCustomers = reservations.Count(r => r.CustomerId.HasValue);
        var walkIns = reservations.Count(r => !r.CustomerId.HasValue);

        report.RegisteredCustomerReservations = registeredCustomers;
        report.WalkInReservations = walkIns;

        if (report.TotalReservations > 0)
        {
            report.RegisteredCustomerRate = (double)registeredCustomers / report.TotalReservations;
        }
    }
}

/// <summary>
/// Comprehensive reservation statistics report.
/// </summary>
public class ReservationStatisticsReport
{
    public DateTime GeneratedAt { get; set; }
    public int? RestaurantId { get; set; }
    
    // Overall metrics
    public int TotalReservations { get; set; }
    public int ActiveReservationsCount { get; set; }
    public int UpcomingReservationsCount { get; set; }
    public int PastReservationsCount { get; set; }
    
    // Status distribution
    public Dictionary<ReservationStatus, int> StatusDistribution { get; set; } = new();
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int SeatedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int NoShowCount { get; set; }
    
    // Operational KPIs
    public double CompletionRate { get; set; }
    public double CancellationRate { get; set; }
    public double NoShowRate { get; set; }
    
    // Customer type metrics
    public int RegisteredCustomerReservations { get; set; }
    public int WalkInReservations { get; set; }
    public double RegisteredCustomerRate { get; set; }
    
    // Time-based metrics
    public double AverageLeadTimeDays { get; set; }
    
    public override string ToString()
    {
        return $"Reservation Statistics Report (Generated: {GeneratedAt:u})\n" +
               $"Total: {TotalReservations}, Active: {ActiveReservationsCount}, " +
               $"Completion Rate: {CompletionRate:P}, No-Show Rate: {NoShowRate:P}";
    }
}

/// <summary>
/// Time-based reservation statistics.
/// </summary>
public class TimeBasedStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalReservations { get; set; }
    
    /// <summary>
    /// Reservation counts by date.
    /// </summary>
    public Dictionary<DateTime, int> ReservationsByDay { get; set; } = new();
    
    /// <summary>
    /// Reservation counts by hour of day (0-23).
    /// </summary>
    public Dictionary<int, int> ReservationsByHour { get; set; } = new();
    
    /// <summary>
    /// Date with the most reservations.
    /// </summary>
    public DateTime? PeakDay { get; set; }
    
    /// <summary>
    /// Hour with the most reservations.
    /// </summary>
    public int PeakHour { get; set; }
}
