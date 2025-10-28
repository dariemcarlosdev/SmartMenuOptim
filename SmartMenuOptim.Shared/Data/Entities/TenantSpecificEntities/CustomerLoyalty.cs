using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

/// <summary>
/// Represents a customer's loyalty program status and points for a specific restaurant.
/// </summary>
/// <remarks>
/// Multi-Tenant Support: This entity is tenant-specific while linking to a global Customer.
/// Each CustomerLoyalty record represents a customer's standing with a specific restaurant,
/// allowing different point balances and tiers across restaurants.
/// </remarks>
public class CustomerLoyalty : TenantEntityBase
{
    /// CustomerLoyalty:
    //•	Pure tenant-specific entity (inherits TenantEntityBase)
    //•	Links global Customer with restaurant-specific loyalty data
    //•	Enables different loyalty standings per restaurant
    //•	Tracks points, tiers, and transactions per restaurant
    //•	Maintains customer engagement at restaurant level

    // === Standalone Properties ===

    /// <summary>
    /// Current point balance for this customer at this restaurant.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Customer's loyalty tier at this restaurant.
    /// </summary>
    public LoyaltyTier Tier { get; set; }

    /// <summary>
    /// Date of last point earning activity.
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    // === Global Entity Relationship ===

    /// <summary>
    /// Foreign key to the global Customer entity.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the global Customer.
    /// </summary>
    public Customer? Customer { get; set; }

    // === Additional Properties ===

    /// <summary>
    /// Total points earned historically.
    /// </summary>
    public int LifetimePoints { get; set; }

    /// <summary>
    /// Point transactions for this customer at this restaurant.
    /// </summary>
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = [];
}

public enum LoyaltyTier
{
    Bronze,
    Silver,
    Gold,
    Platinum
}