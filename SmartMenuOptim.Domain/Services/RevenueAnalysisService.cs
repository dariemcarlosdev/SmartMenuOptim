using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for analyzing revenue streams, profitability, and financial performance metrics.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service provides comprehensive revenue analysis capabilities including trend analysis,
/// profitability calculations, and financial forecasting for restaurant operations.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Financial analysis calculations without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with SaleRecord, Order, Revenue data</description></item>
///   <item><description><strong>Business Rules:</strong> Implements financial analysis and forecasting algorithms</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Revenue, Profit, Money, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Analysis Capabilities:</strong></para>
/// <list type="bullet">
///   <item><description>Revenue trend analysis (daily, weekly, monthly)</description></item>
///   <item><description>Profitability calculations and margin analysis</description></item>
///   <item><description>Peak revenue period identification</description></item>
///   <item><description>Revenue forecasting based on historical trends</description></item>
///   <item><description>Comparative period analysis (YoY, MoM, WoW)</description></item>
///   <item><description>Revenue per customer metrics</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var revenueService = new RevenueAnalysisService();
/// var monthlyRevenue = revenueService.CalculateTotalRevenue(salesRecords, period: "monthly");
/// var growth = revenueService.CalculateRevenueGrowth(currentPeriod, previousPeriod);
/// var forecast = revenueService.ForecastRevenue(historicalData, forecastDays: 30);
/// </code>
/// </remarks>
public class RevenueAnalysisService
{
    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const decimal HealthyGrowthRate = 0.10m; // 10% growth is considered healthy
    private const decimal TargetProfitMargin = 0.60m; // Target 60% profit margin
    private const decimal MinimumViableProfitMargin = 0.30m; // 30% minimum for viability
    private const int TrendAnalysisDays = 90; // 90 days for trend analysis
    
    /// <summary>
    /// Represents revenue analysis results for a specific time period.
    /// </summary>
    public class RevenueAnalysisResult
    {
        public Money TotalRevenue { get; set; } = Money.Zero("USD");
        public int TotalTransactions { get; set; }
        public Money AverageTransactionValue { get; set; } = Money.Zero("USD");
        public Money HighestTransaction { get; set; } = Money.Zero("USD");
        public Money LowestTransaction { get; set; } = Money.Zero("USD");
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    /// <summary>
    /// Calculates total revenue from sales records for a given period.
    /// </summary>
    /// <param name="salesRecords">Collection of sales records to analyze.</param>
    /// <param name="startDate">Start date of the analysis period.</param>
    /// <param name="endDate">End date of the analysis period.</param>
    /// <returns>Revenue analysis result containing key metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when salesRecords is null.</exception>
    public RevenueAnalysisResult CalculateTotalRevenue(
        IEnumerable<SaleRecord> salesRecords,
        DateTime startDate,
        DateTime endDate)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var filteredRecords = salesRecords
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
            .ToList();

        if (!filteredRecords.Any())
        {
            return new RevenueAnalysisResult
            {
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }

        var totalRevenue = filteredRecords.Sum(s => s.SaleAmount.Amount);
        var currency = filteredRecords.First().SaleAmount.Currency;

        return new RevenueAnalysisResult
        {
            TotalRevenue = new Money(totalRevenue, currency),
            TotalTransactions = filteredRecords.Count,
            AverageTransactionValue = new Money(totalRevenue / filteredRecords.Count, currency),
            HighestTransaction = new Money(filteredRecords.Max(s => s.SaleAmount.Amount), currency),
            LowestTransaction = new Money(filteredRecords.Min(s => s.SaleAmount.Amount), currency),
            PeriodStart = startDate,
            PeriodEnd = endDate
        };
    }

    /// <summary>
    /// Calculates revenue growth rate between two periods.
    /// </summary>
    /// <param name="currentPeriodRevenue">Revenue for the current period.</param>
    /// <param name="previousPeriodRevenue">Revenue for the previous period.</param>
    /// <returns>Growth rate as a decimal (e.g., 0.15 for 15% growth).</returns>
    public decimal CalculateRevenueGrowth(Money currentPeriodRevenue, Money previousPeriodRevenue)
    {
        if (currentPeriodRevenue == null)
            throw new ArgumentNullException(nameof(currentPeriodRevenue));
        
        if (previousPeriodRevenue == null)
            throw new ArgumentNullException(nameof(previousPeriodRevenue));

        if (previousPeriodRevenue.Amount == 0)
            return currentPeriodRevenue.Amount > 0 ? 1.0m : 0m;

        return (currentPeriodRevenue.Amount - previousPeriodRevenue.Amount) / previousPeriodRevenue.Amount;
    }

    /// <summary>
    /// Analyzes revenue trends over time and identifies patterns.
    /// </summary>
    /// <param name="salesRecords">Historical sales records.</param>
    /// <returns>Dictionary mapping dates to daily revenue totals.</returns>
    public Dictionary<DateTime, Money> AnalyzeRevenueTrend(IEnumerable<SaleRecord> salesRecords)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var salesList = salesRecords.ToList();
        if (!salesList.Any())
            return new Dictionary<DateTime, Money>();

        var currency = salesList.First().SaleAmount.Currency;

        var dailyRevenue = salesList
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => new Money(g.Sum(s => s.SaleAmount.Amount), currency)
            );

        return dailyRevenue;
    }

    /// <summary>
    /// Identifies peak revenue periods (hours, days, or months).
    /// </summary>
    /// <param name="salesRecords">Sales records to analyze.</param>
    /// <returns>List of peak periods with their revenue amounts.</returns>
    public List<(string Period, Money Revenue)> IdentifyPeakRevenuePeriods(IEnumerable<SaleRecord> salesRecords)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var salesList = salesRecords.ToList();
        if (!salesList.Any())
            return new List<(string, Money)>();

        var currency = salesList.First().SaleAmount.Currency;

        // Analyze by day of week
        var dayOfWeekRevenue = salesList
            .GroupBy(s => s.SaleDate.DayOfWeek)
            .Select(g => (
                Period: g.Key.ToString(),
                Revenue: new Money(g.Sum(s => s.SaleAmount.Amount), currency)
            ))
            .OrderByDescending(x => x.Revenue.Amount)
            .Take(3)
            .ToList();

        return dayOfWeekRevenue;
    }

    /// <summary>
    /// Forecasts future revenue based on historical trends using simple moving average.
    /// </summary>
    /// <param name="salesRecords">Historical sales data.</param>
    /// <param name="forecastDays">Number of days to forecast ahead.</param>
    /// <returns>Forecasted daily revenue amount.</returns>
    public Money ForecastRevenue(IEnumerable<SaleRecord> salesRecords, int forecastDays)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));
        
        if (forecastDays <= 0)
            throw new ArgumentException("Forecast days must be greater than zero", nameof(forecastDays));

        var salesList = salesRecords.OrderByDescending(s => s.SaleDate).ToList();
        
        if (!salesList.Any())
            return Money.Zero("USD");

        var currency = salesList.First().SaleAmount.Currency;

        // Calculate average daily revenue from recent history
        var recentSales = salesList.Take(TrendAnalysisDays).ToList();
        var totalDays = (recentSales.Max(s => s.SaleDate) - recentSales.Min(s => s.SaleDate)).Days;
        
        if (totalDays == 0)
            totalDays = 1;

        var averageDailyRevenue = recentSales.Sum(s => s.SaleAmount.Amount) / totalDays;
        var forecastedRevenue = averageDailyRevenue * forecastDays;

        return new Money(forecastedRevenue, currency);
    }

    /// <summary>
    /// Calculates the average revenue per customer over a period.
    /// </summary>
    /// <param name="salesRecords">Sales records for the period.</param>
    /// <param name="uniqueCustomerCount">Number of unique customers in the period.</param>
    /// <returns>Average revenue per customer.</returns>
    public Money CalculateAverageRevenuePerCustomer(
        IEnumerable<SaleRecord> salesRecords,
        int uniqueCustomerCount)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));
        
        if (uniqueCustomerCount <= 0)
            throw new ArgumentException("Customer count must be greater than zero", nameof(uniqueCustomerCount));

        var salesList = salesRecords.ToList();
        if (!salesList.Any())
            return Money.Zero("USD");

        var totalRevenue = salesList.Sum(s => s.SaleAmount.Amount);
        var currency = salesList.First().SaleAmount.Currency;
        var averagePerCustomer = totalRevenue / uniqueCustomerCount;

        return new Money(averagePerCustomer, currency);
    }

    /// <summary>
    /// Analyzes profitability by calculating profit margins for sales records.
    /// </summary>
    /// <param name="salesRecords">Sales records to analyze.</param>
    /// <param name="totalCosts">Total costs associated with the sales.</param>
    /// <returns>Profit margin as a decimal percentage.</returns>
    public decimal CalculateProfitMargin(IEnumerable<SaleRecord> salesRecords, Money totalCosts)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));
        
        if (totalCosts == null)
            throw new ArgumentNullException(nameof(totalCosts));

        var salesList = salesRecords.ToList();
        if (!salesList.Any())
            return 0m;

        var totalRevenue = salesList.Sum(s => s.SaleAmount.Amount);
        
        if (totalRevenue == 0)
            return 0m;

        var profit = totalRevenue - totalCosts.Amount;
        return profit / totalRevenue;
    }

    /// <summary>
    /// Compares revenue performance across different time periods.
    /// </summary>
    /// <param name="periods">Dictionary of period names to sales records.</param>
    /// <returns>Comparative analysis showing revenue for each period.</returns>
    public Dictionary<string, Money> CompareRevenueBetweenPeriods(
        Dictionary<string, IEnumerable<SaleRecord>> periods)
    {
        if (periods == null)
            throw new ArgumentNullException(nameof(periods));

        var comparison = new Dictionary<string, Money>();

        foreach (var period in periods)
        {
            var salesList = period.Value.ToList();
            if (!salesList.Any())
            {
                comparison[period.Key] = Money.Zero("USD");
                continue;
            }

            var totalRevenue = salesList.Sum(s => s.SaleAmount.Amount);
            var currency = salesList.First().SaleAmount.Currency;
            comparison[period.Key] = new Money(totalRevenue, currency);
        }

        return comparison;
    }
}
