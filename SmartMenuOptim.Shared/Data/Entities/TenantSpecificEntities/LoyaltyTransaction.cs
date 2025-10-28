using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents an individual loyalty point transaction for a customer at a specific restaurant.
    /// </summary>
    /// <remarks>
    /// Multi-Tenant Support: This entity is tenant-specific. Each LoyaltyTransaction is linked to a Restaurant
    /// and CustomerLoyalty, tracking point changes within a specific restaurant's loyalty program.
    /// </remarks>
    public class LoyaltyTransaction : TenantEntityBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Amount of points earned or spent in this transaction.
        /// Positive values represent points earned, negative values represent points spent.
        /// </summary>
        public int PointsChange { get; set; }

        /// <summary>
        /// Description of the transaction (e.g., "Order #123", "Welcome Bonus", "Birthday Points").
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Balance after this transaction was applied.
        /// </summary>
        public int BalanceAfter { get; set; }

        /// <summary>
        /// Type of the loyalty transaction.
        /// </summary>
        public LoyaltyTransactionType Type { get; set; }

        /// <summary>
        /// Optional reference to an order that generated these points.
        /// </summary>
        public int? OrderId { get; set; }

        // === Relationship Properties ===

        /// <summary>
        /// Foreign key to the CustomerLoyalty entity.
        /// </summary>
        public int CustomerLoyaltyId { get; set; }

        /// <summary>
        /// Foreign key to the global Customer entity.
        /// </summary>
        public int CustomerId { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Navigation property to the CustomerLoyalty record.
        /// </summary>
        public CustomerLoyalty? CustomerLoyalty { get; set; }

        /// <summary>
        /// Navigation property to the global Customer.
        /// </summary>
        public Customer? Customer { get; set; }

        /// <summary>
        /// Navigation property to the related Order (if points were earned from an order).
        /// </summary>
        public Order? Order { get; set; }
    }

    /// <summary>
    /// Defines the types of loyalty transactions that can occur.
    /// </summary>
    public enum LoyaltyTransactionType
    {
        /// <summary>
        /// Points earned from placing an order
        /// </summary>
        OrderEarning,

        /// <summary>
        /// Points redeemed for a reward
        /// </summary>
        RewardRedemption,

        /// <summary>
        /// Bonus points (e.g., welcome bonus, birthday bonus)
        /// </summary>
        Bonus,

        /// <summary>
        /// Points adjustment by staff/system
        /// </summary>
        Adjustment,

        /// <summary>
        /// Points from referral program
        /// </summary>
        Referral,

        /// <summary>
        /// Points expired
        /// </summary>
        Expiration
    }
}