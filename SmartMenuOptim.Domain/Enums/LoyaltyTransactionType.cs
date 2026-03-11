namespace SmartMenuOptim.Domain.Enums;

/// <summary>
/// Defines the types of loyalty transactions that can occur.
/// </summary>
/// <remarks>
/// Used to categorize and track different sources of loyalty point changes.
/// Each type represents a distinct business event that affects customer points.
/// </remarks>
public enum LoyaltyTransactionType
{
    /// <summary>
    /// Points earned from placing an order.
    /// Typically linked to an Order via OrderId.
    /// </summary>
    OrderEarning,

    /// <summary>
    /// Points redeemed for a reward.
    /// Results in negative PointsChange value.
    /// </summary>
    RewardRedemption,

    /// <summary>
    /// Bonus points awarded.
    /// Examples: Welcome bonus, birthday bonus, promotional bonus.
    /// </summary>
    Bonus,

    /// <summary>
    /// Manual points adjustment by staff or system.
    /// Can be positive or negative based on adjustment reason.
    /// </summary>
    Adjustment,

    /// <summary>
    /// Points earned from referral program.
    /// Awarded when customer refers new customers.
    /// </summary>
    Referral,

    /// <summary>
    /// Points expired due to inactivity or policy.
    /// Results in negative PointsChange value.
    /// </summary>
    Expiration
}
