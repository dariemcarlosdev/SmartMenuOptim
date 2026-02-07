using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

// Note: Promotion is in the global namespace (no namespace declaration in its file)

namespace SmartMenuOptim.Domain.Services;

/// <summary>
/// Domain service for determining customer and dish eligibility for promotions based on business rules.
/// </summary>
/// <remarks>
/// <para><strong>Domain Service - Pure Domain Logic</strong></para>
/// 
/// This service evaluates promotion eligibility criteria for customers and dishes,
/// ensuring promotions are applied correctly according to business rules.
/// 
/// <para><strong>Domain Service Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Stateless:</strong> No internal state, operates purely on input parameters</description></item>
///   <item><description><strong>Pure Domain Logic:</strong> Eligibility evaluation without infrastructure dependencies</description></item>
///   <item><description><strong>Cross-Aggregate Operations:</strong> Works with Promotion, Customer, Dish, Order data</description></item>
///   <item><description><strong>Business Rules:</strong> Implements complex promotion eligibility criteria</description></item>
///   <item><description><strong>Domain Language:</strong> Uses ubiquitous language (Promotion, Eligibility, Customer, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Eligibility Criteria:</strong></para>
/// <list type="bullet">
///   <item><description>Customer loyalty tier requirements</description></item>
///   <item><description>Purchase history and frequency</description></item>
///   <item><description>Date and time restrictions</description></item>
///   <item><description>Minimum purchase amount requirements</description></item>
///   <item><description>First-time customer promotions</description></item>
///   <item><description>Category-specific promotions</description></item>
///   <item><description>Seasonal and event-based eligibility</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// var eligibilityService = new PromotionEligibilityService();
/// var isEligible = eligibilityService.IsCustomerEligibleForPromotion(customer, promotion, orderTotal);
/// var eligibleDishes = eligibilityService.GetEligibleDishesForPromotion(promotion, allDishes);
/// </code>
/// </remarks>
public class PromotionEligibilityService
{
    private readonly ILogger<PromotionEligibilityService> _logger;

    // ===================================================================
    // CONSTANTS - BUSINESS RULES
    // ===================================================================

    private const int MinimumPurchaseHistoryDays = 30; // Days to look back for purchase history
    private const int FrequentCustomerThreshold = 5; // Minimum orders to be considered frequent
    private const decimal MinimumOrderAmountForPromotion = 20.00m; // Minimum order amount in USD

    // ===================================================================
    // CONSTRUCTOR
    // ===================================================================

    /// <summary>
    /// Initializes a new instance of the TableAvailabilityService with logging support.
    /// </summary>
    /// <param name="logger">Logger for tracking availability operations (optional for testing).</param>
    public PromotionEligibilityService(ILogger<PromotionEligibilityService>? logger = null)
    {
        _logger = logger ?? NullLogger<PromotionEligibilityService>.Instance; // Use NullLogger if none provided
    }

    /// <summary>
    /// Represents the result of an eligibility check.
    /// </summary>
    public class EligibilityResult
    {
        public bool IsEligible { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> FailedCriteria { get; set; } = new();
    }

    /// <summary>
    /// Determines if a customer is eligible for a specific promotion.
    /// </summary>
    /// <param name="customer">The customer to evaluate.</param>
    /// <param name="promotion">The promotion to check eligibility for.</param>
    /// <param name="orderTotal">Current order total amount.</param>
    /// <param name="orderDate">Date of the order (default: current date/time).</param>
    /// <returns>Eligibility result indicating if the customer qualifies and why.</returns>
    /// <exception cref="ArgumentNullException">Thrown when customer or promotion is null.</exception>
    public EligibilityResult IsCustomerEligibleForPromotion(
        Customer customer,
        Promotion promotion,
        decimal orderTotal,
        DateTime? orderDate = null)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));
        
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));

        var checkDate = orderDate ?? DateTime.UtcNow;
        var failedCriteria = new List<string>();

        // Check if promotion is currently active
        if (!promotion.IsActive())
        {
            return new EligibilityResult
            {
                IsEligible = false,
                Reason = "Promotion is not currently active",
                FailedCriteria = new List<string> { "Active status" }
            };
        }

        // Check date validity
        if (checkDate < promotion.ValidFrom || checkDate > promotion.ValidTo)
        {
            failedCriteria.Add("Date range");
        }

        // Check minimum order amount
        if (orderTotal < MinimumOrderAmountForPromotion)
        {
            failedCriteria.Add($"Minimum order amount (${MinimumOrderAmountForPromotion})");
        }

        // If any criteria failed, return not eligible
        if (failedCriteria.Any())
        {
            return new EligibilityResult
            {
                IsEligible = false,
                Reason = $"Failed {failedCriteria.Count} eligibility criteria",
                FailedCriteria = failedCriteria
            };
        }

        return new EligibilityResult
        {
            IsEligible = true,
            Reason = "Customer meets all eligibility criteria"
        };
    }

    /// <summary>
    /// Determines if a customer is eligible based on their loyalty tier.
    /// </summary>
    /// <param name="customerLoyalty">Customer's loyalty information.</param>
    /// <param name="requiredTierName">Required loyalty tier name.</param>
    /// <returns>True if customer meets loyalty tier requirement, false otherwise.</returns>
    public bool IsEligibleByLoyaltyTier(CustomerLoyalty? customerLoyalty, string requiredTierName)
    {
        if (customerLoyalty == null || string.IsNullOrWhiteSpace(requiredTierName))
            return false;

        // Customer loyalty may not have tiers implemented yet
        // This is a placeholder for future tier implementation
        return true; // Simplified - always eligible for now
    }

    /// <summary>
    /// Gets all dishes that are eligible for a specific promotion.
    /// </summary>
    /// <param name="promotion">The promotion to evaluate.</param>
    /// <param name="availableDishes">All available dishes.</param>
    /// <returns>List of dishes eligible for the promotion.</returns>
    /// <remarks>
    /// Note: Current Promotion entity doesn't have ApplicableDishes property.
    /// This implementation returns all dishes - can be extended when the feature is added.
    /// </remarks>
    public List<Dish> GetEligibleDishesForPromotion(
        Promotion promotion,
        IEnumerable<Dish> availableDishes)
    {
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));
        
        if (availableDishes == null)
            throw new ArgumentNullException(nameof(availableDishes));

        var dishesList = availableDishes.ToList();

        // Current Promotion entity doesn't restrict to specific dishes
        // Return all dishes as eligible (can be extended in future)
        return dishesList;
    }

    /// <summary>
    /// Checks if a customer is a first-time customer eligible for new customer promotions.
    /// </summary>
    /// <param name="customer">The customer to evaluate.</param>
    /// <param name="orderHistory">Customer's order history.</param>
    /// <returns>True if this is their first order, false otherwise.</returns>
    public bool IsFirstTimeCustomer(Customer customer, IEnumerable<Order> orderHistory)
    {
        _logger.LogDebug("Checking first-time customer status for Customer {CustomerId}", customer?.Id);

        if (customer == null)
        {
            _logger.LogError("IsFirstTimeCustomer failed: customer parameter is null");
            throw new ArgumentNullException(nameof(customer));
        }
        
        if (orderHistory == null)
        {
            _logger.LogError("IsFirstTimeCustomer failed: orderHistory parameter is null");
            throw new ArgumentNullException(nameof(orderHistory));
        }

        var isFirstTime = !orderHistory.Any();

        _logger.LogInformation(
            "Customer {CustomerId} first-time status: {IsFirstTime}",
            customer.Id, isFirstTime ? "YES (First-time customer)" : "NO (Returning customer)");

        return isFirstTime;
    }

    /// <summary>
    /// Determines if a customer is a frequent customer eligible for loyalty promotions.
    /// </summary>
    /// <param name="customer">The customer to evaluate.</param>
    /// <param name="orderHistory">Customer's order history.</param>
    /// <param name="lookbackDays">Number of days to look back (default: 30).</param>
    /// <returns>True if customer meets frequent customer criteria, false otherwise.</returns>
    public bool IsFrequentCustomer(
        Customer customer,
        IEnumerable<Order> orderHistory,
        int lookbackDays = MinimumPurchaseHistoryDays)
    {
        _logger.LogDebug(
            "Checking frequent customer status for Customer {CustomerId} (lookback: {LookbackDays} days)",
            customer?.Id, lookbackDays);

        if (customer == null)
        {
            _logger.LogError("IsFrequentCustomer failed: customer parameter is null");
            throw new ArgumentNullException(nameof(customer));
        }
        
        if (orderHistory == null)
        {
            _logger.LogError("IsFrequentCustomer failed: orderHistory parameter is null");
            throw new ArgumentNullException(nameof(orderHistory));
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-lookbackDays);
        var recentOrders = orderHistory.Count(o => o.OrderDate >= cutoffDate);
        var isFrequent = recentOrders >= FrequentCustomerThreshold;

        _logger.LogInformation(
            "Customer {CustomerId} frequent status: {IsFrequent} ({RecentOrders} orders in last {LookbackDays} days, threshold: {Threshold})",
            customer.Id, isFrequent ? "YES (Frequent)" : "NO (Not frequent)", 
            recentOrders, lookbackDays, FrequentCustomerThreshold);

        return isFrequent;
    }

    /// <summary>
    /// Checks if the current date/time falls within promotion's valid time windows.
    /// </summary>
    /// <param name="promotion">The promotion to check.</param>
    /// <param name="checkDateTime">The date/time to validate (default: current date/time).</param>
    /// <returns>True if within valid time window, false otherwise.</returns>
    public bool IsWithinValidTimeWindow(Promotion promotion, DateTime? checkDateTime = null)
    {
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));

        var checkDate = checkDateTime ?? DateTime.UtcNow;

        // Check date range
        if (checkDate < promotion.ValidFrom || checkDate > promotion.ValidTo)
            return false;

        // Additional time-of-day checks could be added here
        // For example: Happy hour promotions (5 PM - 7 PM)
        // This would require additional properties on the Promotion entity

        return true;
    }

    /// <summary>
    /// Validates if a promotion can still be used based on usage limits.
    /// </summary>
    /// <param name="promotion">The promotion to validate.</param>
    /// <returns>True if promotion has remaining uses, false otherwise.</returns>
    /// <remarks>
    /// Note: Current Promotion entity doesn't track usage counts.
    /// This returns true - can be extended when usage tracking is added.
    /// </remarks>
    public bool HasRemainingUses(Promotion promotion)
    {
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));

        // Promotion entity doesn't track usage count currently
        // Return true (unlimited uses) - can be extended in future
        return true;
    }

    /// <summary>
    /// Calculates the discount amount for an eligible promotion.
    /// </summary>
    /// <param name="promotion">The promotion to apply.</param>
    /// <param name="orderTotal">The total order amount.</param>
    /// <returns>The discount amount to be applied.</returns>
    public decimal CalculateDiscountAmount(Promotion promotion, decimal orderTotal)
    {
        if (promotion == null)
            throw new ArgumentNullException(nameof(promotion));
        
        if (orderTotal <= 0)
            return 0m;

        // Promotion uses fixed discount amount (not percentage)
        var discountAmount = promotion.DiscountAmount;

        // Don't exceed order total
        if (discountAmount > orderTotal)
        {
            discountAmount = orderTotal;
        }

        return Math.Round(discountAmount, 2);
    }

    /// <summary>
    /// Gets all currently active promotions that a customer is eligible for.
    /// </summary>
    /// <param name="customer">The customer to evaluate.</param>
    /// <param name="promotions">All available promotions.</param>
    /// <param name="orderTotal">Current order total.</param>
    /// <returns>List of promotions the customer is eligible for.</returns>
    public List<Promotion> GetEligiblePromotions(
        Customer customer,
        IEnumerable<Promotion> promotions,
        decimal orderTotal)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));
        
        if (promotions == null)
            throw new ArgumentNullException(nameof(promotions));

        var eligiblePromotions = new List<Promotion>();

        foreach (var promotion in promotions)
        {
            var eligibility = IsCustomerEligibleForPromotion(customer, promotion, orderTotal);
            if (eligibility.IsEligible)
            {
                eligiblePromotions.Add(promotion);
            }
        }

        return eligiblePromotions;
    }

    /// <summary>
    /// Finds the best promotion for a customer based on maximum discount.
    /// </summary>
    /// <param name="customer">The customer to evaluate.</param>
    /// <param name="promotions">All available promotions.</param>
    /// <param name="orderTotal">Current order total.</param>
    /// <returns>The promotion that provides the maximum discount, or null if none are eligible.</returns>
    public Promotion? FindBestPromotion(
        Customer customer,
        IEnumerable<Promotion> promotions,
        decimal orderTotal)
    {
        var eligiblePromotions = GetEligiblePromotions(customer, promotions, orderTotal);

        if (!eligiblePromotions.Any())
            return null;

        // Find promotion with maximum discount
        return eligiblePromotions
            .OrderByDescending(p => CalculateDiscountAmount(p, orderTotal))
            .FirstOrDefault();
    }
}
