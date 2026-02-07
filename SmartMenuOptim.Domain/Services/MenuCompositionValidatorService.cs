using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Validates menu composition against business rules and quality standards.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Contains only business rule validation, no infrastructure dependencies</description></item>
///   <item><description><strong>Business-Focused:</strong> Implements restaurant menu quality standards and composition rules</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description>Validate menu before publishing to customers</description></item>
///   <item><description>Check menu quality during creation/modification</description></item>
///   <item><description>Ensure menu meets restaurant strategic goals (variety, pricing)</description></item>
///   <item><description>Identify menu composition issues (duplicates, imbalanced categories)</description></item>
///   <item><description>Verify seasonal menu items are current</description></item>
/// </list>
/// 
/// <para><strong>Business Rules Enforced:</strong></para>
/// <list type="number">
///   <item><description><strong>Minimum Variety:</strong> Menu must have at least 3 dishes to be considered adequate</description></item>
///   <item><description><strong>Category Balance:</strong> No single category should dominate (max 70% of menu)</description></item>
///   <item><description><strong>Price Range:</strong> Menu should have diverse price points (at least 2 different price levels)</description></item>
///   <item><description><strong>No Duplicates:</strong> Each dish should appear only once on the menu</description></item>
///   <item><description><strong>Seasonal Validity:</strong> Seasonal items must be appropriate for current time period</description></item>
/// </list>
/// 
/// <para><strong>Example:</strong></para>
/// <code>
/// var validator = new MenuCompositionValidatorService();
/// var result = validator.ValidateMenuComposition(dinnerMenu);
/// 
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"Error: {error}");
///     }
/// }
/// 
/// // Check specific aspects
/// bool hasVariety = validator.HasAdequateVariety(dinnerMenu);
/// bool balancedPricing = validator.HasBalancedPricePoints(dinnerMenu);
/// </code>
/// </remarks>
public class MenuCompositionValidatorService
{
    // === Business Rule Constants ===

    /// <summary>
    /// Minimum number of dishes required for adequate menu variety.
    /// </summary>
    private const int MinimumDishCount = 3;

    /// <summary>
    /// Maximum percentage any single category can represent (70%).
    /// Prevents category dominance and ensures variety.
    /// </summary>
    private const decimal MaxCategoryDominancePercentage = 0.70m;

    /// <summary>
    /// Minimum number of distinct price levels required for balanced pricing.
    /// Ensures menu appeals to different customer budgets.
    /// </summary>
    private const int MinimumPriceLevels = 2;

    /// <summary>
    /// Price tolerance for grouping dishes into same price level (10% variance).
    /// Dishes within this range are considered same price point.
    /// </summary>
    private const decimal PriceGroupingTolerancePercentage = 0.10m;

    /// <summary>
    /// Number of months a seasonal item can be valid (3 months per season).
    /// </summary>
    private const int SeasonalValidityMonths = 3;

    // === Public Methods (Domain Operations) ===

    /// <summary>
    /// Validates the complete menu composition against all business rules.
    /// </summary>
    /// <param name="menu">The menu to validate. Cannot be null.</param>
    /// <returns>Validation result with errors, warnings, and summary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when menu is null.</exception>
    /// <remarks>
    /// Performs comprehensive validation including:
    /// - Minimum dish variety check
    /// - Category balance validation
    /// - Price range diversity check
    /// - Duplicate dish detection
    /// - Seasonal item currency validation
    /// </remarks>
    public MenuValidationResult ValidateMenuComposition(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        var errors = new List<string>();
        var warnings = new List<string>();

        // Set of validation rules to apply - can be extended in future for more complex scenarios.
        // All rules are applied in sequence, and all errors/warnings are collected for comprehensive feedback.
        // They all should be independent and not short-circuit, to provide full insight into menu issues.
        // This allows restaurant operators to see all potential issues at once and prioritize fixes accordingly.
        // They all have to pass to consider the menu valid, but warnings do not prevent validity - they are informational for improvement.

        // Validate basic menu requirements
        ValidateBasicRequirements(menu, errors);

        // Validate dish variety
        ValidateDishVariety(menu, errors, warnings);

        // Validate category balance
        ValidateCategoryBalance(menu, errors, warnings);

        // Validate price diversity
        ValidatePriceDiversity(menu, errors, warnings);

        // Validate no duplicate dishes
        ValidateNoDuplicates(menu, errors);

        // Validate seasonal items (warning only)
        ValidateSeasonalItems(menu, warnings);

        // Return result
        return errors.Any()
            ? MenuValidationResult.Failure(errors, warnings)
            : MenuValidationResult.Success(warnings);
    }

    /// <summary>
    /// Checks if the menu has adequate variety of dishes.
    /// </summary>
    /// <param name="menu">The menu to check. Cannot be null.</param>
    /// <returns>True if menu has at least the minimum required dishes; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when menu is null.</exception>
    /// <remarks>
    /// Business Rule: A menu must have at least 3 dishes to provide adequate choice.
    /// This ensures customers have meaningful options and the menu doesn't appear limited.
    /// </remarks>
    public bool HasAdequateVariety(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        var activeDishes = GetActiveDishes(menu);
        return activeDishes.Count >= MinimumDishCount;
    }

    /// <summary>
    /// Checks if the menu has balanced price points across different budget levels.
    /// </summary>
    /// <param name="menu">The menu to check. Cannot be null.</param>
    /// <returns>True if menu has diverse price levels; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when menu is null.</exception>
    /// <remarks>
    /// Business Rule: Menu should have at least 2 distinct price levels to appeal to different budgets.
    /// Prices within 10% of each other are considered the same level.
    /// This ensures the menu is accessible to various customer segments.
    /// </remarks>
    public bool HasBalancedPricePoints(Menu menu)
    {
        if (menu == null)
            throw new ArgumentNullException(nameof(menu));

        var prices = GetEffectivePrices(menu);
        if (prices.Count < MinimumDishCount)
            return false;

        var priceLevels = GroupPricesByLevel(prices);
        return priceLevels.Count >= MinimumPriceLevels;
    }

    // === Private Helper Methods (Internal Validation Logic) ===

    /// <summary>
    /// Validates basic menu requirements (not deleted, has dishes).
    /// </summary>
    private void ValidateBasicRequirements(Menu menu, List<string> errors)
    {
        if (menu.IsDeleted)
        {
            errors.Add("Cannot validate a deleted menu");
            return;
        }

        if (menu.MenuDishes == null || !menu.MenuDishes.Any())
        {
            errors.Add("Menu must have at least one dish association");
        }
    }

    /// <summary>
    /// Validates that menu has minimum required dish variety.
    /// </summary>
    private void ValidateDishVariety(Menu menu, List<string> errors, List<string> warnings)
    {
        var activeDishes = GetActiveDishes(menu);

        if (activeDishes.Count == 0)
        {
            errors.Add("Menu has no active dishes available for ordering");
            return;
        }

        if (activeDishes.Count < MinimumDishCount)
        {
            errors.Add($"Menu must have at least {MinimumDishCount} active dishes (currently has {activeDishes.Count})");
        }

        // Warning for limited variety
        if (activeDishes.Count >= MinimumDishCount && activeDishes.Count < 5)
        {
            warnings.Add($"Menu has limited variety with only {activeDishes.Count} dishes. Consider adding more options.");
        }
    }

    /// <summary>
    /// Validates that no single category dominates the menu.
    /// </summary>
    private void ValidateCategoryBalance(Menu menu, List<string> errors, List<string> warnings)
    {
        var activeDishes = GetActiveDishes(menu);
        if (activeDishes.Count < MinimumDishCount)
            return; // Skip if not enough dishes

        var categoryGroups = activeDishes
            .Where(md => md.Dish?.Category != null)
            .GroupBy(md => md.Dish!.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                CategoryName = g.First().Dish!.Category!.Name,
                DishCount = g.Count(),
                Percentage = (decimal)g.Count() / activeDishes.Count
            })
            .ToList();

        if (!categoryGroups.Any())
        {
            warnings.Add("Unable to validate category balance - dishes missing category information");
            return;
        }

        // Check for category dominance
        var dominantCategories = categoryGroups
            .Where(cg => cg.Percentage > MaxCategoryDominancePercentage)
            .ToList();

        foreach (var category in dominantCategories)
        {
            errors.Add($"Category '{category.CategoryName}' dominates the menu with {category.Percentage:P0} of dishes (max allowed: {MaxCategoryDominancePercentage:P0})");
        }

        // Warning for single category menu
        if (categoryGroups.Count == 1)
        {
            warnings.Add("Menu contains dishes from only one category. Consider adding variety across multiple categories.");
        }
    }

    /// <summary>
    /// Validates that menu has diverse price points.
    /// </summary>
    private void ValidatePriceDiversity(Menu menu, List<string> errors, List<string> warnings)
    {
        var prices = GetEffectivePrices(menu);
        if (prices.Count < MinimumDishCount)
            return; // Skip if not enough dishes

        var priceLevels = GroupPricesByLevel(prices);

        if (priceLevels.Count < MinimumPriceLevels)
        {
            errors.Add($"Menu must have at least {MinimumPriceLevels} distinct price levels (currently has {priceLevels.Count}). Add dishes at different price points.");
        }

        // Warning for narrow price range
        var minPrice = prices.Min();
        var maxPrice = prices.Max();
        var priceRange = maxPrice - minPrice;

        if (priceRange < minPrice * 0.5m) // Less than 50% range
        {
            warnings.Add($"Menu has a narrow price range (${minPrice:F2} - ${maxPrice:F2}). Consider adding premium or budget options.");
        }
    }

    /// <summary>
    /// Validates that no dish appears multiple times on the menu.
    /// </summary>
    private void ValidateNoDuplicates(Menu menu, List<string> errors)
    {
        var activeDishes = GetActiveDishes(menu);
        
        var duplicates = activeDishes
            .GroupBy(md => md.DishId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            var dishName = duplicate.First().Dish?.Name ?? $"DishId {duplicate.Key}";
            errors.Add($"Dish '{dishName}' appears {duplicate.Count()} times on the menu. Each dish should appear only once.");
        }
    }

    /// <summary>
    /// Validates that seasonal items are appropriate for current period.
    /// </summary>
    /// <remarks>
    /// This generates warnings only, as seasonal validation is informational.
    /// Assumes dish names or descriptions contain seasonal indicators like "Summer", "Winter", etc.
    /// </remarks>
    private void ValidateSeasonalItems(Menu menu, List<string> warnings)
    {
        var currentMonth = DateTime.UtcNow.Month;
        var currentSeason = GetSeason(currentMonth);

        var activeDishes = GetActiveDishes(menu);

        foreach (var menuDish in activeDishes)
        {
            if (menuDish.Dish == null)
                continue;

            var dishSeason = DetectSeasonFromText(menuDish.Dish.Name, menuDish.Dish.Description);
            
            if (dishSeason.HasValue && dishSeason.Value != currentSeason)
            {
                warnings.Add($"Dish '{menuDish.Dish.Name}' appears to be a {dishSeason.Value} item but current season is {currentSeason}");
            }
        }
    }

    /// <summary>
    /// Gets active (non-deleted) menu dish associations with loaded dish data.
    /// </summary>
    private List<MenuDish> GetActiveDishes(Menu menu)
    {
        return menu.MenuDishes
            .Where(md => !md.IsDeleted && md.Dish != null && !md.Dish.IsDeleted)
            .ToList();
    }

    /// <summary>
    /// Gets effective prices for all menu dishes (special price or base price).
    /// </summary>
    private List<decimal> GetEffectivePrices(Menu menu)
    {
        return GetActiveDishes(menu)
            .Select(md => md.SpecialPrice ?? md.Dish!.DishPrice)
            .Where(price => price > 0)
            .OrderBy(price => price)
            .ToList();
    }

    /// <summary>
    /// Groups prices into levels based on tolerance threshold.
    /// </summary>
    /// <remarks>
    /// Prices within 10% of each other are considered same price level.
    /// Example: $10.00, $10.50, $11.00 would be one level (within 10% of $10).
    /// </remarks>
    private List<List<decimal>> GroupPricesByLevel(List<decimal> prices)
    {
        if (!prices.Any())
            return new List<List<decimal>>();

        var sortedPrices = prices.OrderBy(p => p).ToList();
        var priceLevels = new List<List<decimal>> { new List<decimal> { sortedPrices[0] } };

        for (int i = 1; i < sortedPrices.Count; i++)
        {
            var currentPrice = sortedPrices[i];
            var currentLevelBase = priceLevels.Last().First();
            var tolerance = currentLevelBase * PriceGroupingTolerancePercentage;

            if (currentPrice <= currentLevelBase + tolerance)
            {
                // Same price level
                priceLevels.Last().Add(currentPrice);
            }
            else
            {
                // New price level
                priceLevels.Add(new List<decimal> { currentPrice });
            }
        }

        return priceLevels;
    }

    /// <summary>
    /// Determines the season based on month.
    /// </summary>
    private Season GetSeason(int month)
    {
        return month switch
        {
            12 or 1 or 2 => Season.Winter,
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Fall,
            _ => Season.Unknown
        };
    }

    /// <summary>
    /// Attempts to detect season from dish name or description.
    /// </summary>
    private Season? DetectSeasonFromText(string name, string? description)
    {
        var text = $"{name} {description}".ToLowerInvariant();

        if (text.Contains("winter") || text.Contains("holiday"))
            return Season.Winter;
        if (text.Contains("spring"))
            return Season.Spring;
        if (text.Contains("summer"))
            return Season.Summer;
        if (text.Contains("fall") || text.Contains("autumn"))
            return Season.Fall;

        return null; // Not seasonal or can't detect
    }

    /// <summary>
    /// Represents the four seasons for seasonal validation.
    /// </summary>
    private enum Season
    {
        Unknown = 0,
        Winter = 1,
        Spring = 2,
        Summer = 3,
        Fall = 4
    }
}
