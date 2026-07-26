namespace SmartMenuOptim.Domain.Enums;

/// <summary>
/// Represents the tier levels in a customer loyalty program, based on accumulated points.
/// </summary>
/// <remarks>
/// <para>Tier levels are automatically calculated based on the customer's current point balance:</para>
/// <list type="bullet">
///   <item><description><strong>Bronze:</strong> 0-99 points (default starting tier)</description></item>
///   <item><description><strong>Silver:</strong> 100-499 points</description></item>
///   <item><description><strong>Gold:</strong> 500-999 points</description></item>
///   <item><description><strong>Platinum:</strong> 1000+ points</description></item>
/// </list>
/// <para>Tiers can both increase and decrease based on point accumulation and redemption activities.</para>
/// </remarks>
public enum CustomerLoyaltyTier
{
    /// <summary>Default starting tier: 0-99 points</summary>
    Bronze,
    
    /// <summary>100-499 points</summary>
    Silver,
    
    /// <summary>500-999 points</summary>
    Gold,
    
    /// <summary>1000+ points</summary>
    Platinum
}
