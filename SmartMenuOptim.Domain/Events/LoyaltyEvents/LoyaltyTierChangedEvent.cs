using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Events.LoyaltyEvents;

/// <summary>
/// Domain event raised when a customer's loyalty tier changes (upgrade or downgrade).
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the CustomerLoyalty aggregate when a customer's tier 
/// level changes due to point balance changes, tier evaluation, or manual adjustment.</para>
/// 
/// <para><strong>Tier Progression (SmartMenuOptimizer Standard):</strong></para>
/// <list type="bullet">
///     <item><description><strong>Bronze:</strong> 0-99 points (entry level)</description></item>
///     <item><description><strong>Silver:</strong> 100-499 points (10% discount tier)</description></item>
///     <item><description><strong>Gold:</strong> 500-999 points (15% discount tier)</description></item>
///     <item><description><strong>Platinum:</strong> 1000+ points (20% discount tier + VIP benefits)</description></item>
/// </list>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>NotificationHandler:</strong> Congratulates customer on tier upgrade (or sympathizes on downgrade)</description></item>
///     <item><description><strong>BenefitsHandler:</strong> Activates/deactivates tier-specific benefits</description></item>
///     <item><description><strong>DiscountHandler:</strong> Updates applicable discount percentage</description></item>
///     <item><description><strong>BadgeHandler:</strong> Updates customer profile badges/achievements</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Tracks tier movement patterns</description></item>
///     <item><description><strong>MarketingHandler:</strong> Triggers tier-specific marketing campaigns</description></item>
/// </list>
/// 
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
///     <item><description>Tier upgrades are immediate upon reaching point threshold</description></item>
///     <item><description>Tier downgrades may have a grace period (configurable by restaurant)</description></item>
///     <item><description>Points expiration may trigger tier re-evaluation</description></item>
///     <item><description>Manual tier adjustments require staff authorization</description></item>
/// </list>
/// 
/// <para><strong>Tier Benefits Example:</strong></para>
/// <code>
/// Bronze:   Base benefits, newsletter
/// Silver:   10% discount, birthday reward
/// Gold:     15% discount, birthday reward, priority seating
/// Platinum: 20% discount, birthday reward, priority seating, exclusive menu items, free delivery
/// </code>
/// </remarks>
public sealed class LoyaltyTierChangedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the customer loyalty record.
    /// </summary>
    public int CustomerLoyaltyId { get; init; }

    /// <summary>
    /// Gets the customer identifier.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the customer's previous tier before the change.
    /// </summary>
    public string PreviousTier { get; init; } = string.Empty;

    /// <summary>
    /// Gets the customer's new tier after the change.
    /// </summary>
    public string NewTier { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this was a tier upgrade (true) or downgrade (false).
    /// </summary>
    public bool IsUpgrade { get; init; }

    /// <summary>
    /// Gets the current point balance that triggered the tier change.
    /// </summary>
    public int CurrentPointBalance { get; init; }

    /// <summary>
    /// Gets the reason for the tier change.
    /// </summary>
    public TierChangeReason ChangeReason { get; init; }

    /// <summary>
    /// Gets the discount percentage for the previous tier.
    /// </summary>
    public decimal PreviousTierDiscountPercent { get; init; }

    /// <summary>
    /// Gets the discount percentage for the new tier.
    /// </summary>
    public decimal NewTierDiscountPercent { get; init; }

    /// <summary>
    /// Gets the benefits gained (if upgrade) or lost (if downgrade).
    /// </summary>
    public List<string> BenefitsChanged { get; init; } = new();

    /// <summary>
    /// Gets the staff member ID if this was a manual tier adjustment.
    /// </summary>
    public int? AdjustedByStaffId { get; init; }

    /// <summary>
    /// Gets any notes associated with a manual tier adjustment.
    /// </summary>
    public string? AdjustmentNotes { get; init; }

    /// <summary>
    /// Gets the next tier the customer can achieve (if any).
    /// </summary>
    public string? NextTierName { get; init; }

    /// <summary>
    /// Gets the points required to reach the next tier (if any).
    /// </summary>
    public int? PointsToNextTier { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyTierChangedEvent"/> class.
    /// </summary>
    public LoyaltyTierChangedEvent(
        int customerLoyaltyId,
        int customerId,
        int restaurantId,
        string previousTier,
        string newTier,
        int currentPointBalance,
        TierChangeReason changeReason,
        decimal previousTierDiscountPercent = 0,
        decimal newTierDiscountPercent = 0,
        List<string>? benefitsChanged = null,
        int? adjustedByStaffId = null,
        string? adjustmentNotes = null,
        string? nextTierName = null,
        int? pointsToNextTier = null)
    {
        CustomerLoyaltyId = customerLoyaltyId;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        PreviousTier = previousTier;
        NewTier = newTier;
        IsUpgrade = CompareTiers(newTier, previousTier) > 0;
        CurrentPointBalance = currentPointBalance;
        ChangeReason = changeReason;
        PreviousTierDiscountPercent = previousTierDiscountPercent;
        NewTierDiscountPercent = newTierDiscountPercent;
        BenefitsChanged = benefitsChanged ?? new List<string>();
        AdjustedByStaffId = adjustedByStaffId;
        AdjustmentNotes = adjustmentNotes;
        NextTierName = nextTierName;
        PointsToNextTier = pointsToNextTier;
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private LoyaltyTierChangedEvent() { }

    /// <summary>
    /// Compares two tier names and returns relative ranking.
    /// </summary>
    private static int CompareTiers(string tier1, string tier2)
    {
        var tierOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bronze", 1 },
            { "Silver", 2 },
            { "Gold", 3 },
            { "Platinum", 4 }
        };

        tierOrder.TryGetValue(tier1, out var rank1);
        tierOrder.TryGetValue(tier2, out var rank2);

        return rank1.CompareTo(rank2);
    }
}

/// <summary>
/// Defines the reason for a loyalty tier change.
/// </summary>
public enum TierChangeReason
{
    /// <summary>Points accumulation reached upgrade threshold.</summary>
    PointsAccumulation = 0,

    /// <summary>Points redemption dropped below tier threshold.</summary>
    PointsRedemption = 1,

    /// <summary>Points expired causing tier re-evaluation.</summary>
    PointsExpiration = 2,

    /// <summary>Manual adjustment by staff member.</summary>
    ManualAdjustment = 3,

    /// <summary>Promotional tier upgrade (temporary or permanent).</summary>
    Promotion = 4,

    /// <summary>New customer sign-up bonus tier assignment.</summary>
    SignUpBonus = 5,

    /// <summary>Tier reset due to inactivity.</summary>
    InactivityReset = 6,

    /// <summary>Annual tier review/recalculation.</summary>
    AnnualReview = 7
}
