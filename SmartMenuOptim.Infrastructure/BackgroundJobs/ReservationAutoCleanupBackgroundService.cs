using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using SmartMenuOptim.Domain.Services.Abstractions;

namespace SmartMenuOptim.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically cleans up expired and no-show reservations.
/// </summary>
/// <remarks>
/// <para><strong>Infrastructure Layer - Background Job</strong></para>
/// 
/// This hosted service runs continuously in the background, executing reservation
/// cleanup operations on a configurable schedule.
/// 
/// <para><strong>Features:</strong></para>
/// <list type="bullet">
///   <item><description>Runs periodically based on configuration (default: every 6 hours)</description></item>
///   <item><description>Uses scoped services for database access</description></item>
///   <item><description>Handles errors gracefully with exponential backoff</description></item>
///   <item><description>Logs all operations for monitoring and debugging</description></item>
///   <item><description>Supports graceful shutdown via cancellation token</description></item>
/// </list>
/// 
/// <para><strong>Configuration:</strong></para>
/// <code>
/// {
///   "ReservationCleanup": {
///     "IntervalHours": 6,
///     "PendingExpirationHours": 24,
///     "Enabled": true
///   }
/// }
/// </code>
/// </remarks>
public class ReservationAutoCleanupBackgroundService : BackgroundService
{
    // IserviceProvider is used to create scopes for resolving scoped services. Scoped services cannot be injected directly into singleton background services.
    // This is a common pattern for background services that need to use scoped dependencies like DbContext, repositories, etc.
    private readonly IServiceProvider _serviceProvider; 
    private readonly ILogger<ReservationAutoCleanupBackgroundService> _logger;
    private readonly ReservationCleanupOptions _options;

    public ReservationAutoCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReservationAutoCleanupBackgroundService> logger,
        IOptions<ReservationCleanupOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Reservation Auto-Cleanup Background Service starting. " +
            "Interval: {Interval} hours, Pending Expiration: {Expiration} hours, Enabled: {Enabled}",
            _options.IntervalHours,
            _options.PendingExpirationHours,
            _options.Enabled);

        if (!_options.Enabled)
        {
            _logger.LogInformation("Reservation cleanup is DISABLED via configuration. Service will not run.");
            return;
        }

        // Wait a short delay on startup before first execution
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled reservation cleanup cycle");

                await ExecuteCleanupCycleAsync(stoppingToken);

                // Wait for the configured interval before next execution
                var delay = TimeSpan.FromHours(_options.IntervalHours);
                _logger.LogInformation(
                    "Reservation cleanup cycle complete. Next execution in {Hours} hours",
                    _options.IntervalHours);

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // This is expected when the service is stopping
                _logger.LogInformation("Reservation cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error in reservation cleanup background service. Will retry after delay.");

                // Exponential backoff on error (1 minute, then 5 minutes, then normal interval)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Reservation Auto-Cleanup Background Service stopped");
    }

    /// <summary>
    /// Executes a single cleanup cycle.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ExecuteCleanupCycleAsync(CancellationToken cancellationToken)
    {
        // Create a scope to resolve scoped services (DbContext, etc.)
        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Resolve the cleanup service from DI (implementation registered in Application layer)
            // Service provider scope ensures proper disposal after use
            var cleanupService = scope.ServiceProvider.GetRequiredService<IReservationCleanupService>();

            _logger.LogInformation("Executing reservation cleanup with {Hours} hours expiration threshold",
                _options.PendingExpirationHours);

            var result = await cleanupService.ExecuteCleanupAsync(
                _options.PendingExpirationHours,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "✅ Reservation cleanup completed successfully: {Summary}",
                    result.ToString());

                // Log detailed statistics
                if (result.CancelledPendingCount > 0 || result.MarkedAsNoShowCount > 0)
                {
                    _logger.LogInformation(
                        "Cleanup details: " +
                        "Expired Pending (Cancelled {Cancelled}/{Total}), " +
                        "No-Show (Marked {Marked}/{Total}), " +
                        "Duration: {Duration}ms",
                        result.CancelledPendingCount,
                        result.ExpiredPendingCount,
                        result.MarkedAsNoShowCount,
                        result.NoShowIdentifiedCount,
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogDebug("No reservations required cleanup during this cycle");
                }
            }
            else
            {
                _logger.LogWarning(
                    "❌ Reservation cleanup failed: {Summary}",
                    result.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error executing reservation cleanup cycle: {Message}",
                ex.Message);
            throw; // Re-throw to trigger backoff in ExecuteAsync
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reservation Auto-Cleanup Background Service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Configuration options for reservation cleanup background service.
/// </summary>
/// <remarks>
/// Bind this from appsettings.json section "ReservationCleanup".
/// </remarks>
public class ReservationCleanupOptions
{
    /// <summary>
    /// Gets or sets the interval in hours between cleanup executions.
    /// </summary>
    /// <remarks>
    /// Default: 6 hours
    /// Recommended: 4-12 hours depending on reservation volume
    /// </remarks>
    public int IntervalHours { get; set; } = 6;

    /// <summary>
    /// Gets or sets the number of hours after which pending reservations expire.
    /// </summary>
    /// <remarks>
    /// Default: 24 hours
    /// Recommended: 12-48 hours depending on business policy
    /// </remarks>
    public int PendingExpirationHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets whether the cleanup service is enabled.
    /// </summary>
    /// <remarks>
    /// Default: true
    /// Set to false to disable auto-cleanup in development or testing
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
