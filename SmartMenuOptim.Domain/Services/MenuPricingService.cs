using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Calculates optimal pricing for dishes and menus based on multiple business factors.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service encapsulates complex pricing algorithms that don't belong to a single entity.
/// It considers cost, demand, competition, seasonality, and market dynamics to recommend
/// optimal pricing strategies for menu items and overall menu composition.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Complex pricing calculations without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Dish, Menu, SaleRecord, CustomerLoyaltyTier</description></item>
///   <item><description><strong>Business Rules:</strong> Implements sophisticated pricing strategies and business calculations</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Money, Dish, Menu, etc.)</description></item>
///   <item><description><strong>No Infrastructure:</strong> Contains only business logic, no database or external API calls</description></item>
/// </list>
/// 
/// <para><strong>Pricing Strategies Supported:</strong></para>
/// <list type="bullet">
///   <item><description>Cost-plus pricing (ingredient cost + markup)</description></item>
///   <item><description>Demand-based pricing (sales volume analysis)</description></item>
///   <item><description>Competitive pricing (market positioning)</description></item>
///   <item><description>Dynamic pricing (time-of-day, day-of-week adjustments)</description></item>
///   <item><description>Seasonal pricing (seasonal menu variations)</description></item>
///   <item><description>Psychological pricing (.99, .95 endings)</description></item>
///   <item><description>Category-based balancing</description></item>
///   <item><description>Menu-level optimization</description></item>
/// </list>
/// 
/// <para><strong>Design Decision - No Abstraction Layer:</strong></para>
/// <para>This service is implemented as a concrete class without an abstract base or interface because:</para>
/// <list type="bullet">
///   <item><description><strong>Single Responsibility:</strong> One clear pricing strategy for the SmartMenuOptimizer domain</description></item>
///   <item><description><strong>YAGNI Principle:</strong> No current need for multiple pricing implementations</description></item>
///   <item><description><strong>Domain Service Pattern:</strong> Domain services don't require abstraction unless infrastructure separation is needed</description></item>
///   <item><description><strong>Simpler Design:</strong> Reduces complexity and maintains focus on business logic</description></item>
///   <item><description><strong>Easy to Extend:</strong> Can introduce interface/abstraction later if multiple strategies are needed</description></item>
/// </list>
/// 
/// <para><strong>When to Add Abstraction:</strong></para>
/// <para>Consider adding an <c>IPricingService</c> interface if you need:</para>
/// <list type="bullet">
///   <item><description>Multiple pricing strategies (FastFoodPricing, FineDiningPricing, etc.)</description></item>
///   <item><description>Strategy pattern for different restaurant types</description></item>
///   <item><description>Easy mocking for unit tests (though concrete class is also testable)</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var pricingService = new MenuPricingService();
/// var optimalPrice = pricingService.CalculateOptimalPrice(dish, salesHistory);
/// var dynamicPrice = pricingService.ApplyDynamicPricing(basePrice, DateTime.Now, DayOfWeek.Friday);
/// var menuAnalysis = pricingService.AnalyzeMenuPriceDistribution(menu);
/// </code>
/// </remarks>
public class MenuPricingService
{
    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================
    
    private const decimal DefaultMarkupPercentage = 0.65m; // 65% markup standard
    private const decimal MinimumProfitMargin = 0.30m; // 30% minimum profit margin
    private const decimal PeakHoursPremium = 0.10m; // 10% increase during peak hours
    private const decimal OffPeakDiscount = 0.15m; // 15% discount during off-peak
    private const decimal WeekendPremium = 0.05m; // 5% weekend premium
    private const decimal HappyHourDiscount = 0.20m; // 20% happy hour discount
    private const int HighDemandThreshold = 50; // Sales per week for high demand
    private const int LowDemandThreshold = 20; // Sales per week for low demand
    private const decimal HighDemandPremium = 0.20m; // 20% premium for high demand
    private const decimal LowDemandDiscount = 0.10m; // 10% discount for low demand
    private const int PeakLunchStart = 12; // Lunch peak starts at noon
    private const int PeakLunchEnd = 14; // Lunch peak ends at 2 PM
    private const int PeakDinnerStart = 18; // Dinner peak starts at 6 PM
    private const int PeakDinnerEnd = 21; // Dinner peak ends at 9 PM
    private const int OffPeakStart = 15; // Off-peak starts at 3 PM
    private const int OffPeakEnd = 17; // Off-peak ends at 5 PM

    // ===================================================================
    // BASE PRICING METHODS
    // ===================================================================

    /// <summary>
    /// Calculates optimal price for a single dish based on multiple factors including cost,
    /// demand (sales history), and competitive positioning.
    /// </summary>
    /// <param name="dish">The dish to price</param>
    /// <param name="salesHistory">Historical sales data for demand analysis</param>
    /// <returns>Recommended optimal price</returns>
    /// <exception cref="ArgumentNullException">When dish is null</exception>
    /// <exception cref="ArgumentException">When dish price is not positive</exception>
    public Money CalculateOptimalPrice(Dish dish, IEnumerable<SaleRecord> salesHistory)
    {
        ValidateDish(dish);

        var dishCost = new Money(dish.DishPrice, "USD");
        
        // Calculate base price using cost-plus pricing
        var basePrice = CalculateCostPlusPrice(dishCost, DefaultMarkupPercentage);

        // Adjust for demand if sales history is available
        if (salesHistory?.Any() == true)
        {
            var demandMultiplier = CalculateDemandMultiplier(salesHistory);
            basePrice = basePrice * demandMultiplier;
        }

        // Ensure minimum profit margin
        var minimumPrice = dishCost / (1 - MinimumProfitMargin);
        if (basePrice.Amount < minimumPrice.Amount)
        {
            basePrice = minimumPrice;
        }

        // Apply psychological pricing
        return ApplyPsychologicalPricing(basePrice, PricingStrategy.PsychologicalPricing);
    }

    /// <summary>
    /// Calculates cost-plus pricing with specified markup percentage.
    /// Standard restaurant industry practice for baseline pricing.
    /// </summary>
    /// <param name="ingredientCost">Total cost of ingredients</param>
    /// <param name="markupPercentage">Markup percentage (e.g., 0.65 for 65%)</param>
    /// <returns>Price with markup applied</returns>
    /// <exception cref="ArgumentNullException">When ingredientCost is null</exception>
    /// <exception cref="ArgumentException">When cost is not positive or markup is negative</exception>
    public Money CalculateCostPlusPrice(Money ingredientCost, decimal markupPercentage)
    {
        if (ingredientCost == null)
            throw new ArgumentNullException(nameof(ingredientCost));

        if (ingredientCost.Amount <= 0)
            throw new ArgumentException("Ingredient cost must be positive", nameof(ingredientCost));

        if (markupPercentage < 0)
            throw new ArgumentException("Markup percentage cannot be negative", nameof(markupPercentage));

        var priceAmount = ingredientCost.Amount * (1 + markupPercentage);
        return new Money(priceAmount, ingredientCost.Currency);
    }

    /// <summary>
    /// Calculates competitive pricing based on market analysis.
    /// Positions pricing slightly below average competitor price for value perception.
    /// </summary>
    /// <param name="baseCost">Base cost of the dish</param>
    /// <param name="competitorPrices">Competitor prices for similar dishes</param>
    /// <returns>Competitively positioned price</returns>
    /// <exception cref="ArgumentNullException">When baseCost is null</exception>
    public Money CalculateCompetitivePrice(Money baseCost, IEnumerable<Money> competitorPrices)
    {
        if (baseCost == null)
            throw new ArgumentNullException(nameof(baseCost));

        // If no competitor data, use cost-plus pricing
        if (competitorPrices == null || !competitorPrices.Any())
        {
            return CalculateCostPlusPrice(baseCost, DefaultMarkupPercentage);
        }

        // Calculate average competitor price
        var avgCompetitorPrice = new Money(
            competitorPrices.Average(p => p.Amount),
            baseCost.Currency
        );

        // Position at 95% of competitor average for value perception
        var targetPrice = avgCompetitorPrice.Amount * 0.95m;

        // Calculate our cost-plus price
        var ourPrice = CalculateCostPlusPrice(baseCost, DefaultMarkupPercentage);

        // Take weighted average: 70% our calculation, 30% competitive positioning
        var finalAmount = (ourPrice.Amount * 0.7m) + (targetPrice * 0.3m);

        // Ensure we maintain minimum profit margin
        var minimumPrice = baseCost / (1 - MinimumProfitMargin);
        if (finalAmount < minimumPrice.Amount)
        {
            finalAmount = minimumPrice.Amount;
        }

        return new Money(finalAmount, baseCost.Currency);
    }

    // ===================================================================
    // DYNAMIC PRICING METHODS
    // ===================================================================

    /// <summary>
    /// Applies time-based pricing adjustments for peak/off-peak hours and weekend premiums.
    /// Helps optimize revenue and manage demand.
    /// </summary>
    /// <param name="basePrice">The standard menu price</param>
    /// <param name="timeOfDay">Current time</param>
    /// <param name="dayOfWeek">Current day of week</param>
    /// <returns>Adjusted price with time-based factors</returns>
    /// <exception cref="ArgumentNullException">When basePrice is null</exception>
    public Money ApplyDynamicPricing(Money basePrice, DateTime timeOfDay, DayOfWeek dayOfWeek)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        var multiplier = 1.0m;
        var hour = timeOfDay.Hour;

        // Peak hours (lunch 12-2pm, dinner 6-9pm): +10%
        if ((hour >= PeakLunchStart && hour < PeakLunchEnd) || 
            (hour >= PeakDinnerStart && hour < PeakDinnerEnd))
        {
            multiplier += PeakHoursPremium;
        }
        // Off-peak hours (3-5pm): -15% to drive traffic
        else if (hour >= OffPeakStart && hour < OffPeakEnd)
        {
            multiplier -= OffPeakDiscount;
        }

        // Weekend premium (Friday-Sunday): +5%
        if (dayOfWeek >= DayOfWeek.Friday && dayOfWeek <= DayOfWeek.Sunday)
        {
            multiplier += WeekendPremium;
        }

        return basePrice * multiplier;
    }

    /// <summary>
    /// Calculates demand-based pricing using sales velocity analysis.
    /// Adjusts prices based on how quickly items are selling.
    /// </summary>
    /// <param name="basePrice">Base price of the item</param>
    /// <param name="salesVelocity">Number of items sold in the period</param>
    /// <param name="period">Time period for velocity calculation</param>
    /// <returns>Demand-adjusted price</returns>
    /// <exception cref="ArgumentNullException">When basePrice is null</exception>
    /// <exception cref="ArgumentException">When salesVelocity is negative</exception>
    public Money CalculateDemandBasedPrice(Money basePrice, int salesVelocity, TimeSpan period)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        if (salesVelocity < 0)
            throw new ArgumentException("Sales velocity cannot be negative", nameof(salesVelocity));

        // Normalize to weekly sales for comparison
        var weeklySales = period.TotalDays > 0 
            ? salesVelocity * (7.0m / (decimal)period.TotalDays) 
            : salesVelocity;

        var multiplier = weeklySales switch
        {
            >= HighDemandThreshold => 1 + HighDemandPremium, // High demand: +20%
            <= LowDemandThreshold => 1 - LowDemandDiscount,  // Low demand: -10%
            _ => 1.0m // Medium demand: no change
        };

        return basePrice * multiplier;
    }

    /// <summary>
    /// Applies seasonal pricing adjustments based on ingredient availability and demand patterns.
    /// </summary>
    /// <param name="basePrice">Base price of the dish</param>
    /// <param name="currentSeason">Current season</param>
    /// <param name="category">Dish category</param>
    /// <returns>Seasonally adjusted price</returns>
    /// <exception cref="ArgumentNullException">When basePrice or category is null</exception>
    public Money ApplySeasonalPricing(Money basePrice, Season currentSeason, DishCategory category)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        if (category == null)
            throw new ArgumentNullException(nameof(category));

        var multiplier = 1.0m;
        var categoryName = category.Name.ToLowerInvariant();

        // Seasonal adjustments based on typical patterns
        switch (currentSeason)
        {
            case Season.Summer:
                // Salads and cold dishes premium in summer
                if (categoryName.Contains("salad") || categoryName.Contains("cold"))
                    multiplier = 1.10m;
                // Hot soups discount in summer
                else if (categoryName.Contains("soup"))
                    multiplier = 0.90m;
                break;

            case Season.Winter:
                // Hot soups and comfort food premium in winter
                if (categoryName.Contains("soup") || categoryName.Contains("stew"))
                    multiplier = 1.10m;
                // Salads discount in winter
                else if (categoryName.Contains("salad"))
                    multiplier = 0.90m;
                break;

            case Season.Spring:
            case Season.Fall:
                // Moderate seasons, minimal adjustment
                multiplier = 1.0m;
                break;
        }

        return basePrice * multiplier;
    }

    // ===================================================================
    // DISCOUNT AND PROMOTION METHODS
    // ===================================================================

    /// <summary>
    /// Calculates bulk order discounts based on quantity.
    /// Encourages larger orders and catering business.
    /// </summary>
    /// <param name="dishes">Dishes in the bulk order</param>
    /// <param name="quantity">Total quantity ordered</param>
    /// <returns>Total price with bulk discount applied</returns>
    /// <exception cref="ArgumentNullException">When dishes is null</exception>
    /// <exception cref="ArgumentException">When quantity is not positive</exception>
    public Money CalculateBulkDiscount(IEnumerable<Dish> dishes, int quantity)
    {
        if (dishes == null)
            throw new ArgumentNullException(nameof(dishes));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        var dishList = dishes.ToList();
        if (!dishList.Any())
            return new Money(0, "USD");

        // Calculate total without discount
        var total = dishList.Sum(d => d.DishPrice * quantity);

        // Apply tiered bulk discounts
        var discountRate = quantity switch
        {
            >= 50 => 0.15m,  // 15% discount for 50+ items
            >= 20 => 0.10m,  // 10% discount for 20-49 items
            >= 10 => 0.05m,  // 5% discount for 10-19 items
            _ => 0m          // No discount for less than 10
        };

        var discountedTotal = total * (1 - discountRate);
        return new Money(discountedTotal, "USD");
    }

    /// <summary>
    /// Calculates happy hour pricing with standard discount.
    /// Typically applied during slow business periods.
    /// </summary>
    /// <param name="regularPrice">Regular menu price</param>
    /// <param name="happyHourPeriod">Duration of happy hour</param>
    /// <returns>Discounted happy hour price</returns>
    /// <exception cref="ArgumentNullException">When regularPrice is null</exception>
    public Money CalculateHappyHourPrice(Money regularPrice, TimeSpan happyHourPeriod)
    {
        if (regularPrice == null)
            throw new ArgumentNullException(nameof(regularPrice));

        // Standard 20% discount for happy hour
        return regularPrice * (1 - HappyHourDiscount);
    }

    /// <summary>
    /// Applies loyalty customer discounts based on tier level.
    /// Rewards repeat customers and encourages program participation.
    /// </summary>
    /// <param name="basePrice">Base price before discount</param>
    /// <param name="loyaltyTier">Customer's loyalty tier</param>
    /// <returns>Price with loyalty discount applied</returns>
    /// <exception cref="ArgumentNullException">When basePrice is null</exception>
    public Money ApplyLoyaltyDiscount(Money basePrice, CustomerLoyaltyTier loyaltyTier)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        var discountRate = loyaltyTier switch
        {
            CustomerLoyaltyTier.Platinum => 0.15m,  // 15% discount
            CustomerLoyaltyTier.Gold => 0.10m,      // 10% discount
            CustomerLoyaltyTier.Silver => 0.05m,    // 5% discount
            CustomerLoyaltyTier.Bronze => 0.02m,    // 2% discount
            _ => 0m                                  // No discount for unknown tier
        };

        return basePrice * (1 - discountRate);
    }

    /// <summary>
    /// Calculates combo meal pricing with bundle discount.
    /// Encourages multi-item purchases and increases average order value.
    /// </summary>
    /// <param name="comboDishes">Dishes included in the combo</param>
    /// <param name="comboDiscountPercentage">Discount percentage for the combo</param>
    /// <returns>Total combo price with discount</returns>
    /// <exception cref="ArgumentNullException">When comboDishes is null</exception>
    /// <exception cref="ArgumentException">When discount percentage is invalid</exception>
    public Money CalculateComboPrice(IEnumerable<Dish> comboDishes, decimal comboDiscountPercentage)
    {
        if (comboDishes == null)
            throw new ArgumentNullException(nameof(comboDishes));

        if (comboDiscountPercentage < 0 || comboDiscountPercentage > 1)
            throw new ArgumentException("Discount percentage must be between 0 and 1", nameof(comboDiscountPercentage));

        var dishList = comboDishes.ToList();
        if (!dishList.Any())
            return new Money(0, "USD");

        var totalRegularPrice = dishList.Sum(d => d.DishPrice);
        var discountedPrice = totalRegularPrice * (1 - comboDiscountPercentage);

        return new Money(discountedPrice, "USD");
    }

    // ===================================================================
    // MARKET ANALYSIS METHODS
    // ===================================================================

    /// <summary>
    /// Calculates price elasticity for demand forecasting.
    /// Measures how demand changes in response to price changes.
    /// </summary>
    /// <param name="salesHistory">Historical sales data</param>
    /// <param name="priceHistory">Historical price data</param>
    /// <returns>Price elasticity coefficient</returns>
    public decimal CalculatePriceElasticity(IEnumerable<SaleRecord> salesHistory, IEnumerable<Money> priceHistory)
    {
        if (salesHistory == null || !salesHistory.Any() || 
            priceHistory == null || !priceHistory.Any())
            return 0;

        var sales = salesHistory.ToList();
        var prices = priceHistory.ToList();

        if (sales.Count < 2 || prices.Count < 2 || sales.Count != prices.Count)
            return 0;

        // Calculate percentage changes
        var quantityChange = ((decimal)sales.Last().QuantitySold - sales.First().QuantitySold) / sales.First().QuantitySold;
        var priceChange = (prices.Last().Amount - prices.First().Amount) / prices.First().Amount;

        // Avoid division by zero
        if (priceChange == 0)
            return 0;

        // Price elasticity = % change in quantity / % change in price
        return quantityChange / priceChange;
    }

    /// <summary>
    /// Determines optimal price point for maximum revenue.
    /// Uses sales data to find the sweet spot between volume and margin.
    /// </summary>
    /// <param name="dish">Dish to optimize pricing for</param>
    /// <param name="salesData">Historical sales data at various price points</param>
    /// <returns>Revenue-optimal price</returns>
    public Money FindRevenueOptimalPrice(Dish dish, IEnumerable<SaleRecord> salesData)
    {
        ValidateDish(dish);

        if (salesData == null || !salesData.Any())
        {
            // Fallback to cost-plus pricing if no data
            return CalculateCostPlusPrice(new Money(dish.DishPrice, "USD"), DefaultMarkupPercentage);
        }

        // Group by similar price points and calculate revenue
        var revenueByPrice = salesData
            .GroupBy(s => Math.Round(s.SaleAmount.Amount / s.QuantitySold, 2))
            .Select(g => new
            {
                Price = g.Key,
                Revenue = g.Sum(s => s.SaleAmount.Amount)
            })
            .OrderByDescending(x => x.Revenue)
            .FirstOrDefault();

        if (revenueByPrice != null)
        {
            return new Money(revenueByPrice.Price, "USD");
        }

        // Fallback to current price with psychological pricing
        return ApplyPsychologicalPricing(new Money(dish.DishPrice, "USD"), PricingStrategy.PsychologicalPricing);
    }

    /// <summary>
    /// Calculates break-even price point based on costs and expected volume.
    /// Essential for understanding minimum viable pricing.
    /// </summary>
    /// <param name="totalCost">Total cost including ingredients, labor, overhead</param>
    /// <param name="expectedSalesVolume">Expected number of units to sell</param>
    /// <returns>Break-even price per unit</returns>
    /// <exception cref="ArgumentNullException">When totalCost is null</exception>
    /// <exception cref="ArgumentException">When expectedSalesVolume is not positive</exception>
    public Money CalculateBreakEvenPrice(Money totalCost, int expectedSalesVolume)
    {
        if (totalCost == null)
            throw new ArgumentNullException(nameof(totalCost));

        if (expectedSalesVolume <= 0)
            throw new ArgumentException("Expected sales volume must be positive", nameof(expectedSalesVolume));

        var breakEvenPrice = totalCost.Amount / expectedSalesVolume;
        return new Money(breakEvenPrice, totalCost.Currency);
    }

    // ===================================================================
    // PROFIT MARGIN METHODS
    // ===================================================================

    /// <summary>
    /// Calculates price to achieve target profit margin.
    /// Critical for meeting financial goals.
    /// </summary>
    /// <param name="cost">Cost of the dish</param>
    /// <param name="targetMarginPercentage">Target profit margin (e.g., 0.40 for 40%)</param>
    /// <returns>Price that achieves target margin</returns>
    /// <exception cref="ArgumentNullException">When cost is null</exception>
    /// <exception cref="ArgumentException">When margin percentage is invalid</exception>
    public Money CalculatePriceForTargetMargin(Money cost, decimal targetMarginPercentage)
    {
        if (cost == null)
            throw new ArgumentNullException(nameof(cost));

        if (targetMarginPercentage < 0 || targetMarginPercentage >= 1)
            throw new ArgumentException("Target margin must be between 0 and 1 (exclusive)", nameof(targetMarginPercentage));

        // Price = Cost / (1 - Target Margin)
        var price = cost.Amount / (1 - targetMarginPercentage);
        return new Money(price, cost.Currency);
    }

    /// <summary>
    /// Validates that a price meets minimum profit margin requirements.
    /// Essential for maintaining business profitability.
    /// </summary>
    /// <param name="sellingPrice">Proposed selling price</param>
    /// <param name="cost">Cost of the item</param>
    /// <param name="minimumMarginPercentage">Minimum acceptable margin</param>
    /// <returns>True if margin requirement is met</returns>
    public bool ValidateMinimumMargin(Money sellingPrice, Money cost, decimal minimumMarginPercentage)
    {
        if (sellingPrice == null || cost == null)
            return false;

        if (sellingPrice.Currency != cost.Currency)
            return false;

        if (sellingPrice.Amount <= 0 || cost.Amount <= 0)
            return false;

        // Calculate actual margin: (Price - Cost) / Price
        var actualMargin = (sellingPrice.Amount - cost.Amount) / sellingPrice.Amount;
        return actualMargin >= minimumMarginPercentage;
    }

    /// <summary>
    /// Calculates contribution margin for menu planning and profitability analysis.
    /// Helps identify which dishes contribute most to covering fixed costs.
    /// </summary>
    /// <param name="sellingPrice">Selling price of the dish</param>
    /// <param name="variableCost">Variable costs (ingredients, direct labor)</param>
    /// <returns>Contribution margin amount</returns>
    /// <exception cref="ArgumentNullException">When sellingPrice or variableCost is null</exception>
    public Money CalculateContributionMargin(Money sellingPrice, Money variableCost)
    {
        if (sellingPrice == null)
            throw new ArgumentNullException(nameof(sellingPrice));

        if (variableCost == null)
            throw new ArgumentNullException(nameof(variableCost));

        if (sellingPrice.Currency != variableCost.Currency)
            throw new InvalidOperationException("Currency mismatch between selling price and variable cost");

        return sellingPrice - variableCost;
    }

    // ===================================================================
    // CATEGORY AND MENU-LEVEL METHODS
    // ===================================================================

    /// <summary>
    /// Balances prices across dish categories to create cohesive menu structure.
    /// Ensures price consistency and appropriate tiering within categories.
    /// </summary>
    /// <param name="dishes">Dishes in the category</param>
    /// <param name="category">Category to balance</param>
    /// <returns>Price recommendations for category dishes</returns>
    public IEnumerable<DishPriceRecommendation> BalanceCategoryPrices(
        IEnumerable<Dish> dishes,
        DishCategory category)
    {
        if (dishes == null || !dishes.Any())
            return Enumerable.Empty<DishPriceRecommendation>();

        if (category == null)
            throw new ArgumentNullException(nameof(category));

        var dishList = dishes.ToList();
        var recommendations = new List<DishPriceRecommendation>();

        // Calculate category statistics
        var avgPrice = dishList.Average(d => d.DishPrice);
        var minPrice = dishList.Min(d => d.DishPrice);
        var maxPrice = dishList.Max(d => d.DishPrice);
        var priceRange = maxPrice - minPrice;

        foreach (var dish in dishList)
        {
            var currentPrice = new Money(dish.DishPrice, "USD");
            Money recommendedPrice;
            string reasoning;

            // Dishes priced too high for category
            if (dish.DishPrice > avgPrice * 1.5m)
            {
                recommendedPrice = new Money(avgPrice * 1.3m, "USD");
                reasoning = $"Price is {((dish.DishPrice - avgPrice) / avgPrice * 100):F0}% above category average. " +
                           $"Recommend reducing to maintain category consistency.";
            }
            // Dishes priced too low for category
            else if (dish.DishPrice < avgPrice * 0.6m)
            {
                recommendedPrice = new Money(avgPrice * 0.75m, "USD");
                reasoning = $"Price is {((avgPrice - dish.DishPrice) / avgPrice * 100):F0}% below category average. " +
                           $"Recommend increasing to improve perceived value.";
            }
            // Price is within acceptable range
            else
            {
                recommendedPrice = ApplyPsychologicalPricing(currentPrice, PricingStrategy.PsychologicalPricing);
                reasoning = "Price is well-positioned within category. Applied psychological pricing adjustments.";
            }

            recommendations.Add(new DishPriceRecommendation(
                dish,
                currentPrice,
                recommendedPrice,
                reasoning
            ));
        }

        return recommendations;
    }

    /// <summary>
    /// Analyzes menu price distribution to identify pricing strategy opportunities.
    /// Provides statistical overview of menu pricing structure.
    /// </summary>
    /// <param name="menu">Menu to analyze</param>
    /// <returns>Price analysis report</returns>
    /// <exception cref="ArgumentNullException">When menu is null</exception>
    public MenuPriceAnalysis AnalyzeMenuPriceDistribution(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        var dishes = menu.Dishes?.Where(d => d.IsActive).ToList();
        
        if (dishes == null || !dishes.Any())
        {
            // Return zero analysis for empty menu
            var zeroPriceUsd = new Money(0, "USD");
            return new MenuPriceAnalysis(
                AveragePrice: zeroPriceUsd,
                MedianPrice: zeroPriceUsd,
                LowestPrice: zeroPriceUsd,
                HighestPrice: zeroPriceUsd,
                PriceSpread: 0
            );
        }

        var prices = dishes.Select(d => d.DishPrice).OrderBy(p => p).ToList();

        var avgPrice = new Money(prices.Average(), "USD");
        var medianPrice = new Money(
            prices.Count % 2 == 0
                ? (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2
                : prices[prices.Count / 2],
            "USD"
        );
        var lowestPrice = new Money(prices.First(), "USD");
        var highestPrice = new Money(prices.Last(), "USD");
        var priceSpread = (highestPrice.Amount - lowestPrice.Amount) / avgPrice.Amount;

        return new MenuPriceAnalysis(
            AveragePrice: avgPrice,
            MedianPrice: medianPrice,
            LowestPrice: lowestPrice,
            HighestPrice: highestPrice,
            PriceSpread: priceSpread
        );
    }

    /// <summary>
    /// Recommends price anchoring strategy for menu.
    /// The anchor price is a higher-priced item that makes other items seem more reasonably priced.
    /// </summary>
    /// <param name="menu">Menu to analyze for anchoring</param>
    /// <returns>Price anchoring strategy recommendation</returns>
    /// <exception cref="ArgumentNullException">When menu is null</exception>
    public PriceAnchoringStrategy RecommendPriceAnchoring(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        var activeDishes = menu.Dishes?.Where(d => d.IsActive).ToList();

        if (activeDishes == null || !activeDishes.Any())
        {
            throw new InvalidOperationException("Cannot recommend anchoring for menu with no active dishes");
        }

        // Find the highest-priced dish to serve as anchor
        var anchorDish = activeDishes.OrderByDescending(d => d.DishPrice).First();
        var currentAnchorPrice = new Money(anchorDish.DishPrice, "USD");

        // Calculate average menu price
        var avgPrice = activeDishes.Average(d => d.DishPrice);

        // Recommended anchor should be 2-3x average price
        var recommendedAnchorAmount = avgPrice * 2.5m;
        var recommendedAnchorPrice = new Money(recommendedAnchorAmount, "USD");

        var strategy = currentAnchorPrice.Amount >= avgPrice * 2
            ? "Current highest-priced item serves well as price anchor."
            : $"Consider introducing a premium item priced around {recommendedAnchorPrice.Amount:C} to serve as price anchor.";

        var expectedImpact = "Price anchoring can increase average order value by 15-25% by making " +
                           "mid-priced items appear more affordable in comparison. Customers tend to " +
                           "avoid the highest-priced item but are willing to spend more on items that " +
                           "seem like good value relative to the anchor.";

        return new PriceAnchoringStrategy(
            AnchorDish: anchorDish,
            RecommendedAnchorPrice: recommendedAnchorPrice,
            Strategy: strategy,
            ExpectedImpact: expectedImpact
        );
    }

    // ===================================================================
    // VALIDATION AND BUSINESS RULES
    // ===================================================================

    /// <summary>
    /// Validates price against business constraints and industry standards.
    /// Ensures pricing makes business sense and follows best practices.
    /// </summary>
    /// <param name="proposedPrice">Price to validate</param>
    /// <param name="dish">Dish being priced</param>
    /// <returns>Validation result with errors and suggestions</returns>
    public PriceValidationResult ValidatePrice(Money proposedPrice, Dish dish)
    {
        var errors = new List<string>();

        if (proposedPrice == null)
        {
            errors.Add("Price cannot be null");
            return new PriceValidationResult(false, errors, null);
        }

        if (dish == null)
        {
            errors.Add("Dish cannot be null");
            return new PriceValidationResult(false, errors, null);
        }

        // Validate minimum price
        if (proposedPrice.Amount < 0.01m)
        {
            errors.Add("Price must be at least $0.01");
        }

        // Validate maximum reasonable price (avoid data entry errors)
        if (proposedPrice.Amount > 1000m)
        {
            errors.Add("Price exceeds reasonable maximum ($1000). Please verify.");
        }

        // Validate profit margin
        var dishCost = new Money(dish.DishPrice, "USD");
        if (!ValidateMinimumMargin(proposedPrice, dishCost, MinimumProfitMargin))
        {
            errors.Add($"Price does not meet minimum profit margin requirement of {MinimumProfitMargin * 100}%");
            
            var suggestedPrice = CalculatePriceForTargetMargin(dishCost, MinimumProfitMargin);
            return new PriceValidationResult(false, errors, suggestedPrice);
        }

        // Validate pricing is reasonable compared to cost (not more than 10x)
        if (proposedPrice.Amount > dishCost.Amount * 10)
        {
            errors.Add("Price is more than 10x the cost. Please verify this is intentional.");
        }

        // If validation passed
        if (!errors.Any())
        {
            var optimizedPrice = ApplyPsychologicalPricing(proposedPrice, PricingStrategy.PsychologicalPricing);
            return new PriceValidationResult(true, Enumerable.Empty<string>(), optimizedPrice);
        }

        return new PriceValidationResult(false, errors, null);
    }

    /// <summary>
    /// Ensures prices follow psychological pricing rules for better customer perception.
    /// Common strategies: .99 ending (bargain), .95 ending (quality), round numbers (premium).
    /// </summary>
    /// <param name="calculatedPrice">Raw calculated price</param>
    /// <param name="strategy">Pricing strategy to apply</param>
    /// <returns>Psychologically optimized price</returns>
    public Money ApplyPsychologicalPricing(Money calculatedPrice, PricingStrategy strategy)
    {
        if (calculatedPrice == null)
            throw new ArgumentNullException(nameof(calculatedPrice));

        var amount = calculatedPrice.Amount;

        switch (strategy)
        {
            case PricingStrategy.PsychologicalPricing:
                // Use .99 ending for value perception
                amount = Math.Floor(amount) + 0.99m;
                break;

            case PricingStrategy.Premium:
                // Use .95 ending or round numbers for premium items
                if (amount >= 20)
                    amount = Math.Round(amount); // Round to whole dollar for high-end
                else
                    amount = Math.Floor(amount) + 0.95m;
                break;

            case PricingStrategy.RoundNumber:
                // Round to nearest dollar for simplicity
                amount = Math.Round(amount);
                break;

            case PricingStrategy.Value:
                // Use .49 or .99 endings for value items
                if (amount < 10)
                    amount = Math.Floor(amount) + 0.49m;
                else
                    amount = Math.Floor(amount) + 0.99m;
                break;
        }

        return new Money(amount, calculatedPrice.Currency);
    }

    /// <summary>
    /// Rounds price to acceptable increments following psychological pricing principles.
    /// Default strategy uses .99 endings for value perception.
    /// </summary>
    /// <param name="price">Price to round</param>
    /// <returns>Rounded price</returns>
    public Money RoundToAcceptablePrice(Money price)
    {
        if (price == null)
            throw new ArgumentNullException(nameof(price));

        // Use .99 ending as default
        var roundedAmount = Math.Floor(price.Amount) + 0.99m;
        
        // For prices under $1, use .49 or .99
        if (price.Amount < 1)
        {
            if (price.Amount < 0.50m)
                roundedAmount = 0.49m;
            else
                roundedAmount = 0.99m;
        }

        return new Money(roundedAmount, price.Currency);
    }

    // ===================================================================
    // A/B TESTING SUPPORT
    // ===================================================================

    /// <summary>
    /// Generates price variants for A/B testing to optimize pricing.
    /// Creates test prices at specified variation from base price.
    /// </summary>
    /// <param name="basePrice">Current price</param>
    /// <param name="variationPercentage">Percentage variation (e.g., 0.10 for ±10%)</param>
    /// <returns>Collection of test price variants</returns>
    /// <exception cref="ArgumentNullException">When basePrice is null</exception>
    /// <exception cref="ArgumentException">When variation percentage is invalid</exception>
    public IEnumerable<Money> GeneratePriceTestVariants(Money basePrice, decimal variationPercentage)
    {
        if (basePrice == null)
            throw new ArgumentNullException(nameof(basePrice));

        if (variationPercentage < 0 || variationPercentage > 0.5m)
            throw new ArgumentException("Variation percentage must be between 0 and 0.5 (50%)", nameof(variationPercentage));

        var variants = new List<Money>
        {
            basePrice, // Control price
            basePrice * (1 - variationPercentage), // Lower variant
            basePrice * (1 + variationPercentage)  // Higher variant
        };

        // Apply psychological pricing to each variant
        return variants.Select(v => ApplyPsychologicalPricing(v, PricingStrategy.PsychologicalPricing));
    }

    /// <summary>
    /// Analyzes A/B test results for pricing decisions.
    /// Compares control group vs test group performance to determine optimal pricing.
    /// </summary>
    /// <param name="controlGroupSales">Sales from control group (original price)</param>
    /// <param name="testGroupSales">Sales from test group (variant price)</param>
    /// <returns>Analysis results with recommendation</returns>
    public PriceTestResult AnalyzePriceTestResults(
        IEnumerable<SaleRecord> controlGroupSales,
        IEnumerable<SaleRecord> testGroupSales)
    {
        var controlSales = controlGroupSales?.ToList() ?? new List<SaleRecord>();
        var testSales = testGroupSales?.ToList() ?? new List<SaleRecord>();

        if (!controlSales.Any() || !testSales.Any())
        {
            return new PriceTestResult(
                ControlGroupRevenue: new Money(0, "USD"),
                TestGroupRevenue: new Money(0, "USD"),
                ControlGroupSalesCount: 0,
                TestGroupSalesCount: 0,
                RevenueChangePercentage: 0,
                SalesVelocityChange: 0,
                IsStatisticallySignificant: false,
                Recommendation: "Insufficient data for analysis"
            );
        }

        // Calculate metrics
        var controlRevenue = new Money(controlSales.Sum(s => s.SaleAmount.Amount), "USD");
        var testRevenue = new Money(testSales.Sum(s => s.SaleAmount.Amount), "USD");
        var controlCount = controlSales.Sum(s => s.QuantitySold);
        var testCount = testSales.Sum(s => s.QuantitySold);

        var revenueChange = ((testRevenue.Amount - controlRevenue.Amount) / controlRevenue.Amount) * 100;
        var velocityChange = ((decimal)testCount - controlCount) / controlCount * 100;

        // Simple statistical significance check (>10% revenue difference with >30 samples each)
        var isSignificant = Math.Abs(revenueChange) > 10 && controlCount > 30 && testCount > 30;

        var recommendation = revenueChange switch
        {
            > 5 when isSignificant => $"Adopt test price. Revenue increased by {revenueChange:F1}% with statistical significance.",
            < -5 when isSignificant => $"Keep control price. Test price decreased revenue by {Math.Abs(revenueChange):F1}%.",
            _ => "Results inconclusive. Consider extending test period or increasing sample size."
        };

        return new PriceTestResult(
            ControlGroupRevenue: controlRevenue,
            TestGroupRevenue: testRevenue,
            ControlGroupSalesCount: controlCount,
            TestGroupSalesCount: testCount,
            RevenueChangePercentage: revenueChange,
            SalesVelocityChange: velocityChange,
            IsStatisticallySignificant: isSignificant,
            Recommendation: recommendation
        );
    }

    // ===================================================================
    // PRIVATE HELPER METHODS
    // ===================================================================

    /// <summary>
    /// Validates dish input for pricing operations.
    /// </summary>
    private static void ValidateDish(Dish dish)
    {
        if (dish == null)
            throw new ArgumentNullException(nameof(dish));

        if (dish.DishPrice <= 0)
            throw new ArgumentException("Dish price must be positive", nameof(dish));
    }

    /// <summary>
    /// Calculates demand multiplier based on sales history.
    /// Used for demand-based pricing adjustments.
    /// </summary>
    private static decimal CalculateDemandMultiplier(IEnumerable<SaleRecord> salesHistory)
    {
        var totalSales = salesHistory.Sum(s => s.QuantitySold);

        if (totalSales >= HighDemandThreshold)
            return 1 + HighDemandPremium; // High demand: +20%
        
        if (totalSales <= LowDemandThreshold)
            return 1 - LowDemandDiscount;  // Low demand: -10%
        
        return 1.0m; // Medium demand: no change
    }
}
