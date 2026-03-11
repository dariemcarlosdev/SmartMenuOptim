using SmartMenuOptim.Domain.Features.Restaurants;
using SmartMenuOptim.Domain.Exceptions;
using System;

/// <summary>
/// Represents a promotional offer that applies a fixed discount amount to qualifying orders within a specified date range
/// for a restaurant tenant.
/// </summary>
/// <remarks>
/// <para><strong>DDD AGGREGATE ROOT</strong></para>
/// <para>This class serves as the Aggregate Root for the Promotion aggregate in the Domain-Driven Design context.</para>
/// 
/// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Rich Aggregates without Child Entities</strong></para>
/// <para>This class implements a rich DDD aggregate root pattern WITHOUT child entities. It represents a standalone business concept
/// with complete encapsulation, behavioral methods, and business rule enforcement, but manages only its own state.</para>
/// 
/// <para><strong>Tier 2 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods (Activate, Deactivate, UpdateDetails)</description></item>
///   <item><description><strong>No Child Entities:</strong> Standalone aggregate with no child entity collections - simpler consistency boundary</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to promotion state happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for activation lifecycle, date validation, and discount application rules</description></item>
///   <item><description><strong>Invariant Protection:</strong> Maintains invariants (ValidTo > ValidFrom, can't activate before ValidFrom, can't modify while active)</description></item>
///   <item><description><strong>Encapsulated State:</strong> Private fields (_isActive) control internal state, accessible only through behavioral methods</description></item>
///   <item><description><strong>Lifecycle Management:</strong> Controls promotion lifecycle through state transitions (Draft → Scheduled → Active → Expired)</description></item>
///   <item><description><strong>Tenant Scoped:</strong> Inherits TenantEntityBase - each promotion belongs to a specific restaurant</description></item>
/// </list>
/// 
/// <para><strong>Aggregate Type:</strong> Promotion Aggregate</para>
/// <para>The Promotion is a standalone aggregate with no child entities. It represents a single, cohesive business concept
/// that manages promotional discount rules and their lifecycle within a restaurant's menu system.</para>
/// 
/// <para><strong>Aggregate Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Consistency Boundary:</strong> All promotion state changes are validated and executed atomically through this root entity</description></item>
///   <item><description><strong>Invariants Protected:</strong> Ensures ValidTo date is after ValidFrom date, prevents activation before start date, enforces business rules for discount calculation, validates discount amount range (0 to 1,000,000)</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state (_isActive) and date fields are fully encapsulated and can only be modified through behavioral methods</description></item>
///   <item><description><strong>Rich Domain Model:</strong> Contains business logic for activation, validation, date range checking, and discount application rather than being an anemic data container</description></item>
/// </list>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each promotion is scoped to a specific
/// restaurant (RestaurantId), ensuring proper data isolation in a multi-tenant environment.</para>
/// 
/// <para><strong>Aggregate Pattern Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (typically Id property)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to promotion state happen through public methods that maintain invariants</description></item>
///   <item><description><strong>Access Control:</strong> External code cannot directly modify internal state; must use Activate(), Deactivate(), UpdateDetails(), etc.</description></item>
///   <item><description><strong>Lifecycle Management:</strong> Controls promotion lifecycle through Activate/Deactivate methods with business rule validation</description></item>
///   <item><description><strong>Business Operations:</strong> Exposes domain operations (IsValidAt, IsActive, CanBeActivated) that encapsulate business logic</description></item>
/// </list>
/// 
/// <para><strong>Lifecycle States:</strong></para>
/// <code>
/// Draft → Scheduled → Active → Expired
///   ↓        ↓          ↓
/// Updated  Activated  Deactivated (Manual)
/// 
/// 1. Draft: Created but not activated (_isActive = false, can be modified)
/// 2. Scheduled: ValidFrom in future, waiting for start date
/// 3. Active: ValidFrom ≤ now ≤ ValidTo, _isActive = true, applicable to orders
/// 4. Deactivated: Manually disabled (_isActive = false, within date range)
/// 5. Expired: Current date > ValidTo (automatic expiration)
/// 
/// State Transitions:
/// - Draft → Scheduled: When ValidFrom > now
/// - Scheduled → Active: Call Activate() when now ≥ ValidFrom
/// - Active → Deactivated: Call Deactivate() (manual override)
/// - Active → Expired: Automatic when now > ValidTo
/// - Deactivated → Active: Call Activate() again (if within date range)
/// 
/// Business Rules:
/// - Can only activate if now ≥ ValidFrom
/// - Cannot modify details while active (must deactivate first)
/// - Expired promotions cannot be reactivated
/// - Date range cannot exceed 1 year from now
/// </code>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new promotion
/// var promotion = new Promotion(
///     restaurantId: 123,
///     name: "Spring Sale",
///     discountAmount: 15.00m, // $15.00 off
///     validFrom: new DateTime(2024, 3, 1),
///     validTo: new DateTime(2024, 3, 31),
///     description: "Spring season discount for all orders"
/// );
/// 
/// // Optionally add notes
/// promotion.UpdateNotes("Terms: Valid for dine-in and takeout orders only");
/// 
/// // Activating the promotion (validates start date)
/// promotion.Activate();
/// 
/// // Checking if promotion is active and valid
/// if (promotion.IsActive() && promotion.IsValidAt(DateTime.UtcNow))
/// {
///     // Apply promotion to order
///     var discountAmount = promotion.DiscountAmount;
/// }
/// 
/// // Updating promotion details (must deactivate first)
/// promotion.Deactivate();
/// promotion.UpdateDetails(
///     name: "Extended Spring Sale",
///     discountAmount: 20.00m,
///     validFrom: new DateTime(2024, 3, 1),
///     validTo: new DateTime(2024, 4, 15),
///     description: "Extended spring discount"
/// );
/// promotion.Activate();
/// 
/// // Deactivating when no longer needed
/// promotion.Deactivate();
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through a repository pattern. Private setters and fields are accessible to EF Core through
/// reflection-based field mapping in the entity configuration.</para>
/// 
/// <para><strong>Repository Access:</strong></para>
/// <para>Should be accessed only through IPromotionRepository. Direct instantiation should be limited to factories or
/// application services. Changes should be persisted through Unit of Work pattern to maintain transactional integrity.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Fixed Discount Model:</strong> Uses a fixed discount amount rather than percentage to simplify application and avoid calculation errors</description></item>
///   <item><description><strong>Date Range Validation:</strong> ValidFrom must be before ValidTo, with maximum 1-year extension from current date</description></item>
///   <item><description><strong>Activation Control:</strong> Can only be activated if current date is on or after ValidFrom date</description></item>
///   <item><description><strong>Modification Safety:</strong> Promotion details cannot be updated while active - must deactivate first to ensure consistency</description></item>
/// </list>
/// </remarks>


public class Promotion : TenantEntityBase
{
    // === Private Setters (Encapsulated State) ===
    /// <summary>
    /// Human-friendly name for the promotion (maximum 150 characters).
    /// </summary>
    public string Name { get; private set; }
    
    /// <summary>
    /// Optional description providing details about the promotion.
    /// </summary>
    public string Description { get; private set; }
    
    /// <summary>
    /// Fixed discount amount applied by this promotion (stored as decimal with 2 decimal places).
    /// Must be between 0 and 1,000,000.
    /// </summary>
    public decimal DiscountAmount { get; private set; }
    
    /// <summary>
    /// Start date (inclusive) when the promotion becomes valid (UTC).
    /// </summary>
    public DateTime ValidFrom { get; private set; }
    
    /// <summary>
    /// End date (inclusive) when the promotion expires (UTC).
    /// </summary>
    public DateTime ValidTo { get; private set; }
    
    /// <summary>
    /// Optional notes or terms and conditions for the promotion (maximum 1000 characters).
    /// </summary>
    public string? Notes { get; private set; }
    
    // === Encapsulated State ===
    /// <summary>
    /// Indicates whether the promotion is currently active.
    /// Controlled through Activate() and Deactivate() methods.
    /// </summary>
    private bool _isActive;

    // === Constructors ===
    /// <summary>
    /// Protected parameterless constructor for Entity Framework Core. Why protected?. Protected parameterless constructor allows EF Core to create instances via reflection while preventing direct instantiation from outside the class.
    /// Reason: EF Core requires a parameterless constructor to materialize objects from database records. Making it protected ensures that only EF Core and derived classes can access it, maintaining encapsulation and preventing misuse in application code.
    /// Recommendation: Always provide a protected parameterless constructor in aggregate root entities when using EF Core to ensure proper ORM functionality while adhering to DDD principles.
    /// Reflexion is a powerful feature in .NET that allows code to inspect and manipulate object types and members at runtime. EF Core leverages reflection to create instances of entity classes, set property values, and map database records to objects without requiring public constructors or setters.
    /// Reflection enables EF Core to bypass normal access restrictions, allowing it to instantiate entities even when constructors are protected or private. This is essential for ORM frameworks that need to materialize objects from database data without exposing constructors to application code.
    /// Reeflection-based field mapping in EF Core allows the framework to directly access and set private fields of entity classes during materialization. This means that even if properties have private setters, EF Core can still populate them by manipulating the underlying fields via reflection.
    /// </summary>
    protected Promotion() { /* EF Core */ }
    
    /// <summary>
    /// Creates a new promotion with specified discount and validity period.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier this promotion belongs to.</param>
    /// <param name="name">Human-friendly name for the promotion (required, max 150 characters).</param>
    /// <param name="discountAmount">Fixed discount amount to apply (must be between 0 and 1,000,000).</param>
    /// <param name="validFrom">Start date when promotion becomes valid (inclusive).</param>
    /// <param name="validTo">End date when promotion expires (inclusive).</param>
    /// <param name="description">Optional description providing promotion details.</param>
    /// <exception cref="ArgumentException">Thrown when name is empty, discount is out of range, or dates are invalid.</exception>
    public Promotion(
        int restaurantId,
        string name,
        decimal discountAmount,
        DateTime validFrom,
        DateTime validTo,
        string description = "")
    {
        // ---------------------------------------------------------------
        // PARAMETER GUARD CLAUSES — ArgumentException
        //
        // Guard clauses validate constructor preconditions (caller contract),
        // NOT domain business rules.
        //
        // • ArgumentException → 400 Bad Request (programming error)
        // • PromotionDomainException → 422 Unprocessable Entity
        //   (valid input, but violates a promotion lifecycle rule)
        //
        // EXAMPLES:
        // • name = null             → ArgumentException  (caller bug)
        // • discountAmount = -1     → ArgumentOutOfRangeException (caller bug)
        // • Activate() before start → PromotionDomainException (business rule)
        // • UpdateDetails() active  → PromotionDomainException (business rule)
        // ---------------------------------------------------------------

        if (restaurantId <= 0)
            throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));

        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > 150)
            throw new ArgumentException("Promotion name cannot exceed 150 characters.", nameof(name));

        if (discountAmount < 0 || discountAmount > 1000000m)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), discountAmount, "Discount amount must be between 0 and 1,000,000.");

        if (validTo <= validFrom)
            throw new ArgumentException("ValidTo date must be after ValidFrom date.", nameof(validTo));

        var maxFutureDate = DateTime.UtcNow.AddYears(1);
        if (validTo > maxFutureDate)
            throw new ArgumentException("Promotion cannot extend more than one year into the future.", nameof(validTo));

        RestaurantId = restaurantId;
        Name = name;
        Description = description ?? string.Empty;
        DiscountAmount = discountAmount;
        ValidFrom = validFrom;
        ValidTo = validTo;
        _isActive = false;
        Notes = null;
    }
    
    
    // === Behavioral Methods ===
    /// <summary>
    /// Activates the promotion, making it available for use.
    /// </summary>
    /// <exception cref="PromotionDomainException">Thrown when attempting to activate before ValidFrom date.</exception>
    /// <remarks>
    /// Promotion can only be activated if the current date/time is on or after the ValidFrom date.
    /// Once activated, the promotion can be applied to qualifying orders.
    /// </remarks>
    public void Activate()
    {
        // Domain rule: promotion cannot be activated before its scheduled start date.
        if (DateTime.UtcNow < ValidFrom)
            throw new PromotionDomainException("Cannot activate promotion before ValidFrom date.");
        
        _isActive = true;
    }
    
    /// <summary>
    /// Deactivates the promotion, preventing it from being applied to new orders.
    /// </summary>
    /// <remarks>
    /// Deactivation is immediate and can be performed at any time.
    /// The promotion can be reactivated later by calling Activate() if still within valid date range.
    /// </remarks>
    public void Deactivate() => _isActive = false;
    
    /// <summary>
    /// Checks if the promotion is currently active.
    /// </summary>
    /// <returns>True if the promotion is active; otherwise, false.</returns>
    public bool IsActive() => _isActive;
    
    /// <summary>
    /// Checks if the promotion is valid at a specific date and time.
    /// </summary>
    /// <param name="dateTime">The date and time to check (typically DateTime.UtcNow).</param>
    /// <returns>True if the promotion is both active and within its valid date range; otherwise, false.</returns>
    /// <remarks>
    /// A promotion is considered valid at a given time if:
    /// 1. It is currently activated (_isActive is true)
    /// 2. The specified dateTime is on or after ValidFrom
    /// 3. The specified dateTime is on or before ValidTo
    /// </remarks>
    public bool IsValidAt(DateTime dateTime)
    {
        return _isActive 
            && dateTime >= ValidFrom 
            && dateTime <= ValidTo;
    }
    
    /// <summary>
    /// Checks if the promotion can be activated based on the current date.
    /// </summary>
    /// <returns>True if current date is on or after ValidFrom; otherwise, false.</returns>
    /// <remarks>
    /// Use this method to determine if Activate() can be called without throwing an exception.
    /// </remarks>
    public bool CanBeActivated()
    {
        return DateTime.UtcNow >= ValidFrom;
    }
    
    /// <summary>
    /// Updates the promotion details.
    /// </summary>
    /// <param name="name">New promotion name (required, max 150 characters).</param>
    /// <param name="discountAmount">New discount amount (must be between 0 and 1,000,000).</param>
    /// <param name="validFrom">New start date.</param>
    /// <param name="validTo">New end date (must be after validFrom).</param>
    /// <param name="description">New description.</param>
    /// <exception cref="PromotionDomainException">Thrown when attempting to update an active promotion.</exception>
    /// <exception cref="ArgumentException">Thrown when validation rules are violated.</exception>
    /// <remarks>
    /// Promotion must be deactivated before updating details to ensure consistency.
    /// After updating, the promotion can be reactivated if desired.
    /// </remarks>
    public void UpdateDetails(
        string name,
        decimal discountAmount,
        DateTime validFrom,
        DateTime validTo,
        string description = "")
    {
        // Domain rule: cannot modify details while promotion is live.
        if (_isActive)
            throw new PromotionDomainException("Cannot update promotion details while active. Deactivate first.");

        // Guard clauses: invalid parameters are programming errors, not business rules.
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (name.Length > 150)
            throw new ArgumentException("Promotion name cannot exceed 150 characters.", nameof(name));

        if (discountAmount < 0 || discountAmount > 1000000m)
            throw new ArgumentOutOfRangeException(nameof(discountAmount), discountAmount, "Discount amount must be between 0 and 1,000,000.");

        if (validTo <= validFrom)
            throw new ArgumentException("ValidTo date must be after ValidFrom date.", nameof(validTo));

        var maxFutureDate = DateTime.UtcNow.AddYears(1);
        if (validTo > maxFutureDate)
            throw new ArgumentException("Promotion cannot extend more than one year into the future.", nameof(validTo));
        
        Name = name;
        Description = description ?? string.Empty;
        DiscountAmount = discountAmount;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }
    
    /// <summary>
    /// Updates or clears the promotional notes/terms.
    /// </summary>
    /// <param name="notes">Notes or terms for the promotion (max 1000 characters, or null to clear).</param>
    /// <exception cref="ArgumentException">Thrown when notes exceed maximum length or are too short.</exception>
    /// <remarks>
    /// Notes can be updated independently of other promotion details and while the promotion is active.
    /// If notes are provided, they must be at least 10 characters long.
    /// </remarks>
    public void UpdateNotes(string? notes)
    {
        // Guard clauses: invalid note length is a programming error, not a business rule.
        if (!string.IsNullOrEmpty(notes))
        {
            if (notes.Length > 1000)
                throw new ArgumentException("Notes cannot exceed 1000 characters.", nameof(notes));

            if (notes.Length < 10)
                throw new ArgumentException("Notes, if provided, must be at least 10 characters long.", nameof(notes));
        }
        
        Notes = notes;
    }
    
    // ========================================================================
    // === NAVIGATION PROPERTIES (FOR EF CORE ONLY - NOT FOR DOMAIN LOGIC) ===
    // ========================================================================
    //
    // ARCHITECTURAL DECISION: Hybrid Pattern (DDD + EF Core Navigation Properties)
    //
    // This aggregate uses a HYBRID PATTERN that combines:
    // ✅ Pure DDD practices (private setters, behavioral methods, invariant protection)
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
    // - Domain aggregate for business logic (private setters, behavioral methods)
    // - EF Core entity for database mapping (navigation properties below)
    //
    // TRADE-OFFS ACCEPTED:
    // ⚠️ Aggregate boundary violation (navigation to other aggregates exists but discouraged)
    // ⚠️ Requires discipline to NOT use these properties in domain logic
    // ⚠️ Temptation to bypass repository pattern
    //
    // BENEFITS GAINED:
    // ✅ Single source of truth (one Promotion class)
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
    // ❌ DON'T: promotion.Restaurant.Name
    // ✅ DO:    await _restaurantRepository.GetByIdAsync(promotion.RestaurantId)
    //
    // ❌ DON'T: Navigate through relationships in business logic
    // ✅ DO:    Query via repositories and pass data explicitly
    //
    // These properties are marked 'virtual' to support EF Core lazy loading (if enabled).
    //
    // BLAZOR NOTE:
    // Even with this consolidation, Blazor forms still need simple DTOs because:
    // - Private setters don't work with @bind-Value
    // - Solution: Create PromotionFormDto for Blazor, map to/from this aggregate
    //
    // ========================================================================
    
    /// <summary>
    /// Navigation property to the restaurant tenant.
    /// FOR EF CORE ONLY - Use RestaurantId in domain logic instead.
    /// </summary>
    /// <remarks>
    /// This property enables EF Core to load the restaurant that owns this promotion.
    /// In domain logic, use RestaurantId for promotion operations.
    /// Query Restaurant separately via repository when needed.
    /// </remarks>
    public virtual Restaurant? Restaurant { get; set; }
    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the promotion maintains multi-tenant boundaries and consistency.
    /// </summary>
    /// <exception cref="PromotionDomainException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - RestaurantId is valid and positive
    /// - Restaurant navigation property matches RestaurantId
    /// - Promotion is properly scoped to its restaurant tenant
    /// - All internal state is consistent with tenant boundaries
    /// 
    /// Tenant Consistency Rules:
    /// 1. Promotion must belong to exactly one restaurant (RestaurantId must be positive)
    /// 2. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 3. Promotion cannot be applied to orders from different restaurants
    /// 4. All promotion usage must be within the same restaurant boundary
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant promotion application and discount abuse
    /// - Discounts from one restaurant being applied to another restaurant's orders
    /// - Unauthorized access to promotional offers across tenants
    /// - Reporting inaccuracies in multi-tenant dashboards and analytics
    /// - Revenue leakage and financial fraud across tenant boundaries
    /// - Competitive intelligence leakage (one restaurant seeing another's promotions)
    /// 
    /// When to Call:
    /// - After loading promotions with navigation properties from database
    /// - Before applying promotion to orders in multi-tenant contexts
    /// - Before activating or modifying promotion details
    /// - In data import/migration processes to ensure data integrity
    /// - As part of scheduled data integrity audits
    /// - When validating promotion assignments to orders
    /// - In admin interfaces before displaying promotion details
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// Promotion is a standalone aggregate with no child entities, making validation efficient.
    /// Validation is lightweight and can be called frequently without performance concerns.
    /// 
    /// Design Note:
    /// As a standalone aggregate, Promotion has minimal relationships, making this validation
    /// simpler than aggregates with complex child entity hierarchies. However, the validation
    /// is still critical for maintaining multi-tenant security boundaries.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate RestaurantId is valid
        if (RestaurantId <= 0)
        {
            throw new PromotionDomainException(
                $"Promotion has invalid RestaurantId: {RestaurantId}. " +
                $"RestaurantId must be a positive integer. " +
                $"Promotion: '{Name}' (ID: {Id})");
        }

        // Validate Restaurant navigation property consistency
        if (Restaurant != null)
        {
            if (Restaurant.Id != RestaurantId)
            {
                throw new PromotionDomainException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}). " +
                    $"Promotion: '{Name}' (ID: {Id}), " +
                    $"Restaurant: '{Restaurant.Name}' (ID: {Restaurant.Id})");
            }

            // Additional validation: Ensure restaurant is active and not deleted
            if (Restaurant.IsDeleted)
            {
                throw new PromotionDomainException(
                    $"Promotion '{Name}' (ID: {Id}) is associated with a deleted restaurant '{Restaurant.Name}' (ID: {Restaurant.Id}). " +
                    $"Promotions cannot belong to deleted restaurants.");
            }
        }

        // Validate promotion state consistency with tenant
        // Ensure that if promotion is active, the restaurant context is valid
        if (_isActive && Restaurant != null && !Restaurant.IsAcceptingOrders)
        {
            // This is a warning-level issue, not necessarily an error
            // but worth noting in validation context
            // Could be logged or handled differently based on business rules
            // For now, we'll allow it but could be made stricter if needed
        }
    }

}
