using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for forecasting inventory needs based on sales patterns and dish popularity.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service analyzes historical sales data to predict future inventory requirements,
/// helping restaurants optimize stock levels and reduce waste.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Inventory forecasting calculations without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Dish, SaleRecord, Ingredient data</description></item>
///   <item><description><strong>Business Rules:</strong> Implements forecasting algorithms and inventory optimization</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Dish, Ingredient, Quantity, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Forecasting Capabilities:</strong></para>
/// <list type="bullet">
///   <item><description>Predict ingredient needs based on historical sales patterns</description></item>
///   <item><description>Calculate optimal reorder points and quantities</description></item>
///   <item><description>Identify seasonal trends in ingredient usage</description></item>
///   <item><description>Suggest safety stock levels to prevent stockouts</description></item>
///   <item><description>Alert for potential waste based on expiration dates</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var forecastingService = new InventoryForecastingService();
/// var forecast = forecastingService.ForecastIngredientNeeds(dish, salesHistory, forecastDays: 7);
/// var reorderPoint = forecastingService.CalculateReorderPoint(ingredient, dailyUsage, leadTimeDays);
/// </code>
/// </remarks>
public class InventoryForecastingService
{
    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const decimal SafetyStockMultiplier = 1.2m; // 20% safety stock buffer
    private const int DefaultForecastDays = 7; // Default forecast period
    private const int MinimumHistoricalDays = 14; // Minimum data for accurate forecast
    private const decimal SeasonalVarianceFactor = 0.15m; // 15% variance for seasonal adjustments
    private const decimal WastageAllowance = 0.05m; // 5% expected wastage
    
    /// <summary>
    /// Forecasts ingredient requirements based on historical sales data for a specific dish.
    /// </summary>
    /// <param name="dish">The dish to forecast ingredient needs for.</param>
    /// <param name="salesHistory">Historical sales records for the dish.</param>
    /// <param name="forecastDays">Number of days to forecast ahead (default: 7).</param>
    /// <returns>Dictionary mapping ingredient names to forecasted quantities needed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dish or salesHistory is null.</exception>
    /// <exception cref="ArgumentException">Thrown when insufficient historical data is provided.</exception>
    public Dictionary<string, decimal> ForecastIngredientNeeds(
        Dish dish,
        IEnumerable<SaleRecord> salesHistory,
        int forecastDays = DefaultForecastDays)
    {
        if (dish == null)
            throw new ArgumentNullException(nameof(dish));
        
        if (salesHistory == null)
            throw new ArgumentNullException(nameof(salesHistory));

        var salesList = salesHistory.ToList();
        
        if (!salesList.Any())
            throw new ArgumentException("Sales history cannot be empty", nameof(salesHistory));

        // Calculate average daily sales
        var totalDays = (salesList.Max(s => s.SaleDate) - salesList.Min(s => s.SaleDate)).Days;
        if (totalDays < MinimumHistoricalDays)
            throw new ArgumentException($"Insufficient historical data. Minimum {MinimumHistoricalDays} days required.", nameof(salesHistory));

        var totalQuantitySold = salesList.Sum(s => s.QuantitySold);
        var averageDailySales = totalQuantitySold / Math.Max(1, totalDays);

        // Forecast total sales for the period
        var forecastedSales = averageDailySales * forecastDays;
        
        // Apply safety stock and wastage allowance
        var adjustedForecast = forecastedSales * SafetyStockMultiplier * (1 + WastageAllowance);

        // Calculate ingredient needs (placeholder - would need actual ingredient data from dish)
        var ingredientForecast = new Dictionary<string, decimal>
        {
            { $"{dish.Name}_ForecastedUnits", Math.Ceiling(adjustedForecast) }
        };

        return ingredientForecast;
    }

    /// <summary>
    /// Calculates the reorder point for an ingredient based on daily usage and lead time.
    /// </summary>
    /// <param name="ingredientName">Name of the ingredient.</param>
    /// <param name="averageDailyUsage">Average daily usage quantity.</param>
    /// <param name="leadTimeDays">Supplier lead time in days.</param>
    /// <returns>The quantity at which to reorder the ingredient.</returns>
    public decimal CalculateReorderPoint(string ingredientName, decimal averageDailyUsage, int leadTimeDays)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
            throw new ArgumentException("Ingredient name cannot be empty", nameof(ingredientName));
        
        if (averageDailyUsage <= 0)
            throw new ArgumentException("Average daily usage must be greater than zero", nameof(averageDailyUsage));
        
        if (leadTimeDays <= 0)
            throw new ArgumentException("Lead time must be greater than zero", nameof(leadTimeDays));

        // Reorder Point = (Average Daily Usage × Lead Time) + Safety Stock
        var usageDuringLeadTime = averageDailyUsage * leadTimeDays;
        var safetyStock = usageDuringLeadTime * (SafetyStockMultiplier - 1);
        
        return Math.Ceiling(usageDuringLeadTime + safetyStock);
    }

    /// <summary>
    /// Analyzes sales trends to identify seasonal patterns in ingredient usage.
    /// </summary>
    /// <param name="salesHistory">Historical sales records covering multiple seasons.</param>
    /// <returns>Dictionary mapping month numbers to seasonal demand multipliers.</returns>
    public Dictionary<int, decimal> AnalyzeSeasonalTrends(IEnumerable<SaleRecord> salesHistory)
    {
        if (salesHistory == null)
            throw new ArgumentNullException(nameof(salesHistory));

        var salesList = salesHistory.ToList();
        if (!salesList.Any())
            return new Dictionary<int, decimal>();

        // Group sales by month and calculate average
        var monthlyAverages = salesList
            .GroupBy(s => s.SaleDate.Month)
            .ToDictionary(
                g => g.Key,
                g => g.Average(s => s.QuantitySold)
            );

        // Calculate overall average
        var overallAverage = monthlyAverages.Values.Average();

        // Calculate seasonal multipliers (how each month compares to average)
        var seasonalMultipliers = monthlyAverages.ToDictionary(
            kvp => kvp.Key,
            kvp => (decimal)(kvp.Value / overallAverage)
        );

        return seasonalMultipliers;
    }

    /// <summary>
    /// Calculates the economic order quantity (EOQ) for optimal inventory ordering.
    /// </summary>
    /// <param name="annualDemand">Annual demand for the ingredient.</param>
    /// <param name="orderingCost">Fixed cost per order.</param>
    /// <param name="holdingCostPerUnit">Annual cost to hold one unit in inventory.</param>
    /// <returns>The optimal order quantity that minimizes total inventory costs.</returns>
    public decimal CalculateEconomicOrderQuantity(decimal annualDemand, decimal orderingCost, decimal holdingCostPerUnit)
    {
        if (annualDemand <= 0)
            throw new ArgumentException("Annual demand must be greater than zero", nameof(annualDemand));
        
        if (orderingCost <= 0)
            throw new ArgumentException("Ordering cost must be greater than zero", nameof(orderingCost));
        
        if (holdingCostPerUnit <= 0)
            throw new ArgumentException("Holding cost must be greater than zero", nameof(holdingCostPerUnit));

        // EOQ = √((2 × Annual Demand × Ordering Cost) / Holding Cost per Unit)
        var eoq = Math.Sqrt((2 * (double)annualDemand * (double)orderingCost) / (double)holdingCostPerUnit);
        
        return Math.Ceiling((decimal)eoq);
    }
}
