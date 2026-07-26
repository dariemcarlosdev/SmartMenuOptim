using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate
{
    /// <summary>
    /// Immutable audit trail of loyalty point changes (earning, redemption, adjustment).
    /// Child entity of CustomerLoyalty aggregate - value-object-like with identity for EF Core tracking.
    /// </summary>
    /// <remarks>
    /// 🧩 CHILD ENTITY - CustomerLoyalty Aggregate (Tier 1)
    /// 
    /// Key Characteristics:
    /// • Immutable append-only history (no update methods)
    /// • Created only via CustomerLoyalty.AddPoints/RedeemPoints/AddAdjustment
    /// • Internal constructor prevents external instantiation
    /// • Tenant-scoped: must match parent CustomerLoyalty's RestaurantId
    /// 
    /// Business Rules:
    /// • PointsChange cannot be zero
    /// • BalanceAfter must be non-negative (calculated by aggregate)
    /// • Description required (max 200 chars)
    /// 
    /// Transaction Types: OrderEarning, RewardRedemption, Bonus, Adjustment, Referral, Expiration
    /// 
    /// <code>
    /// // ✅ CORRECT - Through parent aggregate
    /// customerLoyalty.AddPoints(100, "Order #12345", LoyaltyTransactionType.OrderEarning, orderId: 123);
    /// 
    /// // ❌ WRONG - Direct instantiation (won't compile)
    /// var transaction = new LoyaltyTransaction(...);
    /// </code>
    /// </remarks>
    [Table("LoyaltyTransactions")]
    public class LoyaltyTransaction : TenantEntityBase
    {
        // ===================================================================
        // PROPERTIES WITH ENCAPSULATION (Private Setters)
        // ===================================================================

        /// <summary>
        /// Amount of points earned or spent in this transaction.
        /// </summary>
        /// <remarks>
        /// - Positive values: Points earned
        /// - Negative values: Points spent/redeemed
        /// - Cannot be zero
        /// - Set at creation through aggregate root
        /// </remarks>
        public int PointsChange { get; private set; }

        /// <summary>
        /// Description of the transaction.
        /// </summary>
        /// <remarks>
        /// - Required, cannot be null or empty
        /// - Maximum length: 200 characters
        /// - Examples: "Order #123", "Welcome Bonus", "Birthday Points", "Redeemed: Free Appetizer"
        /// </remarks>
        [MaxLength(200)]
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Customer's point balance after this transaction was applied.
        /// </summary>
        /// <remarks>
        /// - Calculated and set by CustomerLoyalty aggregate
        /// - Always non-negative
        /// - Represents cumulative balance at time of transaction
        /// - Used for audit trail and balance verification
        /// </remarks>
        public int BalanceAfter { get; private set; }

        /// <summary>
        /// Type of the loyalty transaction.
        /// </summary>
        /// <remarks>
        /// Determines the nature and source of the point change.
        /// See LoyaltyTransactionType enum for available types.
        /// </remarks>
        public LoyaltyTransactionType Type { get; private set; }

        /// <summary>
        /// Date and time when the transaction occurred (UTC).
        /// </summary>
        /// <remarks>
        /// - Set at creation time
        /// - Immutable after creation
        /// - Stored in UTC for consistency across timezones
        /// - Used for transaction history and audit trail
        /// </remarks>
        public DateTime TransactionDate { get; private set; }

        // ===================================================================
        // RELATIONSHIP PROPERTIES (Foreign Keys)
        // ===================================================================

        /// <summary>
        /// Foreign key to the CustomerLoyalty entity (parent aggregate root).
        /// </summary>
        /// <remarks>
        /// - Required for every transaction
        /// - Must reference a valid CustomerLoyalty
        /// - Set at creation by aggregate root
        /// </remarks>
        [Required]
        [ForeignKey(nameof(CustomerLoyalty))]
        public int CustomerLoyaltyId { get; private set; }

        /// <summary>
        /// Optional foreign key to the Order that generated these points.
        /// </summary>
        /// <remarks>
        /// - Null for non-order transactions (bonus, adjustment, etc.)
        /// - When set, Order must belong to same restaurant
        /// - Used for tracing points back to originating order
        /// </remarks>
        [ForeignKey(nameof(Order))]
        public int? OrderId { get; private set; }

        // ===================================================================
        // NAVIGATION PROPERTIES
        // ===================================================================

        /// <summary>
        /// Navigation property to the CustomerLoyalty parent aggregate root.
        /// </summary>
        /// <remarks>
        /// - May be null if not loaded (lazy loading)
        /// - Used for tenant consistency validation
        /// - Provides access to parent aggregate state
        /// </remarks>
        public CustomerLoyalty? CustomerLoyalty { get; private set; }

        /// <summary>
        /// Navigation property to the Order that generated these points (if applicable).
        /// </summary>
        /// <remarks>
        /// - Null for non-order transactions
        /// - Used for order-based point tracking
        /// - Validated for same-restaurant constraint
        /// </remarks>
        public Order? Order { get; private set; }

        // ===================================================================
        // CONSTRUCTORS
        // ===================================================================

        /// <summary>
        /// Protected parameterless constructor for EF Core.
        /// Required for entity materialization from database.
        /// </summary>
        /// <remarks>
        /// Not for direct use. EF Core uses this via reflection.
        /// </remarks>
        protected LoyaltyTransaction() { }

        /// <summary>
        /// Creates a new loyalty transaction.
        /// Internal constructor - can only be called from within the domain assembly (by CustomerLoyalty aggregate).
        /// </summary>
        /// <param name="customerLoyaltyId">The parent CustomerLoyalty identifier.</param>
        /// <param name="restaurantId">The restaurant (tenant) identifier.</param>
        /// <param name="pointsChange">Points earned (positive) or spent (negative). Cannot be zero.</param>
        /// <param name="description">Transaction description (required, max 200 chars).</param>
        /// <param name="type">Transaction type.</param>
        /// <param name="balanceAfter">Customer's point balance after this transaction.</param>
        /// <param name="orderId">Optional order reference that generated these points.</param>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        /// <remarks>
        /// This constructor enforces invariants at creation time (DDD best practice).
        /// Only the CustomerLoyalty aggregate should call this constructor.
        /// </remarks>
        internal LoyaltyTransaction(
            int customerLoyaltyId,
            int restaurantId,
            int pointsChange,
            string description,
            LoyaltyTransactionType type,
            int balanceAfter,
            int? orderId = null)
        {
            // Validate invariants
            ValidateCustomerLoyaltyId(customerLoyaltyId);
            ValidateRestaurantId(restaurantId);
            ValidatePointsChange(pointsChange);
            ValidateDescription(description);
            ValidateBalanceAfter(balanceAfter);

            // Set properties
            CustomerLoyaltyId = customerLoyaltyId;
            RestaurantId = restaurantId;
            PointsChange = pointsChange;
            Description = description.Trim();
            Type = type;
            BalanceAfter = balanceAfter;
            OrderId = orderId;
        }

        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the transaction maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - CustomerLoyalty parent belongs to the same restaurant
        /// - Order reference (if present) belongs to the same restaurant
        /// 
        /// Tenant Consistency Rules:
        /// 1. Transaction must belong to exactly one restaurant
        /// 2. CustomerLoyalty parent must belong to the same restaurant
        /// 3. Order (if referenced) must belong to the same restaurant
        /// 4. Restaurant navigation (if loaded) must match RestaurantId
        /// 
        /// This is a critical security and data integrity check for multi-tenant systems.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate CustomerLoyalty parent tenant consistency
            if (CustomerLoyalty != null)
            {
                if (CustomerLoyalty.Id != CustomerLoyaltyId)
                {
                    throw new InvalidOperationException(
                        $"CustomerLoyalty navigation property ID ({CustomerLoyalty.Id}) does not match CustomerLoyaltyId ({CustomerLoyaltyId}).");
                }

                if (CustomerLoyalty.RestaurantId != RestaurantId)
                {
                    throw new InvalidOperationException(
                        $"Loyalty transaction must belong to same restaurant as CustomerLoyalty. " +
                        $"Transaction RestaurantId: {RestaurantId}, CustomerLoyalty RestaurantId: {CustomerLoyalty.RestaurantId}");
                }
            }

            // Validate Order tenant consistency (if present)
            if (Order != null)
            {
                if (OrderId.HasValue && Order.Id != OrderId.Value)
                {
                    throw new InvalidOperationException(
                        $"Order navigation property ID ({Order.Id}) does not match OrderId ({OrderId}).");
                }

                if (Order.RestaurantId != RestaurantId)
                {
                    throw new InvalidOperationException(
                        $"Referenced Order must belong to same restaurant as transaction. " +
                        $"Transaction RestaurantId: {RestaurantId}, Order RestaurantId: {Order.RestaurantId}");
                }
            }
        }

        // ===================================================================
        // PRIVATE VALIDATION METHODS (Guard Clauses)
        // ===================================================================

        private static void ValidateCustomerLoyaltyId(int customerLoyaltyId)
        {
            if (customerLoyaltyId <= 0)
            {
                throw new ArgumentException(
                    "CustomerLoyaltyId must be a positive integer.",
                    nameof(customerLoyaltyId));
            }
        }

        private static void ValidateRestaurantId(int restaurantId)
        {
            if (restaurantId <= 0)
            {
                throw new ArgumentException(
                    "RestaurantId must be a positive integer.",
                    nameof(restaurantId));
            }
        }

        private static void ValidatePointsChange(int pointsChange)
        {
            if (pointsChange == 0)
            {
                throw new ArgumentException(
                    "PointsChange cannot be zero. Transaction must add or subtract points.",
                    nameof(pointsChange));
            }
        }

        private static void ValidateDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException(
                    "Description is required and cannot be empty.",
                    nameof(description));
            }

            if (description.Length > 200)
            {
                throw new ArgumentException(
                    "Description cannot exceed 200 characters.",
                    nameof(description));
            }
        }

        private static void ValidateBalanceAfter(int balanceAfter)
        {
            if (balanceAfter < 0)
            {
                throw new ArgumentException(
                    "BalanceAfter cannot be negative.",
                    nameof(balanceAfter));
            }
        }
    }
}