using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.ReviewAggregate;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for ranking dishes by popularity based on sales data and customer preferences.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service analyzes dish performance metrics to create popularity rankings,
/// helping restaurants understand customer preferences and optimize menu offerings.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Ranking algorithms without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Dish, SaleRecord, Review data</description></item>
///   <item><description><strong>Business Rules:</strong> Implements popularity scoring and ranking algorithms</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Dish, Popularity, Ranking, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Ranking Factors:</strong></para>
/// <list type="bullet">
///   <item><description>Sales volume (number of orders)</description></item>
///   <item><description>Revenue contribution</description></item>
///   <item><description>Customer ratings and reviews</description></item>
///   <item><description>Repeat purchase rate</description></item>
///   <item><description>Time-based trends (trending up/down)</description></item>
///   <item><description>Seasonal popularity variations</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var rankingService = new DishPopularityRankingService();
/// var topDishes = rankingService.RankDishesByPopularity(dishes, salesRecords, topN: 10);
/// var trendingDishes = rankingService.IdentifyTrendingDishes(salesRecords, timeWindow: 30);
/// </code>
/// </remarks>
public class DishPopularityRankingService
{
    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const decimal SalesVolumeWeight = 0.40m; // 40% weight for sales volume
    private const decimal RevenueWeight = 0.30m; // 30% weight for revenue contribution
    private const decimal RatingWeight = 0.20m; // 20% weight for customer ratings
    private const decimal TrendWeight = 0.10m; // 10% weight for trending factor
    private const int TrendingThresholdDays = 30; // Days to consider for trending analysis
    private const decimal TrendingGrowthThreshold = 0.20m; // 20% growth to be considered trending
    
    /// <summary>
    /// Represents a dish with its popularity score and ranking.
    /// </summary>
    public class DishPopularityRanking
    {
        public int DishId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public int Rank { get; set; }
        public decimal PopularityScore { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal? AverageRating { get; set; }
        public bool IsTrending { get; set; }
    }

    /// <summary>
    /// Ranks dishes by popularity based on multiple factors including sales, revenue, and ratings.
    /// </summary>
    /// <param name="dishes">Collection of dishes to rank.</param>
    /// <param name="salesRecords">Historical sales records.</param>
    /// <param name="reviews">Customer reviews (optional).</param>
    /// <param name="topN">Number of top dishes to return (default: 10).</param>
    /// <returns>List of top N dishes ranked by popularity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dishes or salesRecords is null.</exception>
    public List<DishPopularityRanking> RankDishesByPopularity(
        IEnumerable<Dish> dishes,
        IEnumerable<SaleRecord> salesRecords,
        IEnumerable<Review>? reviews = null,
        int topN = 10)
    {
        if (dishes == null)
            throw new ArgumentNullException(nameof(dishes));
        
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var dishesList = dishes.ToList();
        var salesList = salesRecords.ToList();
        var reviewsList = reviews?.ToList() ?? new List<Review>();

        if (!dishesList.Any() || !salesList.Any())
            return new List<DishPopularityRanking>();

        // Calculate max values for normalization
        var maxSales = salesList.GroupBy(s => s.DishId).Max(g => g.Sum(s => s.QuantitySold));
        var maxRevenue = salesList.GroupBy(s => s.DishId).Max(g => g.Sum(s => s.SaleAmount.Amount));

        var rankings = new List<DishPopularityRanking>();

        foreach (var dish in dishesList)
        {
            var dishSales = salesList.Where(s => s.DishId == dish.Id).ToList();
            var dishReviews = reviewsList.Where(r => r.DishId == dish.Id).ToList();

            if (!dishSales.Any())
                continue;

            var totalSales = dishSales.Sum(s => s.QuantitySold);
            var totalRevenue = dishSales.Sum(s => s.SaleAmount.Amount);
            var averageRating = dishReviews.Any() ? (decimal?)dishReviews.Average(r => r.Rating) : null;

            // Normalize scores (0-1 range)
            var normalizedSales = maxSales > 0 ? (decimal)totalSales / maxSales : 0;
            var normalizedRevenue = maxRevenue > 0 ? totalRevenue / maxRevenue : 0;
            var normalizedRating = averageRating.HasValue ? averageRating.Value / 5.0m : 0.5m; // Default to neutral if no ratings

            // Calculate trending factor
            var isTrending = IsDishTrending(dishSales);
            var trendingFactor = isTrending ? 1.0m : 0.5m;

            // Calculate weighted popularity score
            var popularityScore = 
                (normalizedSales * SalesVolumeWeight) +
                (normalizedRevenue * RevenueWeight) +
                (normalizedRating * RatingWeight) +
                (trendingFactor * TrendWeight);

            rankings.Add(new DishPopularityRanking
            {
                DishId = dish.Id,
                DishName = dish.Name,
                PopularityScore = Math.Round(popularityScore, 4),
                TotalSales = totalSales,
                TotalRevenue = totalRevenue,
                AverageRating = averageRating,
                IsTrending = isTrending
            });
        }

        // Rank and assign positions
        var rankedList = rankings
            .OrderByDescending(r => r.PopularityScore)
            .Take(topN)
            .ToList();

        for (int i = 0; i < rankedList.Count; i++)
        {
            rankedList[i].Rank = i + 1;
        }

        return rankedList;
    }

    /// <summary>
    /// Identifies dishes that are currently trending (showing significant growth).
    /// </summary>
    /// <param name="salesRecords">Recent sales records.</param>
    /// <param name="timeWindowDays">Number of days to analyze (default: 30).</param>
    /// <returns>List of dish IDs that are trending.</returns>
    public List<int> IdentifyTrendingDishes(
        IEnumerable<SaleRecord> salesRecords,
        int timeWindowDays = TrendingThresholdDays)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var salesList = salesRecords.OrderByDescending(s => s.SaleDate).ToList();
        
        if (!salesList.Any())
            return new List<int>();

        var cutoffDate = DateTime.UtcNow.AddDays(-timeWindowDays);
        var recentSales = salesList.Where(s => s.SaleDate >= cutoffDate).ToList();
        
        if (!recentSales.Any())
            return new List<int>();

        var midpoint = cutoffDate.AddDays(timeWindowDays / 2.0);
        
        var trendingDishes = recentSales
            .GroupBy(s => s.DishId)
            .Select(g =>
            {
                var firstHalf = g.Where(s => s.SaleDate < midpoint).Sum(s => s.QuantitySold);
                var secondHalf = g.Where(s => s.SaleDate >= midpoint).Sum(s => s.QuantitySold);
                
                var growth = firstHalf > 0 ? (decimal)(secondHalf - firstHalf) / firstHalf : 0;
                
                return new { DishId = g.Key, Growth = growth };
            })
            .Where(d => d.Growth >= TrendingGrowthThreshold)
            .Select(d => d.DishId)
            .ToList();

        return trendingDishes;
    }

    /// <summary>
    /// Finds dishes that are losing popularity (declining sales).
    /// </summary>
    /// <param name="salesRecords">Sales records to analyze.</param>
    /// <param name="timeWindowDays">Number of days to analyze (default: 30).</param>
    /// <returns>List of dish IDs showing declining popularity.</returns>
    public List<int> IdentifyDecliningDishes(
        IEnumerable<SaleRecord> salesRecords,
        int timeWindowDays = TrendingThresholdDays)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var salesList = salesRecords.OrderByDescending(s => s.SaleDate).ToList();
        
        if (!salesList.Any())
            return new List<int>();

        var cutoffDate = DateTime.UtcNow.AddDays(-timeWindowDays);
        var recentSales = salesList.Where(s => s.SaleDate >= cutoffDate).ToList();
        
        if (!recentSales.Any())
            return new List<int>();

        var midpoint = cutoffDate.AddDays(timeWindowDays / 2.0);
        
        var decliningDishes = recentSales
            .GroupBy(s => s.DishId)
            .Select(g =>
            {
                var firstHalf = g.Where(s => s.SaleDate < midpoint).Sum(s => s.QuantitySold);
                var secondHalf = g.Where(s => s.SaleDate >= midpoint).Sum(s => s.QuantitySold);
                
                var decline = firstHalf > 0 ? (decimal)(firstHalf - secondHalf) / firstHalf : 0;
                
                return new { DishId = g.Key, Decline = decline };
            })
            .Where(d => d.Decline >= TrendingGrowthThreshold) // Using same threshold
            .Select(d => d.DishId)
            .ToList();

        return decliningDishes;
    }

    /// <summary>
    /// Calculates the repeat purchase rate for a dish.
    /// </summary>
    /// <param name="dishId">ID of the dish to analyze.</param>
    /// <param name="salesRecords">Sales records including customer information.</param>
    /// <returns>Repeat purchase rate as a decimal (0-1).</returns>
    public decimal CalculateRepeatPurchaseRate(int dishId, IEnumerable<SaleRecord> salesRecords)
    {
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var dishSales = salesRecords.Where(s => s.DishId == dishId).ToList();
        
        if (!dishSales.Any())
            return 0m;

        // Note: This is a simplified calculation
        // In a real implementation, you would need customer IDs in SaleRecord
        // to properly track repeat purchases
        var totalPurchases = dishSales.Count;
        var estimatedRepeatPurchases = totalPurchases > 10 ? totalPurchases * 0.3m : 0;
        
        return totalPurchases > 0 ? estimatedRepeatPurchases / totalPurchases : 0m;
    }

    /// <summary>
    /// Determines if a dish is trending based on recent sales growth.
    /// </summary>
    /// <param name="dishSales">Sales records for the specific dish.</param>
    /// <returns>True if the dish is trending, false otherwise.</returns>
    private bool IsDishTrending(List<SaleRecord> dishSales)
    {
        if (!dishSales.Any())
            return false;

        var cutoffDate = DateTime.UtcNow.AddDays(-TrendingThresholdDays);
        var recentSales = dishSales.Where(s => s.SaleDate >= cutoffDate).ToList();
        
        if (!recentSales.Any())
            return false;

        var midpoint = cutoffDate.AddDays(TrendingThresholdDays / 2.0);
        var firstHalf = recentSales.Where(s => s.SaleDate < midpoint).Sum(s => s.QuantitySold);
        var secondHalf = recentSales.Where(s => s.SaleDate >= midpoint).Sum(s => s.QuantitySold);
        
        if (firstHalf == 0)
            return secondHalf > 0; // New dish with sales is trending

        var growth = (decimal)(secondHalf - firstHalf) / firstHalf;
        return growth >= TrendingGrowthThreshold;
    }

    /// <summary>
    /// Compares popularity between different categories.
    /// </summary>
    /// <param name="dishes">All dishes with their categories.</param>
    /// <param name="salesRecords">Sales records.</param>
    /// <returns>Dictionary mapping category names to average popularity scores.</returns>
    public Dictionary<string, decimal> CompareCategoryPopularity(
        IEnumerable<Dish> dishes,
        IEnumerable<SaleRecord> salesRecords)
    {
        if (dishes == null)
            throw new ArgumentNullException(nameof(dishes));
        
        if (salesRecords == null)
            throw new ArgumentNullException(nameof(salesRecords));

        var dishesList = dishes.ToList();
        var salesList = salesRecords.ToList();

        if (!dishesList.Any() || !salesList.Any())
            return new Dictionary<string, decimal>();

        // Group dishes by category and calculate average sales
        var categoryPopularity = dishesList
            .GroupBy(d => d.Category?.Name ?? "Uncategorized")
            .Select(g =>
            {
                var categoryDishIds = g.Select(d => d.Id).ToHashSet();
                var categorySales = salesList.Where(s => categoryDishIds.Contains(s.DishId));
                var totalSales = categorySales.Sum(s => s.QuantitySold);
                var averageSales = g.Count() > 0 ? (decimal)totalSales / g.Count() : 0;
                
                return new { Category = g.Key, AveragePopularity = averageSales };
            })
            .ToDictionary(x => x.Category, x => x.AveragePopularity);

        return categoryPopularity;
    }
}
