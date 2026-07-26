using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for optimizing menu composition based on profitability, popularity, and strategic goals.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service applies menu engineering principles to optimize menu composition,
/// balancing profitability with customer preferences and operational constraints.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Menu optimization algorithms without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Menu, Dish, SaleRecord, Category</description></item>
///   <item><description><strong>Business Rules:</strong> Implements menu engineering and optimization strategies</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Menu, Dish, Profitability, Popularity, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Optimization Strategies:</strong></para>
/// <list type="bullet">
///   <item><description>Menu Engineering Matrix (Stars, Plowhorses, Puzzles, Dogs)</description></item>
///   <item><description>Profitability-based dish ranking</description></item>
///   <item><description>Category balance optimization</description></item>
///   <item><description>Seasonal menu rotation recommendations</description></item>
///   <item><description>Underperforming item identification</description></item>
///   <item><description>Cross-selling opportunity detection</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var optimizationService = new MenuOptimizationService();
/// var classification = optimizationService.ClassifyDishByPerformance(dish, salesData, profitability);
/// var recommendations = optimizationService.GenerateMenuOptimizationRecommendations(menu, salesHistory);
/// </code>
/// </remarks>
public class MenuOptimizationService
{
    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const decimal HighProfitabilityThreshold = 0.60m; // 60% profit margin for high profitability
    private const decimal LowProfitabilityThreshold = 0.40m; // 40% profit margin threshold
    private const int HighPopularityThreshold = 100; // Sales per month for high popularity
    private const int LowPopularityThreshold = 30; // Sales per month for low popularity
    private const int OptimalMenuSize = 12; // Optimal number of items per menu category
    private const int MinimumMenuSize = 6; // Minimum items per category
    private const int MaximumMenuSize = 18; // Maximum items per category
    
    /// <summary>
    /// Menu engineering classification categories based on profitability and popularity.
    /// </summary>
    public enum DishClassification
    {
        /// <summary>Stars: High profitability, high popularity - promote heavily</summary>
        Star,
        
        /// <summary>Plowhorses: Low profitability, high popularity - consider repricing or cost reduction</summary>
        Plowhorse,
        
        /// <summary>Puzzles: High profitability, low popularity - needs better marketing/positioning</summary>
        Puzzle,
        
        /// <summary>Dogs: Low profitability, low popularity - consider removing from menu</summary>
        Dog
    }

    /// <summary>
    /// Classifies a dish using the Menu Engineering Matrix based on profitability and popularity.
    /// </summary>
    /// <param name="dish">The dish to classify.</param>
    /// <param name="monthlySales">Number of times the dish was sold in the past month.</param>
    /// <param name="profitMargin">Profit margin as a decimal (e.g., 0.65 for 65%).</param>
    /// <returns>The classification category for the dish.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dish is null.</exception>
    public DishClassification ClassifyDishByPerformance(Dish dish, int monthlySales, decimal profitMargin)
    {
        if (dish == null)
            throw new ArgumentNullException(nameof(dish));

        var isHighProfitability = profitMargin >= HighProfitabilityThreshold;
        var isHighPopularity = monthlySales >= HighPopularityThreshold;

        return (isHighProfitability, isHighPopularity) switch
        {
            (true, true) => DishClassification.Star,
            (true, false) => DishClassification.Puzzle,
            (false, true) => DishClassification.Plowhorse,
            (false, false) => DishClassification.Dog
        };
    }

    /// <summary>
    /// Generates optimization recommendations for a menu based on sales history and performance data.
    /// </summary>
    /// <param name="menu">The menu to optimize.</param>
    /// <param name="salesHistory">Historical sales data for dishes in the menu.</param>
    /// <returns>A list of actionable recommendations for menu optimization.</returns>
    public List<string> GenerateMenuOptimizationRecommendations(
        Menu menu,
        IEnumerable<SaleRecord> salesHistory)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));
        
        if (salesHistory == null)
            throw new ArgumentNullException(nameof(salesHistory));

        var recommendations = new List<string>();
        var salesList = salesHistory.ToList();

        // Check menu size
        var dishCount = menu.MenuDishes?.Count ?? 0;
        if (dishCount < MinimumMenuSize)
        {
            recommendations.Add($"Menu has only {dishCount} items. Consider adding more variety (optimal: {OptimalMenuSize}).");
        }
        else if (dishCount > MaximumMenuSize)
        {
            recommendations.Add($"Menu has {dishCount} items. Consider reducing to avoid choice paralysis (optimal: {OptimalMenuSize}).");
        }

        // Analyze dish performance if we have sales data
        if (salesList.Any())
        {
            var totalSales = salesList.Sum(s => s.SaleAmount.Amount);
            var averageSalePerDish = totalSales / Math.Max(1, dishCount);

            // Identify underperforming dishes
            var dishSales = salesList
                .GroupBy(s => s.DishId)
                .Select(g => new
                {
                    DishId = g.Key,
                    TotalRevenue = g.Sum(s => s.SaleAmount.Amount),
                    UnitsSold = g.Sum(s => s.QuantitySold)
                })
                .ToList();

            var poorPerformers = dishSales
                .Where(d => d.TotalRevenue < averageSalePerDish * 0.5m)
                .ToList();

            if (poorPerformers.Any())
            {
                recommendations.Add($"Found {poorPerformers.Count} underperforming dishes generating less than 50% of average revenue. Consider reviewing or replacing.");
            }

            // Identify top performers
            var topPerformers = dishSales
                .OrderByDescending(d => d.TotalRevenue)
                .Take(3)
                .ToList();

            if (topPerformers.Any())
            {
                recommendations.Add($"Top 3 dishes generate significant revenue. Consider featuring them prominently and creating complementary items.");
            }
        }

        return recommendations;
    }

    /// <summary>
    /// Calculates the optimal number of dishes for each category to maintain menu balance.
    /// </summary>
    /// <param name="totalDishes">Total number of dishes in the menu.</param>
    /// <returns>Dictionary mapping category names to recommended dish counts.</returns>
    public Dictionary<string, int> CalculateOptimalCategoryDistribution(int totalDishes)
    {
        if (totalDishes <= 0)
            throw new ArgumentException("Total dishes must be greater than zero", nameof(totalDishes));

        // Standard restaurant category distribution percentages
        return new Dictionary<string, int>
        {
            { "Appetizers", (int)Math.Ceiling(totalDishes * 0.20m) },
            { "Salads", (int)Math.Ceiling(totalDishes * 0.15m) },
            { "Main Courses", (int)Math.Ceiling(totalDishes * 0.35m) },
            { "Sides", (int)Math.Ceiling(totalDishes * 0.10m) },
            { "Desserts", (int)Math.Ceiling(totalDishes * 0.15m) },
            { "Beverages", (int)Math.Ceiling(totalDishes * 0.05m) }
        };
    }

    /// <summary>
    /// Identifies dishes that should be featured or promoted based on performance metrics.
    /// </summary>
    /// <param name="dishes">Collection of dishes to analyze.</param>
    /// <param name="salesHistory">Sales history for the dishes.</param>
    /// <param name="topCount">Number of top dishes to identify (default: 5).</param>
    /// <returns>List of dish IDs that should be featured.</returns>
    public List<int> IdentifyDishesToFeature(
        IEnumerable<Dish> dishes,
        IEnumerable<SaleRecord> salesHistory,
        int topCount = 5)
    {
        if (dishes == null)
            throw new ArgumentNullException(nameof(dishes));
        
        if (salesHistory == null)
            throw new ArgumentNullException(nameof(salesHistory));

        var salesList = salesHistory.ToList();
        if (!salesList.Any())
            return new List<int>();

        // Calculate performance score: (Revenue × 0.6) + (Units Sold × 0.4)
        var dishPerformance = salesList
            .GroupBy(s => s.DishId)
            .Select(g => new
            {
                DishId = g.Key,
                Revenue = g.Sum(s => s.SaleAmount.Amount),
                UnitsSold = g.Sum(s => s.QuantitySold),
                PerformanceScore = (g.Sum(s => s.SaleAmount.Amount) * 0.6m) + (g.Sum(s => s.QuantitySold) * 0.4m)
            })
            .OrderByDescending(d => d.PerformanceScore)
            .Take(topCount)
            .Select(d => d.DishId)
            .ToList();

        return dishPerformance;
    }

    /// <summary>
    /// Evaluates menu diversity and suggests improvements.
    /// </summary>
    /// <param name="menu">The menu to evaluate.</param>
    /// <returns>Diversity score from 0 to 1, where 1 is perfectly diverse.</returns>
    public decimal EvaluateMenuDiversity(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        if (menu.MenuDishes == null || !menu.MenuDishes.Any())
            return 0m;

        // Placeholder for diversity calculation
        // In a real implementation, this would analyze:
        // - Category distribution
        // - Cuisine variety
        // - Dietary option coverage (vegetarian, vegan, gluten-free, etc.)
        // - Price range distribution
        
        var dishCount = menu.MenuDishes.Count;
        var diversityScore = dishCount >= OptimalMenuSize ? 0.8m : (decimal)dishCount / OptimalMenuSize;

        return Math.Min(diversityScore, 1.0m);
    }
}
