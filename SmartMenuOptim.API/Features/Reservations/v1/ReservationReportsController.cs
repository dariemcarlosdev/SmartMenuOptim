using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMenuOptim.Application.Services.Reservations;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Enums;

namespace SmartMenuOptim.API.Features.Reservations.v1;

/// <summary>
/// API controller for reservation reporting and statistics.
/// </summary>
/// <remarks>
/// Provides endpoints for reservation analytics, monitoring, and operational metrics.
/// <para><b>Status:</b> Planned - Not yet implemented. Hidden from API documentation.</para>
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)] // TODO: Remove when fully implemented
[Authorize]
public class ReservationReportsController : ControllerBase
{
    private readonly ReservationReportingService _reportingService;
    private readonly ILogger<ReservationReportsController> _logger;

    public ReservationReportsController(
        ReservationReportingService reportingService,
        ILogger<ReservationReportsController> logger)
    {
        _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive reservation statistics.
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive statistics report.</returns>
    /// <response code="200">Returns the statistics report.</response>
    /// <response code="401">Unauthorized - user not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ReservationStatisticsReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReservationStatisticsReport>> GetStatisticsAsync(
        [FromQuery] int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "GET /api/ReservationReports/statistics called{Restaurant}",
                restaurantId.HasValue ? $" for restaurant {restaurantId}" : "");

            var report = await _reportingService.GetStatisticsAsync(restaurantId, cancellationToken);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reservation statistics");
            return StatusCode(500, "An error occurred while generating statistics");
        }
    }

    /// <summary>
    /// Gets reservation counts grouped by status.
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary with counts by status.</returns>
    /// <response code="200">Returns status counts.</response>
    /// <response code="401">Unauthorized - user not authenticated.</response>
    [HttpGet("status-counts")]
    [ProducesResponseType(typeof(Dictionary<ReservationStatus, int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Dictionary<ReservationStatus, int>>> GetStatusCountsAsync(
        [FromQuery] int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GET /api/ReservationReports/status-counts called");

            var counts = await _reportingService.GetStatusCountsAsync(restaurantId, cancellationToken);

            return Ok(counts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status counts");
            return StatusCode(500, "An error occurred while retrieving status counts");
        }
    }

    /// <summary>
    /// Gets time-based reservation statistics for a date range.
    /// </summary>
    /// <param name="startDate">Start of date range (ISO 8601 format).</param>
    /// <param name="endDate">End of date range (ISO 8601 format).</param>
    /// <param name="restaurantId">Optional restaurant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Time-based statistics.</returns>
    /// <response code="200">Returns time-based statistics.</response>
    /// <response code="400">Bad request - invalid date range.</response>
    /// <response code="401">Unauthorized - user not authenticated.</response>
    [HttpGet("time-based")]
    [ProducesResponseType(typeof(TimeBasedStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TimeBasedStatistics>> GetTimeBasedStatisticsAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest("Start date must be before end date");
            }

            if ((endDate - startDate).TotalDays > 365)
            {
                return BadRequest("Date range cannot exceed 365 days");
            }

            _logger.LogInformation(
                "GET /api/ReservationReports/time-based called: {Start} to {End}",
                startDate, endDate);

            var statistics = await _reportingService.GetTimeBasedStatisticsAsync(
                startDate,
                endDate,
                restaurantId,
                cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time-based statistics");
            return StatusCode(500, "An error occurred while retrieving time-based statistics");
        }
    }

    /// <summary>
    /// Gets the count of active reservations (Pending + Confirmed + Seated).
    /// </summary>
    /// <param name="restaurantId">Optional restaurant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active reservations count.</returns>
    /// <response code="200">Returns active count.</response>
    /// <response code="401">Unauthorized - user not authenticated.</response>
    [HttpGet("active-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<int>> GetActiveCountAsync(
        [FromQuery] int? restaurantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("GET /api/ReservationReports/active-count called");

            var count = await _reportingService.GetActiveReservationsCountAsync(restaurantId, cancellationToken);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active reservations count");
            return StatusCode(500, "An error occurred while retrieving active count");
        }
    }
}
