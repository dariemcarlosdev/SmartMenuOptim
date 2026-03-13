using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate.Events;

namespace SmartMenuOptim.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that generates daily sales summaries at the end of each business day.
/// </summary>
/// <remarks>
/// <para><strong>Purpose:</strong></para>
/// <para>This hosted service runs on a schedule to aggregate daily sales data and publish
/// <see cref="DailySalesSummarizedEvent"/> for each active restaurant.</para>
/// 
/// <para><strong>Schedule:</strong></para>
/// <para>Runs at 2:00 AM daily to summarize the previous day's sales. This timing ensures
/// all orders from the previous business day are captured.</para>
/// 
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
///     <item><description>Aggregate sales data for each restaurant</description></item>
///     <item><description>Calculate KPIs (revenue, orders, avg order value, etc.)</description></item>
///     <item><description>Identify underperforming dishes</description></item>
///     <item><description>Compare against historical data and targets</description></item>
///     <item><description>Publish DailySalesSummarizedEvent for each restaurant</description></item>
/// </list>
/// 
/// <para><strong>Production Considerations:</strong></para>
/// <para>In production, consider using a more robust scheduler like Hangfire or Azure Functions
/// with timer triggers for better reliability and monitoring.</para>
/// </remarks>
public class DailySalesSummaryBackgroundJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailySalesSummaryBackgroundJob> _logger;
    
    /// <summary>
    /// Time of day to run the summary job (2:00 AM).
    /// </summary>
    private readonly TimeSpan _runTime = new(2, 0, 0);

    public DailySalesSummaryBackgroundJob(
        IServiceProvider serviceProvider,
        ILogger<DailySalesSummaryBackgroundJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DailySalesSummaryBackgroundJob started. Scheduled to run daily at {Time}",
            _runTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = GetNextRunTime(now);
            var delay = nextRun - now;

            _logger.LogDebug(
                "Next daily summary run scheduled for {NextRun} (in {Delay})",
                nextRun,
                delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DailySalesSummaryBackgroundJob stopping due to cancellation");
                break;
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await RunDailySummaryAsync(stoppingToken);
            }
        }

        _logger.LogInformation("DailySalesSummaryBackgroundJob stopped");
    }

    private DateTime GetNextRunTime(DateTime now)
    {
        var todayRun = now.Date.Add(_runTime);
        
        // If we've passed today's run time, schedule for tomorrow
        return now > todayRun 
            ? todayRun.AddDays(1) 
            : todayRun;
    }

    private async Task RunDailySummaryAsync(CancellationToken stoppingToken)
    {
        var summaryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        
        _logger.LogInformation(
            "Starting daily sales summary for {Date}",
            summaryDate);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // In a real implementation, you would:
            // 1. Query all active restaurants
            // 2. For each restaurant, aggregate sales data for the previous day
            // 3. Calculate metrics (revenue, orders, avg order value, etc.)
            // 4. Identify underperforming dishes
            // 5. Fetch comparison data (previous day, same day last week)
            // 6. Publish DailySalesSummarizedEvent

            // Placeholder: Simulate processing for demonstration
            // In production, replace with actual data aggregation logic

            _logger.LogInformation(
                "Daily sales summary job completed for {Date}",
                summaryDate);

            // Example of publishing an event (commented out as it requires actual data)
            /*
            var summaryEvent = new DailySalesSummarizedEvent(
                restaurantId: restaurantId,
                restaurantName: restaurant.Name,
                summaryDate: summaryDate,
                totalRevenue: totalRevenue,
                totalOrders: totalOrders,
                totalItemsSold: totalItems,
                uniqueCustomers: uniqueCustomers,
                topSellingDish: topDish,
                topSellingDishQuantity: topDishQuantity,
                topSellingDishRevenue: topDishRevenue,
                peakHour: peakHour,
                peakHourOrders: peakHourOrders,
                // ... other parameters
            );

            await mediator.Publish(summaryEvent, stoppingToken);
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error running daily sales summary for {Date}. Error: {Message}",
                summaryDate,
                ex.Message);
        }
    }
}
