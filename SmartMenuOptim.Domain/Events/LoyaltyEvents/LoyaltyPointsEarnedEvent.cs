namespace SmartMenuOptim.Domain.Events.LoyaltyEvents;

/// <summary>
/// Domain event raised when a customer earns loyalty points through purchases or promotions.
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the CustomerLoyalty aggregate when points are added to 
/// a customer's loyalty account through various earning mechanisms.</para>
/// 
/// <para><strong>Point Earning Sources:</strong></para>
/// <list type="bullet">
///     <item><description><strong>Purchase:</strong> Standard points earned from order total (e.g., 1 point per $1)</description></item>
///     <item><description><strong>Bonus:</strong> Extra points from promotions or special offers</description></item>
///     <item><description><strong>Referral:</strong> Points earned from referring new customers</description></item>
///     <item><description><strong>Birthday:</strong> Bonus points awarded on customer's birthday</description></item>
///     <item><description><strong>Review:</strong> Points for leaving a product/restaurant review</description></item>
///     <item><description><strong>Adjustment:</strong> Manual adjustment by staff (positive adjustment)</description></item>
/// </list>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>TierEvaluationHandler:</strong> Checks if points earned trigger tier upgrade</description></item>
///     <item><description><strong>NotificationHandler:</strong> Notifies customer of points earned</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Updates loyalty program metrics</description></item>
///     <item><description><strong>AuditHandler:</strong> Logs point transaction for compliance</description></item>
///     <item><description><strong>MilestoneHandler:</strong> Checks for achievement milestones</description></item>
/// </list>
/// 
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
///     <item><description>Points earned must be positive</description></item>
///     <item><description>Points may have an expiration date based on restaurant policy</description></item>
///     <item><description>Bonus multipliers may apply during promotional periods</description></item>
/// </list>
/// </remarks>
public sealed class LoyaltyPointsEarnedEvent : DomainEventBase
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
    /// Gets the number of points earned.
    /// </summary>
    public int PointsEarned { get; init; }

    /// <summary>
    /// Gets the customer's new total point balance after earning.
    /// </summary>
    public int NewTotalBalance { get; init; }

    /// <summary>
    /// Gets the customer's previous point balance before earning.
    /// </summary>
    public int PreviousBalance { get; init; }

    /// <summary>
    /// Gets the source/reason for earning points.
    /// </summary>
    public PointEarningSource EarningSource { get; init; }

    /// <summary>
    /// Gets the order ID if points were earned from a purchase.
    /// </summary>
    public int? RelatedOrderId { get; init; }

    /// <summary>
    /// Gets the order amount if points were earned from a purchase.
    /// </summary>
    public decimal? OrderAmount { get; init; }

    /// <summary>
    /// Gets the points multiplier applied (e.g., 2x during promotions).
    /// </summary>
    public decimal PointsMultiplier { get; init; } = 1.0m;

    /// <summary>
    /// Gets the base points before any multiplier was applied.
    /// </summary>
    public int BasePointsBeforeMultiplier { get; init; }

    /// <summary>
    /// Gets the promotion code if points were earned through a promotion.
    /// </summary>
    public string? PromotionCode { get; init; }

    /// <summary>
    /// Gets the current tier of the customer.
    /// </summary>
    public string CurrentTier { get; init; } = string.Empty;

    /// <summary>
    /// Gets the points required to reach the next tier.
    /// </summary>
    public int? PointsToNextTier { get; init; }

    /// <summary>
    /// Gets the expiration date for the earned points (if applicable).
    /// </summary>
    public DateTime? PointsExpirationDate { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoyaltyPointsEarnedEvent"/> class.
    /// </summary>
    public LoyaltyPointsEarnedEvent(
        int customerLoyaltyId,
        int customerId,
        int restaurantId,
        int pointsEarned,
        int previousBalance,
        int newTotalBalance,
        PointEarningSource earningSource,
        string currentTier,
        int basePointsBeforeMultiplier = 0,
        decimal pointsMultiplier = 1.0m,
        int? relatedOrderId = null,
        decimal? orderAmount = null,
        string? promotionCode = null,
        int? pointsToNextTier = null,
        DateTime? pointsExpirationDate = null)
    {
        CustomerLoyaltyId = customerLoyaltyId;
        CustomerId = customerId;
        RestaurantId = restaurantId;
        PointsEarned = pointsEarned;
        PreviousBalance = previousBalance;
        NewTotalBalance = newTotalBalance;
        EarningSource = earningSource;
        CurrentTier = currentTier;
        BasePointsBeforeMultiplier = basePointsBeforeMultiplier > 0 ? basePointsBeforeMultiplier : pointsEarned;
        PointsMultiplier = pointsMultiplier;
        RelatedOrderId = relatedOrderId;
        OrderAmount = orderAmount;
        PromotionCode = promotionCode;
        PointsToNextTier = pointsToNextTier;
        PointsExpirationDate = pointsExpirationDate;
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private LoyaltyPointsEarnedEvent() { }
}

/// <summary>
/// Defines the source/mechanism through which loyalty points were earned.
/// </summary>
public enum PointEarningSource
{
    /// <summary>Points earned from a standard purchase.</summary>
    Purchase = 0,

    /// <summary>Bonus points from a promotion or special offer.</summary>
    Bonus = 1,

    /// <summary>Points earned from referring a new customer.</summary>
    Referral = 2,

    /// <summary>Birthday bonus points.</summary>
    Birthday = 3,

    /// <summary>Points earned for leaving a review.</summary>
    Review = 4,

    /// <summary>Sign-up bonus points for new loyalty members.</summary>
    SignUpBonus = 5,

    /// <summary>Manual positive adjustment by staff.</summary>
    Adjustment = 6,

    /// <summary>Points earned from completing a survey.</summary>
    Survey = 7,

    /// <summary>Points earned from social media engagement.</summary>
    SocialMedia = 8,

    /// <summary>Points restored after a cancelled order reversal was itself reversed.</summary>
    Restoration = 9
}
