using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Application.Contracts;

/// <summary>
/// Defines the contract for advanced pricing operations, including applying discounts, promotions,
/// and calculating prices for dishes, menus, and orders.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture:</strong></para>
/// <para>Interface defined in Application layer (port), implementations in Infrastructure layer (adapter).</para>
/// </remarks>
public interface IPricingService
{
    /// <summary>
    /// Calculates the final price for a dish after applying any active promotions.
    /// </summary>
    Money CalculateDishPrice(Dish dish, Promotion[] promotion);

    /// <summary>
    /// Calculates the total price for a menu consisting of multiple dishes with promotions.
    /// </summary>
    Money CalculateMenuPrice(Dish[] dishes, Promotion[] promotion);

    /// <summary>
    /// Calculates the total for an order after applying the specified pricing rules.
    /// </summary>
    Money CalculateOrderTotal(Order order, BusinessRule[] pricingRules);

    /// <summary>
    /// Applies a loyalty-based discount to the given base price.
    /// </summary>
    Money ApplyLoyaltyDiscount(Money basePrice, CustomerLoyalty loyaltyLevel);

    /// <summary>
    /// Applies a promotion discount to the given base price.
    /// </summary>
    Money ApplyPromotion(Money basePrice, Promotion promotion);
}
