using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System;

/// <summary>
/// Represents a customer's loyalty program status and points for a specific restaurant.
/// </summary>
/// <remarks>
/// Multi-Tenant Support: This entity is tenant-specific while linking to a global Customer.
/// Each CustomerLoyalty record represents a customer's standing with a specific restaurant,
/// allowing different point balances and tiers across restaurants.
///
/// NOTE: Indexes are centralized in `AppDbContext.OnModelCreating` to avoid duplication and to
/// provide a single place to control index naming and performance characteristics.
/// </remarks>
[Table("CustomerLoyalties")]
public class CustomerLoyalty : TenantEntityBase
{
    /// <summary>
    /// Current point balance for this customer at this restaurant.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Points cannot be negative")]
    public int Points { get; set; }

    /// <summary>
    /// Customer's loyalty tier at this restaurant.
    /// </summary>
    [Required]
    [EnumDataType(typeof(LoyaltyTier))]
    public LoyaltyTier Tier { get; set; }

    /// <summary>
    /// Date of last point earning activity (UTC).
    /// </summary>
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Foreign key to the global Customer entity.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Customer))]
    public int CustomerId { get; set; }

    /// <summary>
    /// Navigation property to the global Customer.
    /// </summary>
    [Required]
    public Customer Customer { get; set; }

    /// <summary>
    /// Total points earned historically.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Lifetime points cannot be negative")]
    public int LifetimePoints { get; set; }

    /// <summary>
    /// Point transactions for this customer at this restaurant.
    /// </summary>
    /// <remarks>
    /// InverseProperty is used to specify the inverse navigation property in LoyaltyTransaction.
    /// The collection is initialized to avoid null-reference issues.
    /// </remarks>
    [InverseProperty(nameof(LoyaltyTransaction.CustomerLoyalty))]
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}

public enum LoyaltyTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}