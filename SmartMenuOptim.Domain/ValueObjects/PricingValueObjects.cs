// ===============================================================================================
// DOMAIN SERVICE VALUE OBJECTS - Developer Reference
// ===============================================================================================
// These types support Domain Services (e.g., MenuPricingService) following Clean Architecture.
//
// ✅ Domain Service Characteristics:
//    • Pure Domain Logic: No database access, external APIs, or infrastructure dependencies
//    • Stateless Operations: Functions operate on provided parameters without internal state
//    • Cross-Aggregate Operations: Combine data from multiple aggregates (Dish, Menu, Customer)
//    • Business Rules: Encode restaurant pricing strategies and industry-standard formulas
//    • Domain Language: Use restaurant/pricing terminology and domain objects
//
// ❌ Domain Services Do NOT Include:
//    • Data Retrieval (use Repositories in Infrastructure layer)
//    • External APIs (use Integration Services in Infrastructure layer)
//    • Data Persistence (use Repositories in Infrastructure layer)
//    • Email/SMS Notifications (use Notification Services in Infrastructure layer)
//
// See: SmartMenuOptim.Domain\docs\DOMAIN_SERVICE.md for complete guidelines
// ===============================================================================================

using SmartMenuOptim.Domain.Aggregates.DishAggregate;


namespace SmartMenuOptim.Domain.ValueObjects;

/// <summary>

/// Represents a recommendation for updating the price of a dish, including the current price, the suggested new price,
/// and the reasoning behind the recommendation.
/// </summary>
/// <param name="Dish">The dish for which the price recommendation is made.</param>
/// <param name="CurrentPrice">The current price of the dish.</param>
/// <param name="RecommendedPrice">The recommended new price for the dish.</param>
/// <param name="Reasoning">The explanation or rationale for the recommended price change.</param>
public record DishPriceRecommendation(
    Dish Dish,
    Money CurrentPrice,
    Money RecommendedPrice,
    string Reasoning);

/// <summary>
/// Represents a summary of price statistics for a menu, including average, median, lowest, highest prices, and the
/// price spread.
/// </summary>
/// <param name="AveragePrice">The average price of all menu items.</param>
/// <param name="MedianPrice">The median price among all menu items.</param>
/// <param name="LowestPrice">The lowest price found among all menu items.</param>
/// <param name="HighestPrice">The highest price found among all menu items.</param>
/// <param name="PriceSpread">The difference between the highest and lowest menu item prices.</param>
public record MenuPriceAnalysis(
    Money AveragePrice,
    Money MedianPrice,
    Money LowestPrice,
    Money HighestPrice,
    decimal PriceSpread);

/// <summary>
/// Represents the result of validating a price, including whether the price is valid, any validation errors, and an
/// optional suggested price.
/// </summary>
/// <param name="IsValid">A value indicating whether the price passed all validation checks. Set to <see langword="true"/> if the price is
/// valid; otherwise, <see langword="false"/>.</param>
/// <param name="ValidationErrors">A collection of error messages describing any validation failures. The collection is empty if the price is valid.</param>
/// <param name="SuggestedPrice">A suggested price that meets validation criteria, or <see langword="null"/> if no suggestion is available.</param>
public record PriceValidationResult(
    bool IsValid,
    IEnumerable<string> ValidationErrors,
    Money SuggestedPrice);

/// <summary>
/// Specifies the pricing strategy to apply when determining product prices.
/// </summary>
/// <remarks>Use this enumeration to select a pricing approach, such as psychological pricing (e.g., prices ending
/// in .99), round number pricing, premium pricing, or value-based pricing. The selected strategy influences how prices
/// are presented to customers and may affect perceived value or competitiveness.</remarks>
public enum PricingStrategy
{
    PsychologicalPricing,    // .99, .95 endings
    RoundNumber,             // Even dollar amounts
    Premium,                 // Higher price points
    Value                    // Competitive price points
}

/// <summary>
/// Specifies the four seasons of the year.
/// </summary>
/// <remarks>Use this enumeration to represent or select a specific season, such as for scheduling,
/// weather-related logic, or seasonal calculations. The values correspond to Spring, Summer, Fall, and Winter in the
/// Gregorian calendar.</remarks>
public enum Season
{
    Spring,
    Summer,
    Fall,
    Winter
}

/// <summary>
/// Represents a price anchoring strategy recommendation for a menu, including the anchor dish (highest-priced item),
/// the recommended anchor price, and the rationale for this pricing strategy.
/// </summary>
/// <param name="AnchorDish">The dish recommended to serve as the price anchor (typically the highest-priced item).</param>
/// <param name="RecommendedAnchorPrice">The recommended price for the anchor dish.</param>
/// <param name="Strategy">A description of the anchoring strategy and how it influences customer perception.</param>
/// <param name="ExpectedImpact">The anticipated effect of this anchoring strategy on overall sales and customer behavior.</param>
public record PriceAnchoringStrategy(
    Dish AnchorDish,
    Money RecommendedAnchorPrice,
    string Strategy,
    string ExpectedImpact);

/// <summary>
/// Represents the results of an A/B price test, comparing control group performance against test group performance.
/// </summary>
/// <param name="ControlGroupRevenue">Total revenue generated by the control group.</param>
/// <param name="TestGroupRevenue">Total revenue generated by the test group.</param>
/// <param name="ControlGroupSalesCount">Number of sales in the control group.</param>
/// <param name="TestGroupSalesCount">Number of sales in the test group.</param>
/// <param name="RevenueChangePercentage">Percentage change in revenue from control to test group.</param>
/// <param name="SalesVelocityChange">Change in sales velocity between control and test groups.</param>
/// <param name="IsStatisticallySignificant">Indicates whether the test results are statistically significant.</param>
/// <param name="Recommendation">A recommendation based on the test results (e.g., adopt test price, keep control price).</param>
public record PriceTestResult(
    Money ControlGroupRevenue,
    Money TestGroupRevenue,
    int ControlGroupSalesCount,
    int TestGroupSalesCount,
    decimal RevenueChangePercentage,
    decimal SalesVelocityChange,
    bool IsStatisticallySignificant,
    string Recommendation);


