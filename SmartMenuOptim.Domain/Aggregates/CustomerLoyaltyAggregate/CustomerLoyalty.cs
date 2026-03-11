using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Features.Restaurants;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.Events.LoyaltyEvents;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Common;

namespace SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;

/// <summary>
/// Represents a customer's loyalty program membership, managing points accumulation, tier progression, 
/// and transaction history for a restaurant tenant.
/// </summary>
/// <remarks>
/// <para><strong>DDD AGGREGATE ROOT</strong></para>
/// <para>This class serves as the Aggregate Root for the CustomerLoyalty aggregate in the Domain-Driven Design context.</para>
/// 
/// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
/// <para>This class implements a full DDD aggregate root pattern with child entities (LoyaltyTransaction) and rich domain behavior.
/// It manages customer loyalty program membership with complete encapsulation of points, tier progression, and transaction history.</para>
/// 
/// <para><strong>Tier 1 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods (AddPoints, RedeemPoints, AddAdjustment)</description></item>
///   <item><description><strong>Child Entity Management:</strong> Manages LoyaltyTransaction child entities through encapsulated collection (_transactions)</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to loyalty points and transactions happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for point accumulation, redemption, tier progression, and transaction logging</description></item>
///   <item><description><strong>Invariant Protection:</strong> Maintains invariants (points cannot go negative, tier matches point balance, transaction history is immutable)</description></item>
///   <item><description><strong>Collection Encapsulation:</strong> Private backing field (_transactions) with read-only public access (Transactions)</description></item>
///   <item><description><strong>Automatic Tier Calculation:</strong> Tier automatically updates based on point balance through UpdateTier() method</description></item>
///   <item><description><strong>Tenant Scoped:</strong> Inherits TenantEntityBase - each loyalty membership belongs to a specific restaurant</description></item>
/// </list>
/// 
/// <para><strong>Aggregate Type:</strong> CustomerLoyalty Aggregate</para>
/// <para>The CustomerLoyalty is an aggregate that manages a customer's loyalty membership with child entities. 
/// It represents a cohesive business concept that encapsulates loyalty points management, tier progression rules, 
/// and the complete transaction history within a restaurant's loyalty program.</para>
/// 
/// <para><strong>Aggregate Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Consistency Boundary:</strong> All loyalty state changes (points, tier, transactions) are validated and executed atomically through this root entity</description></item>
///   <item><description><strong>Invariants Protected:</strong> Ensures points cannot go negative, enforces positive point values for operations, maintains tier progression rules, guarantees transaction history integrity</description></item>
///   <item><description><strong>Encapsulated Collections:</strong> Transaction collection is fully encapsulated with private backing field and read-only public interface to prevent external manipulation</description></item>
///   <item><description><strong>Child Entities:</strong> Contains LoyaltyTransaction child entities that can only be created and managed through the aggregate root's behavioral methods</description></item>
///   <item><description><strong>Rich Domain Model:</strong> Contains business logic for point accumulation, redemption, tier progression, and transaction tracking rather than being an anemic data container</description></item>
/// </list>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each customer loyalty membership is scoped 
/// to a specific restaurant (RestaurantId), ensuring proper data isolation and preventing cross-tenant data access in a 
/// multi-tenant environment.</para>
/// 
/// <para><strong>Aggregate Pattern Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (typically Id property) and maintains CustomerId reference</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to loyalty state happen through public methods that maintain invariants and update dependent state atomically</description></item>
///   <item><description><strong>Access Control:</strong> External code cannot directly modify internal state or transaction collection; must use AddPoints(), RedeemPoints(), etc.</description></item>
///   <item><description><strong>Lifecycle Management:</strong> Controls loyalty membership lifecycle through point operations with automatic tier progression and transaction logging</description></item>
///   <item><description><strong>Business Operations:</strong> Exposes domain operations that encapsulate business logic for points management and tier calculation</description></item>
///   <item><description><strong>Collection Management:</strong> Manages child LoyaltyTransaction entities ensuring they are only added through aggregate methods, never modified or removed externally</description></item>
/// </list>
/// 
/// <para><strong>Aggregate Composition:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Root Entity:</strong> CustomerLoyalty (this class)</description></item>
///   <item><description><strong>Child Entities:</strong> LoyaltyTransaction collection - represents individual point earning and redemption events</description></item>
///   <item><description><strong>Value Objects:</strong> LoyaltyTier enum representing the customer's current tier level</description></item>
/// </list>
/// 
/// <para><strong>Tier Progression Rules:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Bronze:</strong> 0-99 points (default starting tier)</description></item>
///   <item><description><strong>Silver:</strong> 100-499 points</description></item>
///   <item><description><strong>Gold:</strong> 500-999 points</description></item>
///   <item><description><strong>Platinum:</strong> 1000+ points</description></item>
/// </list>
/// 
/// <para><strong>Lifecycle States:</strong></para>
/// <code>
/// New → Active ⇄ Dormant → Archived
///   ↓
/// Bronze → Silver → Gold → Platinum (Tier Progression)
///   ↑       ↓        ↓        ↓
///   └───────┴────────┴────────┘ (Bidirectional tier changes)
/// 
/// 1. New: Just created, 0 points, Bronze tier, no transactions
/// 2. Active: Has point activity (earning or redemption) within last 12 months
/// 3. Dormant: No activity for 12+ months, points may expire per policy
/// 4. Archived: Customer inactive or deleted, preserved for historical reporting
/// 
/// Tier States (dynamic, based on current points):
/// - Bronze: 0-99 points (default starting tier)
/// - Silver: 100-499 points
/// - Gold: 500-999 points  
/// - Platinum: 1000+ points
/// 
/// Note: Tier changes are automatic and bidirectional based on point balance.
/// Points can decrease through redemption, potentially downgrading tier.
/// </code>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new customer loyalty membership
/// var loyalty = new CustomerLoyalty(
///     restaurantId: 123,
///     customerId: 456
/// );
/// // Initial state: 0 points, Bronze tier
/// 
/// // Customer makes a purchase - earn points
/// loyalty.AddPoints(150, "Order #12345 - $50.00");
/// // Now: 150 points, Silver tier, 1 transaction
/// 
/// // Customer makes another purchase
/// loyalty.AddPoints(400, "Order #12346 - $100.00");
/// // Now: 550 points, Gold tier, 2 transactions
/// 
/// // Customer redeems points for a reward
/// loyalty.RedeemPoints(100, "Free appetizer");
/// // Now: 450 points, Silver tier (downgraded), 3 transactions
/// 
/// // Checking current status
/// Console.WriteLine($"Points: {loyalty.Points}");
/// Console.WriteLine($"Tier: {loyalty.CurrentTier}");
/// Console.WriteLine($"Transactions: {loyalty.Transactions.Count}");
/// 
/// // Reviewing transaction history
/// foreach (var transaction in loyalty.Transactions)
/// {
///     Console.WriteLine($"{transaction.Type}: {transaction.Points} - {transaction.Description}");
/// }
/// 
/// // Attempting invalid operations (will throw exceptions)
/// // loyalty.RedeemPoints(1000, "Reward"); // InvalidOperationException - insufficient points
/// // loyalty.AddPoints(-50, "Invalid"); // ArgumentException - points must be positive
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>The aggregate can be persisted and retrieved through a repository pattern. The private _transactions collection 
/// is accessible to EF Core through reflection-based field mapping in the entity configuration. EF Core will handle the 
/// persistence of both the root entity and its child LoyaltyTransaction entities through proper entity configuration.</para>
/// 
/// <para><strong>Repository Access:</strong></para>
/// <para>Should be accessed only through ICustomerLoyaltyRepository. Direct instantiation should be limited to factories 
/// or application services. Changes should be persisted through Unit of Work pattern to maintain transactional integrity 
/// across the aggregate boundary, ensuring that all point changes and transaction additions are committed atomically.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Immutable History:</strong> Transactions are append-only; once created, they cannot be modified or deleted, ensuring audit trail integrity</description></item>
///   <item><description><strong>Automatic Tier Management:</strong> Tier is automatically recalculated after every point operation, ensuring consistency</description></item>
///   <item><description><strong>Bidirectional Operations:</strong> Points can increase (AddPoints) and decrease (RedeemPoints), with tier automatically adjusting in both directions</description></item>
///   <item><description><strong>Transaction Logging:</strong> Every point operation creates a corresponding transaction record for complete auditability</description></item>
/// </list>
/// </remarks>
public class CustomerLoyalty : TenantEntityBase
{
    // === Private Collections ===
    private readonly List<LoyaltyTransaction> _transactions = new();
    private readonly List<IDomainEvent> _domainEvents = new();
    
    // === Domain Events (Aggregate Pattern) ===
    
    /// <summary>
    /// Gets the domain events raised by this aggregate.
    /// Events are dispatched by the infrastructure layer after successful persistence.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Clears all domain events. Called by infrastructure after dispatching.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
    
    /// <summary>
    /// Adds a domain event to be dispatched after persistence.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    
    // === Private Setters (Encapsulated State) ===
    public int CustomerId { get; private set; }
    public int Points { get; private set; }
    public CustomerLoyaltyTier Tier { get; private set; }
    public DateTime LastActivity { get; private set; }
    public int LifetimePoints { get; private set; }
    
    // ========================================================================
    // === NAVIGATION PROPERTIES (FOR EF CORE ONLY - NOT FOR DOMAIN LOGIC) ===
    // ========================================================================
    //
    // ARCHITECTURAL DECISION: Hybrid Pattern (DDD + EF Core Navigation Properties)
    //
    // This aggregate uses a HYBRID PATTERN that combines:
    // ✅ Pure DDD practices (private setters, encapsulated collections, behavioral methods)
    // ✅ EF Core navigation properties (for ORM convenience)
    //
    // CHALLENGE SOLVED:
    // Originally, we had separate domain and EF Core entity classes causing:
    // ❌ Code duplication
    // ❌ Synchronization issues between versions
    // ❌ Confusion about which to use where
    // ❌ Maintenance overhead
    //
    // SOLUTION: Consolidate into ONE class that serves BOTH purposes
    // - Domain aggregate for business logic (private setters, child entity management)
    // - EF Core entity for database mapping (navigation properties below)
    //
    // TRADE-OFFS ACCEPTED:
    // ⚠️ Aggregate boundary violation (navigation to other aggregates exists but discouraged)
    // ⚠️ Requires discipline to NOT use these properties in domain logic
    // ⚠️ Temptation to bypass repository pattern
    //
    // BENEFITS GAINED:
    // ✅ Single source of truth (one CustomerLoyalty class)
    // ✅ No duplication or synchronization issues
    // ✅ Works directly with EF Core (no separate DTO needed for database)
    // ✅ Maintains DDD patterns through private setters and behavioral methods
    // ✅ EF Core can use these for eager loading, Include(), and relationship mapping
    //
    // ⚠️ IMPORTANT: DO NOT USE THESE IN DOMAIN LOGIC
    //
    // These navigation properties exist ONLY for Entity Framework Core ORM purposes.
    // They violate DDD aggregate boundaries and should NOT be used in business logic.
    //
    // WHY THEY'RE HERE:
    // - EF Core needs them for relationship mapping
    // - Simplifies database queries and eager loading
    // - Maintains compatibility with existing database schema
    // - Eliminates need for separate Shared entity
    //
    // DOMAIN LOGIC RULES:
    // ❌ DON'T: loyalty.Customer.Name or loyalty.Restaurant.Name
    // ✅ DO:    await _customerRepository.GetByIdAsync(loyalty.CustomerId)
    //
    // ❌ DON'T: Navigate through relationships in business logic
    // ✅ DO:    Query via repositories and pass data explicitly
    //
    // These properties are marked 'virtual' to support EF Core lazy loading (if enabled).
    //
    // BLAZOR NOTE:
    // Even with this consolidation, Blazor forms still need simple DTOs because:
    // - Private setters don't work with @bind-Value
    // - Solution: Create CustomerLoyaltyFormDto for Blazor, map to/from this aggregate
    //
    // ========================================================================
    
    /// <summary>
    /// Navigation property to the restaurant tenant.
    /// FOR EF CORE ONLY - Use RestaurantId in domain logic instead.
    /// </summary>
    /// <remarks>
    /// This property enables EF Core to load the restaurant that owns this loyalty program.
    /// In domain logic, use RestaurantId for loyalty operations.
    /// Query Restaurant separately via repository when needed.
    /// </remarks>
    public virtual Restaurant? Restaurant { get; set; }
    
    /// <summary>
    /// Navigation property to the global Customer entity.
    /// FOR EF CORE ONLY - Use CustomerId in domain logic instead.
    /// </summary>
    /// <remarks>
    /// This property enables EF Core to load the customer associated with this loyalty membership.
    /// In domain logic, use CustomerId for customer operations.
    /// Query Customer separately via repository when needed.
    /// Note: Customer is a global entity (not tenant-specific), so customers can have
    /// loyalty memberships at multiple restaurants.
    /// </remarks>
    public virtual Customer? Customer { get; set; }
    
    // === Read-Only Collections ===
    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();

    // === Constructors ===
    /// <summary>
    /// Protected parameterless constructor for Entity Framework Core. Why protected?. Protected parameterless constructor allows EF Core to create instances via reflection while preventing direct instantiation from outside the class.
    /// Reason: EF Core requires a parameterless constructor to materialize objects from database records. Making it protected ensures that only EF Core and derived classes can access it, maintaining encapsulation and preventing misuse in application code.
    /// Recommendation: Always provide a protected parameterless constructor in aggregate root entities when using EF Core to ensure proper ORM functionality while adhering to DDD principles.
    /// Reflexion is a powerful feature in .NET that allows code to inspect and manipulate object types and members at runtime. EF Core leverages reflection to create instances of entity classes, set property values, and map database records to objects without requiring public constructors or setters.
    /// Reflection enables EF Core to bypass normal access restrictions, allowing it to instantiate entities even when constructors are protected or private. This is essential for ORM frameworks that need to materialize objects from database data without exposing constructors to application code.
    /// Reeflection-based field mapping in EF Core allows the framework to directly access and set private fields of entity classes during materialization. This means that even if properties have private setters, EF Core can still populate them by manipulating the underlying fields via reflection.
    /// </summary>
    protected CustomerLoyalty() { /* EF Core */ }
    
    /// <summary>
    /// Creates a new customer loyalty membership for a specific restaurant and customer.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier this loyalty membership belongs to.</param>
    /// <param name="customerId">The customer identifier for this loyalty membership.</param>
    public CustomerLoyalty(int restaurantId, int customerId)
    {
        // ---------------------------------------------------------------
        // PARAMETER GUARD CLAUSES — ArgumentException / ArgumentNullException
        //
        // These are NOT domain business rules. They are programming-error
        // guards that enforce the method's preconditions (its "contract").
        //
        // WHY NOT DomainException?
        // • An invalid restaurantId or customerId is a CALLER BUG, not a
        //   business rule the domain model needs to express. The caller
        //   passed data that should never reach the domain layer.
        // • ArgumentException/ArgumentNullException signal "you called me
        //   wrong" — they target developers, not end-users.
        // • DomainException signals "the operation violates a business
        //   invariant" — it targets the application layer so it can
        //   present a meaningful error to the user.
        //
        // .NET CONVENTION:
        // • ArgumentException / ArgumentNullException → 400 Bad Request
        //   (middleware maps these to HTTP 400)
        // • DomainException → 422 Unprocessable Entity
        //   (valid input, but violates a business rule)
        //
        // EXAMPLE DISTINCTION:
        // • restaurantId = 0    → ArgumentException  (programming error)
        // • points = -5         → ArgumentOutOfRangeException (programming error)
        // • RedeemPoints(500) with 100 balance → LoyaltyDomainException
        //   (business rule: insufficient points for redemption)
        // • Adjustment causing negative balance → LoyaltyDomainException
        //   (business rule: points cannot go below zero)
        // ---------------------------------------------------------------

        if (restaurantId <= 0)
            throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));

        if (customerId <= 0)
            throw new ArgumentException("Valid customer ID is required.", nameof(customerId));

        RestaurantId = restaurantId;
        CustomerId = customerId;
        Points = 0;
        Tier = CustomerLoyaltyTier.Bronze;
        LastActivity = DateTime.UtcNow;
        LifetimePoints = 0;
    }
    
    // === Behavioral Methods ===
    
    /// <summary>
    /// Adds points to the customer's loyalty account for a specified reason.
    /// This method is a factory for creating LoyaltyTransaction child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <param name="points">The number of points to add (must be positive).</param>
    /// <param name="description">The reason for adding points (e.g., order number, activity description).</param>
    /// <param name="transactionType">The type of transaction (default: OrderEarning).</param>
    /// <param name="orderId">Optional order ID if points are from an order.</param>
    /// <exception cref="ArgumentException">Thrown when points is less than or equal to zero.</exception>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add LoyaltyTransaction child entities. Direct manipulation of the transaction collection
    /// is prevented through encapsulation.
    /// 
    /// This method:
    /// - Validates that points are positive
    /// - Adds points to current balance
    /// - Updates lifetime points accumulation
    /// - Updates last activity timestamp
    /// - Creates a transaction record through proper internal constructor
    /// - Automatically recalculates tier based on new point balance
    /// </remarks>
    public void AddPoints(
        int points, 
        string description, 
        LoyaltyTransactionType transactionType = LoyaltyTransactionType.OrderEarning,
        int? orderId = null,
        decimal? orderAmount = null,
        decimal pointsMultiplier = 1.0m)
    {
        // Guard clause: non-positive points is a programming error, not a business rule.
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), points, "Points must be a positive value.");

        var previousBalance = Points;
        var previousTier = Tier;
        
        Points += points;
        LifetimePoints += points;
        LastActivity = DateTime.UtcNow;
        
        var transaction = new LoyaltyTransaction(
            customerLoyaltyId: Id,
            restaurantId: RestaurantId,
            pointsChange: points,
            description: description,
            type: transactionType,
            balanceAfter: Points,
            orderId: orderId
        );
        
        _transactions.Add(transaction);
        UpdateTier();
        
        // Raise LoyaltyPointsEarnedEvent
        AddDomainEvent(new LoyaltyPointsEarnedEvent(
            customerLoyaltyId: Id,
            customerId: CustomerId,
            restaurantId: RestaurantId,
            pointsEarned: points,
            newTotalBalance: Points,
            previousBalance: previousBalance,
            earningSource: MapTransactionTypeToEarningSource(transactionType),
            relatedOrderId: orderId,
            orderAmount: orderAmount,
            pointsMultiplier: pointsMultiplier,
            currentTier: Tier.ToString()
        ));
        
        // Raise LoyaltyTierChangedEvent if tier changed
        if (Tier != previousTier)
        {
            RaiseTierChangedEvent(previousTier, Tier, TierChangeReason.PointsAccumulation);
        }
    }
    
    /// <summary>
    /// Maps a transaction type to a point earning source for events.
    /// </summary>
    private static PointEarningSource MapTransactionTypeToEarningSource(LoyaltyTransactionType transactionType)
    {
        return transactionType switch
        {
            LoyaltyTransactionType.OrderEarning => PointEarningSource.Purchase,
            LoyaltyTransactionType.Bonus => PointEarningSource.Bonus,
            LoyaltyTransactionType.Referral => PointEarningSource.Referral,
            LoyaltyTransactionType.Adjustment => PointEarningSource.Adjustment,
            _ => PointEarningSource.Purchase
        };
    }
    
    /// <summary>
    /// Raises a tier changed event with full details.
    /// </summary>
    private void RaiseTierChangedEvent(
        CustomerLoyaltyTier previousTier,
        CustomerLoyaltyTier newTier,
        TierChangeReason reason)
    {
        var isUpgrade = newTier > previousTier;
        var benefitsChanged = GetBenefitsForTierChange(previousTier, newTier, isUpgrade);
        
        AddDomainEvent(new LoyaltyTierChangedEvent(
            customerLoyaltyId: Id,
            customerId: CustomerId,
            restaurantId: RestaurantId,
            previousTier: previousTier.ToString(),
            newTier: newTier.ToString(),
            currentPointBalance: Points,
            changeReason: reason,
            previousTierDiscountPercent: GetTierDiscount(previousTier),
            newTierDiscountPercent: GetTierDiscount(newTier),
            benefitsChanged: benefitsChanged
        ));
    }
    
    /// <summary>
    /// Gets the discount percentage for a tier.
    /// </summary>
    private static decimal GetTierDiscount(CustomerLoyaltyTier tier)
    {
        return tier switch
        {
            CustomerLoyaltyTier.Bronze => 0m,
            CustomerLoyaltyTier.Silver => 10m,
            CustomerLoyaltyTier.Gold => 15m,
            CustomerLoyaltyTier.Platinum => 20m,
            _ => 0m
        };
    }
    
    /// <summary>
    /// Gets the list of benefits changed for a tier transition.
    /// </summary>
    private static List<string> GetBenefitsForTierChange(
        CustomerLoyaltyTier previousTier,
        CustomerLoyaltyTier newTier,
        bool isUpgrade)
    {
        var allBenefits = new Dictionary<CustomerLoyaltyTier, List<string>>
        {
            { CustomerLoyaltyTier.Silver, new List<string> { "10% Discount", "Birthday Reward" } },
            { CustomerLoyaltyTier.Gold, new List<string> { "15% Discount", "Priority Seating" } },
            { CustomerLoyaltyTier.Platinum, new List<string> { "20% Discount", "VIP Access", "Free Delivery" } }
        };
        
        var benefits = new List<string>();
        
        if (isUpgrade)
        {
            // Get benefits gained from new tier
            if (allBenefits.TryGetValue(newTier, out var newBenefits))
            {
                benefits.AddRange(newBenefits);
            }
        }
        else
        {
            // Get benefits lost from previous tier
            if (allBenefits.TryGetValue(previousTier, out var lostBenefits))
            {
                benefits.AddRange(lostBenefits);
            }
        }
        
        return benefits;
    }
    
    /// <summary>
    /// Redeems points from the customer's loyalty account for a specified reward.
    /// This method is a factory for creating LoyaltyTransaction child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <param name="points">The number of points to redeem (must be positive).</param>
    /// <param name="description">The reward being redeemed (e.g., "Free appetizer", "10% discount").</param>
    /// <param name="transactionType">The type of transaction (default: RewardRedemption).</param>
    /// <exception cref="ArgumentException">Thrown when points is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to redeem more points than available.</exception>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add LoyaltyTransaction child entities for redemptions. Direct manipulation of the transaction collection
    /// is prevented through encapsulation.
    /// 
    /// This method:
    /// - Validates that points are positive
    /// - Verifies sufficient point balance
    /// - Deducts points from current balance
    /// - Updates last activity timestamp
    /// - Creates a transaction record (with negative point value)
    /// - Automatically recalculates tier based on new point balance (may downgrade)
    /// Note: LifetimePoints is NOT reduced when redeeming points
    /// </remarks>
    public void RedeemPoints(
        int points, 
        string description,
        LoyaltyTransactionType transactionType = LoyaltyTransactionType.RewardRedemption)
    {
        // Guard clause: non-positive points is a programming error, not a business rule.
        if (points <= 0)
            throw new ArgumentOutOfRangeException(nameof(points), points, "Points must be a positive value.");

        // Domain rule: cannot redeem more points than the current balance.
        if (points > Points)
            throw new LoyaltyDomainException($"Insufficient points for redemption. Available: {Points}, Requested: {points}.");

        Points -= points;
        LastActivity = DateTime.UtcNow;
        
        var transaction = new LoyaltyTransaction(
            customerLoyaltyId: Id,
            restaurantId: RestaurantId,
            pointsChange: -points,  // Negative for redemption
            description: description,
            type: transactionType,
            balanceAfter: Points,
            orderId: null
        );
        
        _transactions.Add(transaction);
        UpdateTier();
    }

    //
    
    /// <summary>
    /// Adds a bonus or adjustment to the customer's loyalty account.
    /// This method is a factory for creating LoyaltyTransaction child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <param name="points">The number of points to add or subtract (can be positive or negative for adjustments).</param>
    /// <param name="description">Description of the bonus or adjustment.</param>
    /// <param name="transactionType">The type of transaction (Bonus, Adjustment, Referral, or Expiration).</param>
    /// <exception cref="ArgumentException">Thrown when points is zero or description is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when adjustment would result in negative balance.</exception>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add LoyaltyTransaction child entities for adjustments. Direct manipulation of the transaction collection
    /// is prevented through encapsulation.
    /// 
    /// This method handles special transactions like:
    /// - Welcome bonuses
    /// - Birthday bonuses
    /// - Manual adjustments by staff
    /// - Referral rewards
    /// - Point expirations
    /// </remarks>
    public void AddAdjustment(
        int points,
        string description,
        LoyaltyTransactionType transactionType)
    {
        // Guard clause: zero-point adjustment is a programming error, not a business rule.
        if (points == 0)
            throw new ArgumentException("Points adjustment cannot be zero.", nameof(points));

        // Domain rule: adjustment must not cause a negative balance.
        if (Points + points < 0)
            throw new LoyaltyDomainException(
                $"Adjustment of {points} points would result in negative balance. Current: {Points}.");
        
        Points += points;
        
        // Only add to lifetime points if it's a positive adjustment
        if (points > 0)
            LifetimePoints += points;
        
        LastActivity = DateTime.UtcNow;
        
        var transaction = new LoyaltyTransaction(
            customerLoyaltyId: Id,
            restaurantId: RestaurantId,
            pointsChange: points,
            description: description,
            type: transactionType,
            balanceAfter: Points,
            orderId: null
        );
        
        _transactions.Add(transaction);
        UpdateTier();
    }
    
    /// <summary>
    /// Updates the customer's tier based on their current point balance.
    /// </summary>
    /// <remarks>
    /// Tier progression rules:
    /// - Bronze: 0-99 points
    /// - Silver: 100-499 points
    /// - Gold: 500-999 points
    /// - Platinum: 1000+ points
    /// This method is called automatically after AddPoints and RedeemPoints operations.
    /// </remarks>
    private void UpdateTier()
    {
        Tier = Points switch
        {
            >= 1000 => CustomerLoyaltyTier.Platinum,
            >= 500 => CustomerLoyaltyTier.Gold,
            >= 100 => CustomerLoyaltyTier.Silver,
            _ => CustomerLoyaltyTier.Bronze
        };
    }
    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the customer loyalty membership maintains multi-tenant boundaries and consistency across all relationships.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - Restaurant navigation property matches RestaurantId
    /// - Customer exists and is accessible
    /// - All loyalty transactions belong to the same restaurant
    /// 
    /// Tenant Consistency Rules:
    /// 1. CustomerLoyalty must belong to exactly one restaurant (RestaurantId)
    /// 2. All transactions must belong to the same restaurant
    /// 3. Customer reference must be valid (cross-tenant customer access allowed)
    /// 4. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant loyalty program enrollment
    /// - Points from one restaurant being transferred to another
    /// - Transactions from one restaurant appearing in another's loyalty program
    /// - Reporting inaccuracies in multi-tenant dashboards
    /// - Revenue and reward leakage across tenant boundaries
    /// 
    /// When to Call:
    /// - After loading loyalty memberships with navigation properties from database
    /// - Before processing loyalty transactions in multi-tenant contexts
    /// - In data import/migration processes
    /// - As part of data integrity audits
    /// - When validating point operations and tier calculations
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// For large transaction collections, consider validating via database query instead.
    /// 
    /// Customer Cross-Tenant Access:
    /// Note that Customer entities are global (not tenant-specific), so a customer can have
    /// loyalty memberships at multiple restaurants. This is expected and not a violation.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            throw new InvalidOperationException(
                $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
        }

        // Validate all transactions belong to same restaurant
        if (_transactions != null && _transactions.Any())
        {
            var inconsistentTransactions = _transactions
                .Where(t => t.RestaurantId != RestaurantId)
                .Select(t => new { t.Id, t.Type, t.PointsChange, t.RestaurantId, t.TransactionDate })
                .ToList();

            if (inconsistentTransactions.Any())
            {
                var transactionInfo = string.Join(", ", inconsistentTransactions.Select(t => 
                    $"ID: {t.Id}, Type: {t.Type}, Points: {t.PointsChange}, Date: {t.TransactionDate:yyyy-MM-dd}, RestaurantId: {t.RestaurantId}"));
                
                throw new InvalidOperationException(
                    $"CustomerLoyalty contains transactions from different restaurants. " +
                    $"Loyalty RestaurantId: {RestaurantId}, " +
                    $"Inconsistent transactions: [{transactionInfo}]");
            }
        }

        // Note: Customer validation is intentionally omitted because customers are global entities
        // A customer can have loyalty memberships at multiple restaurants (expected behavior)
    }
}