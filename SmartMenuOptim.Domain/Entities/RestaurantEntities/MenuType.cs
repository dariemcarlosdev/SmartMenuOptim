using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartMenuOptim.Domain.Entities.TenantSpecificEntities
{
    /// <summary>
    /// Represents a menu type category for organizing and scheduling menus within a restaurant's menu system.
    /// </summary>
    /// <remarks>
    /// This is a LOOKUP AGGREGATE ROOT following Domain-Driven Design principles.
    /// <para><strong>3-TIER DDD STRATEGY: Tier 2 - Simple Aggregates (Lightweight DDD) - Lookup/Reference Data</strong></para>
    /// <para>This class implements a lightweight DDD aggregate pattern for lookup/reference data entities. While simpler than
    /// main domain aggregates (Menu, Order, Restaurant), it still provides encapsulation, validation, and behavioral methods
    /// to maintain data consistency and support menu organization and scheduling.</para>
    /// 
    /// <para><strong>Tier 2 Characteristics (Lookup Aggregate):</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Encapsulation:</strong> Properties use private setters to prevent unauthorized state changes</description></item>
    ///   <item><description><strong>Validation:</strong> Business rules enforced through constructor and behavioral methods with guard clauses</description></item>
    ///   <item><description><strong>Rich Behavior:</strong> Domain logic encapsulated in methods (UpdateBasicInfo, SetDefaultTimes, UpdateDisplayOrder) rather than anemic property bags</description></item>
    ///   <item><description><strong>Simple Lifecycle:</strong> No complex child entities, serves as reference data for menu scheduling</description></item>
    ///   <item><description><strong>Lightweight Invariants:</strong> Basic consistency rules (name required, time window validation, display order)</description></item>
    ///   <item><description><strong>Reference Data:</strong> Defines menu categories referenced by Menu aggregate via MenuTypeId</description></item>
    /// </list>
    /// 
    /// <para><strong>Entity Overview:</strong></para>
    /// <para>A MenuType categorizes menus by service period or occasion within a restaurant. Common menu types include
    /// "Breakfast" (6:00 AM - 11:00 AM), "Lunch" (11:00 AM - 3:00 PM), "Dinner" (5:00 PM - 10:00 PM), "Brunch" (weekends),
    /// "Happy Hour", "Late Night", or special occasion types like "Seasonal", "Holiday", "Catering". Menu types include
    /// optional default time windows that serve as templates when creating new menus of that type, streamlining menu setup
    /// and ensuring consistency.</para>
    /// 
    /// <para><strong>Multi-Tenant Support:</strong></para>
    /// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each menu type is scoped to a specific
    /// restaurant (RestaurantId), allowing restaurants to define custom menu type structures and schedules. This ensures proper
    /// data isolation in a multi-tenant environment and prevents cross-tenant menu type references.</para>
    /// 
    /// <para><strong>Consistency Boundary:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Invariants Protected:</strong> Name required (1-100 chars), default times must be set together or both null, start/end times cannot be identical</description></item>
    ///   <item><description><strong>Encapsulated State:</strong> Internal state can only be modified through behavioral methods (UpdateBasicInfo, SetDefaultTimes, ClearDefaultTimes, UpdateDisplayOrder)</description></item>
    ///   <item><description><strong>Transactional Consistency:</strong> All changes validated atomically through public methods</description></item>
    ///   <item><description><strong>Business Rules:</strong> Menus must belong to same restaurant, time windows must be valid</description></item>
    ///   <item><description><strong>Reference Data Integrity:</strong> Cannot be deleted if referenced by active menus</description></item>
    /// </list>
    /// 
    /// <para><strong>Domain Features:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
    ///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
    ///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for soft deletion scenarios</description></item>
    ///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
    ///   <item><description><strong>Display Order:</strong> Supports custom ordering for menu type presentation</description></item>
    ///   <item><description><strong>Default Time Windows:</strong> Optional template times for menu creation (local restaurant time)</description></item>
    ///   <item><description><strong>Flexible Scheduling:</strong> Time windows can be set, cleared, or left undefined</description></item>
    /// </list>
    /// 
    /// <para><strong>Relationships:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Menus (One-to-Many):</strong> Referenced by Menu entities via MenuTypeId foreign key</description></item>
    ///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
    ///   <item><description><strong>Lookup/Reference Data:</strong> Provides categorization and scheduling templates for menus</description></item>
    /// </list>
    /// 
    /// <para><strong>Example Usage:</strong></para>
    /// <code>
    /// // Creating standard service period menu types
    /// var breakfast = new MenuType(
    ///     restaurantId: 123,
    ///     name: "Breakfast",
    ///     description: "Morning menu served daily",
    ///     displayOrder: 1
    /// );
    /// breakfast.SetDefaultTimes(
    ///     startTime: TimeSpan.FromHours(6),      // 6:00 AM
    ///     endTime: TimeSpan.FromHours(11)        // 11:00 AM
    /// );
    /// 
    /// var lunch = new MenuType(
    ///     restaurantId: 123,
    ///     name: "Lunch",
    ///     description: "Midday specials and favorites",
    ///     displayOrder: 2
    /// );
    /// lunch.SetDefaultTimes(
    ///     startTime: TimeSpan.FromHours(11),     // 11:00 AM
    ///     endTime: TimeSpan.FromHours(15)        // 3:00 PM
    /// );
    /// 
    /// var dinner = new MenuType(
    ///     restaurantId: 123,
    ///     name: "Dinner",
    ///     description: "Evening fine dining experience",
    ///     displayOrder: 3
    /// );
    /// dinner.SetDefaultTimes(
    ///     startTime: TimeSpan.FromHours(17),     // 5:00 PM
    ///     endTime: TimeSpan.FromHours(22)        // 10:00 PM
    /// );
    /// 
    /// // Special occasion menu type without default times
    /// var seasonal = new MenuType(
    ///     restaurantId: 123,
    ///     name: "Seasonal Specials",
    ///     description: "Limited-time seasonal offerings",
    ///     displayOrder: 10
    /// );
    /// // No default times - varies by season
    /// 
    /// // Updating menu type information
    /// breakfast.UpdateBasicInfo(
    ///     name: "Breakfast & Brunch",
    ///     description: "Morning favorites and weekend brunch items"
    /// );
    /// 
    /// // Clearing default times
    /// seasonal.ClearDefaultTimes();
    /// 
    /// // Validating tenant consistency after loading from database
    /// breakfast.ValidateTenantConsistency();
    /// 
    /// // Using menu types when creating menus
    /// var menu = new Menu(restaurantId, "Daily Breakfast", breakfast.Id);
    /// // Menu inherits default times from menu type
    /// </code>
    /// 
    /// <para><strong>Entity Framework Core Support:</strong></para>
    /// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The entity can be
    /// persisted and retrieved through a repository pattern. Private setters are accessible to EF Core through reflection-based
    /// field mapping in the entity configuration. Navigation properties configured for menu relationships.</para>
    /// 
    /// <para><strong>Design Considerations:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Name Uniqueness:</strong> Should be unique per restaurant to avoid confusion (recommended, enforced at application level or database index)</description></item>
    ///   <item><description><strong>Time Window Atomicity:</strong> Both start and end times must be set together or both be null (no partial windows)</description></item>
    ///   <item><description><strong>Local Time Zones:</strong> Default times are in local restaurant time (not UTC) for scheduling purposes</description></item>
    ///   <item><description><strong>Template Pattern:</strong> Default times serve as templates, not constraints on individual menus</description></item>
    ///   <item><description><strong>Flexible Scheduling:</strong> Not all menu types need time windows (e.g., seasonal, catering)</description></item>
    ///   <item><description><strong>Reference Data Stability:</strong> Menu types should be relatively stable; changes affect all referencing menus</description></item>
    ///   <item><description><strong>Soft Delete:</strong> Prefer soft deletion over hard deletion to maintain menu classification history</description></item>
    /// </list>
    /// 
    /// <para><strong>Indexing Strategy:</strong></para>
    /// <para>Database indexes for efficient querying are defined centrally in AppDbContext.OnModelCreating:
    /// - IX_MenuTypes_Restaurant_DisplayOrder: For tenant-scoped menu type ordering
    /// - IX_MenuTypes_Restaurant_Name: For lookup by menu type name within restaurant
    /// - Unique constraint on (RestaurantId, Name) to prevent duplicate menu type names per restaurant</para>
    /// 
    /// <para><strong>Use Cases:</strong></para>
    /// <list type="bullet">
    ///   <item><description><strong>Menu Organization:</strong> Categorize menus by service period or occasion</description></item>
    ///   <item><description><strong>Scheduling Templates:</strong> Provide default time windows for consistent menu creation</description></item>
    ///   <item><description><strong>Service Period Management:</strong> Define breakfast, lunch, dinner, and specialty service times</description></item>
    ///   <item><description><strong>Menu Filtering:</strong> Allow customers to view menus by service period (what's available now?)</description></item>
    ///   <item><description><strong>Reporting:</strong> Analyze sales and performance by menu type (breakfast vs dinner revenue)</description></item>
    ///   <item><description><strong>Operational Planning:</strong> Staff scheduling aligned with menu type service periods</description></item>
    /// </list>
    /// </remarks>
    [Table("MenuTypes")]
    public class MenuType : TenantEntityBase, IValidatableObject
    {
        // ===================================================================
        // PROPERTIES WITH ENCAPSULATION (Private Setters)
        // ===================================================================
        
        /// <summary>
        /// Name of the menu type (e.g., Breakfast, Lunch, Dinner, Seasonal).
        /// </summary>
        /// <remarks>
        /// Required identifier for the menu category. Must be:
        /// - Non-empty and non-whitespace
        /// - Between 1 and 100 characters
        /// - Unique per restaurant (recommended, enforced at application/database level)
        /// 
        /// Common Menu Type Names:
        /// - Service Periods: "Breakfast", "Lunch", "Dinner", "Brunch", "Late Night"
        /// - Special Times: "Happy Hour", "Early Bird", "Weekend Brunch"
        /// - Occasions: "Seasonal", "Holiday", "Valentine's Day", "Thanksgiving"
        /// - Service Types: "Catering", "Takeout", "Delivery Only", "Dine-In"
        /// 
        /// Modifiable via UpdateBasicInfo() method.
        /// </remarks>
        [Required(ErrorMessage = "MenuType name is required")]
        [MaxLength(100, ErrorMessage = "MenuType name cannot exceed 100 characters")]
        [MinLength(1, ErrorMessage = "MenuType name must contain at least 1 character")]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Description of the menu type providing additional context.
        /// </summary>
        /// <remarks>
        /// Provides detailed information about the menu type for:
        /// - Maximum length: 500 characters
        /// - Can be empty string (defaults to empty)
        /// 
        /// Used for:
        /// - Menu descriptions and marketing
        /// - Digital menu tooltips and info sections
        /// - Staff scheduling and operational planning
        /// - Customer communication
        /// 
        /// Examples:
        /// - "Morning favorites served from 6 AM to 11 AM daily"
        /// - "Midday specials featuring fresh salads and sandwiches"
        /// - "Fine dining experience with signature entrees"
        /// - "Limited-time seasonal offerings available while supplies last"
        /// 
        /// Modifiable via UpdateBasicInfo() method.
        /// </remarks>
        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Default start time for menus of this type in local restaurant time.
        /// </summary>
        /// <remarks>
        /// Optional template time window for menu creation:
        /// - Represents local restaurant time (not UTC)
        /// - Must be set together with DefaultEndTime (both or neither)
        /// - Cannot be identical to DefaultEndTime
        /// - Serves as template when creating new menus of this type
        /// 
        /// Common Examples:
        /// - Breakfast: 06:00 (6:00 AM)
        /// - Lunch: 11:00 (11:00 AM)
        /// - Dinner: 17:00 (5:00 PM)
        /// - Happy Hour: 15:00 (3:00 PM)
        /// - Late Night: 22:00 (10:00 PM)
        /// 
        /// Set via SetDefaultTimes() method, cleared via ClearDefaultTimes() method.
        /// </remarks>
        public TimeSpan? DefaultStartTime { get; private set; }

        /// <summary>
        /// Default end time for menus of this type in local restaurant time.
        /// </summary>
        /// <remarks>
        /// Optional template time window for menu creation:
        /// - Represents local restaurant time (not UTC)
        /// - Must be set together with DefaultStartTime (both or neither)
        /// - Cannot be identical to DefaultStartTime
        /// - Serves as template when creating new menus of this type
        /// 
        /// Common Examples:
        /// - Breakfast: 11:00 (11:00 AM)
        /// - Lunch: 15:00 (3:00 PM)
        /// - Dinner: 22:00 (10:00 PM)
        /// - Happy Hour: 19:00 (7:00 PM)
        /// - Late Night: 02:00 (2:00 AM next day)
        /// 
        /// Set via SetDefaultTimes() method, cleared via ClearDefaultTimes() method.
        /// </remarks>
        public TimeSpan? DefaultEndTime { get; private set; }

        /// <summary>
        /// Display order for sorting menu types in UI presentations. Lower numbers appear first.
        /// </summary>
        /// <remarks>
        /// Controls the sequence in which menu types appear in:
        /// - Menu selection interfaces
        /// - Admin configuration screens
        /// - Mobile apps and websites
        /// - Reporting dashboards
        /// 
        /// Typical Display Order Pattern:
        /// - 1: Breakfast
        /// - 2: Brunch
        /// - 3: Lunch
        /// - 4: Dinner
        /// - 5: Late Night
        /// - 10: Seasonal/Special (if separate)
        /// 
        /// Constraints:
        /// - Must be non-negative (0 or greater)
        /// - No maximum limit
        /// - Gaps allowed (e.g., 1, 2, 5, 10)
        /// - Multiple menu types can share same order (displayed alphabetically)
        /// 
        /// Modifiable via UpdateDisplayOrder() method.
        /// </remarks>
        [Range(0, int.MaxValue, ErrorMessage = "DisplayOrder must be a non-negative integer")]
        public int DisplayOrder { get; private set; }

        // ===================================================================
        // NAVIGATION PROPERTIES
        // ===================================================================

        /// <summary>
        /// Navigation property for all menus of this type.
        /// </summary>
        /// <remarks>
        /// Provides access to all Menu entities referencing this MenuType via MenuTypeId.
        /// 
        /// Used for:
        /// - Listing all menus in a menu type category
        /// - Menu type-based filtering and search
        /// - Analytics on menu type usage
        /// - Preventing deletion of menu types with active menus
        /// - Tenant consistency validation
        /// 
        /// Performance Considerations:
        /// - May contain large collections for commonly used menu types
        /// - Use Include() explicitly when needed for eager loading
        /// - Consider querying database for counts instead of loading full collection
        /// 
        /// Tenant Consistency:
        /// All menus in this collection must belong to the same restaurant as this MenuType.
        /// Validated in ValidateTenantConsistency() and Validate() methods.
        /// </remarks>
        public ICollection<Menu> Menus { get; set; } = new List<Menu>();
        
        // === Constructors ===
        
        /// <summary>
        /// Protected constructor for EF Core.
        /// </summary>
        protected MenuType() { }
        
        /// <summary>
        /// Creates a new menu type.
        /// </summary>
        public MenuType(int restaurantId, string name, string? description = null, int displayOrder = 0)
        {
            if (restaurantId <= 0)
                throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Menu type name is required.", nameof(name));
            
            RestaurantId = restaurantId;
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            DisplayOrder = displayOrder >= 0 ? displayOrder : 0;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
        
        // === Business Methods (Aggregate Pattern) ===
        
        /// <summary>
        /// Updates the menu type's basic information.
        /// </summary>
        public void UpdateBasicInfo(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Menu type name is required.", nameof(name));
            
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Sets the default time window for menus of this type.
        /// </summary>
        public void SetDefaultTimes(TimeSpan? startTime, TimeSpan? endTime)
        {
            if (startTime.HasValue && endTime.HasValue && startTime.Value == endTime.Value)
                throw new ArgumentException("Start and end times cannot be identical.");
            
            if (startTime.HasValue && !endTime.HasValue || !startTime.HasValue && endTime.HasValue)
                throw new ArgumentException("Both start and end times must be set together.");
            
            DefaultStartTime = startTime;
            DefaultEndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Clears the default time window.
        /// </summary>
        public void ClearDefaultTimes()
        {
            DefaultStartTime = null;
            DefaultEndTime = null;
            UpdatedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Updates the display order.
        /// </summary>
        public void UpdateDisplayOrder(int order)
        {
            if (order < 0)
                throw new ArgumentException("Display order must be non-negative.", nameof(order));
            
            DisplayOrder = order;
            UpdatedAt = DateTime.UtcNow;
        }


        // ===================================================================
        // MULTI-TENANT VALIDATION
        // ===================================================================

        /// <summary>
        /// Validates that the menu type maintains multi-tenant boundaries and consistency.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
        /// <remarks>
        /// This method should be called after navigation properties are loaded to ensure:
        /// - Restaurant navigation property matches RestaurantId
        /// - All referenced menus belong to the same restaurant
        /// 
        /// Tenant Consistency Rules:
        /// 1. MenuType must belong to exactly one restaurant (RestaurantId)
        /// 2. All menus in Menus collection must belong to the same restaurant
        /// 3. Restaurant navigation property ID (if loaded) must match RestaurantId
        /// 
        /// Security Implications:
        /// This is a critical security check in multi-tenant systems to prevent:
        /// - Cross-tenant menu type references
        /// - Menus from one restaurant using another restaurant's menu types
        /// - Scheduling confusion between different restaurant tenants
        /// - Reporting inaccuracies in multi-tenant dashboards
        /// 
        /// When to Call:
        /// - After loading menu types with navigation properties from database
        /// - Before displaying menu type information in multi-tenant contexts
        /// - In data import/migration processes
        /// - As part of data integrity audits
        /// - When validating menu type assignments
        /// 
        /// Performance Note:
        /// Only performs validation if navigation properties are loaded.
        /// Does not trigger lazy loading to avoid N+1 query issues.
        /// For large Menus collections, consider validating via database query instead.
        /// </remarks>
        public void ValidateTenantConsistency()
        {
            // Validate Restaurant navigation property consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                throw new InvalidOperationException(
                    $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
            }

            // Validate all menus belong to same restaurant
            if (Menus != null && Menus.Any())
            {
                var inconsistentMenus = Menus
                    .Where(m => m.RestaurantId != RestaurantId)
                    .Select(m => new { m.Id, m.Name, m.RestaurantId })
                    .ToList();

                if (inconsistentMenus.Any())
                {
                    var menuInfo = string.Join(", ", inconsistentMenus.Select(m => $"{m.Name} (ID: {m.Id}, RestaurantId: {m.RestaurantId})"));
                    
                    throw new InvalidOperationException(
                        $"MenuType contains menus from different restaurants. " +
                        $"MenuType RestaurantId: {RestaurantId}, " +
                        $"Inconsistent menus: [{menuInfo}]");
                }
            }
        }

        // ===================================================================
        // VALIDATION LOGIC (IValidatableObject)
        // ===================================================================
        // IValidatableObject is REQUIRED for Tier 2 when used with EF Core SaveChanges validation
        // Inline tenant consistency checks to avoid try-catch with yield return
        
        /// <summary>
        /// Validates the menu type entity ensuring data consistency and business rules.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>Collection of validation results.</returns>
        /// <remarks>
        /// Validation Rules:
        /// 1. Tenant Boundary:
        ///    - Must belong to exactly one restaurant
        ///    - All menus must belong to same restaurant
        /// 2. MenuType Data:
        ///    - Name must be non-empty and non-whitespace
        ///    - Time windows must be valid (both set or both null)
        ///    - DisplayOrder must be non-negative
        /// </remarks>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ===================================================================
            // TENANT CONSISTENCY VALIDATION
            // ===================================================================
            // ===================================================================
            // TENANT CONSISTENCY VALIDATION
            // ===================================================================
            
            // Validate restaurant ID
            if (RestaurantId <= 0)
            {
                yield return new ValidationResult(
                    "MenuType must be associated with a restaurant",
                    new[] { nameof(RestaurantId) }
                );
            }

            // Validate restaurant navigation consistency
            if (Restaurant != null && Restaurant.Id != RestaurantId)
            {
                yield return new ValidationResult(
                    "Restaurant navigation property is inconsistent with RestaurantId",
                    new[] { nameof(Restaurant), nameof(RestaurantId) }
                );
            }

            // Validate menus belong to same restaurant
            if (Menus != null && Menus.Any())
            {
                var inconsistentMenus = Menus
                    .Where(m => m.RestaurantId != RestaurantId)
                    .Select(m => new { m.Id, m.Name })
                    .ToList();

                if (inconsistentMenus.Any())
                {
                    yield return new ValidationResult(
                        $"MenuType contains menus from different restaurants. Inconsistent menus: {string.Join(", ", inconsistentMenus.Select(m => $"{m.Name} (ID: {m.Id})"))}",
                        new[] { nameof(Menus), nameof(RestaurantId) }
                    );
                }
            }

            // ===================================================================
            // BUSINESS RULE VALIDATION
            // ===================================================================

            // Name validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "MenuType name must not be empty or whitespace",
                    new[] { nameof(Name) }
                );
            }

            // Default time range validation
            if (DefaultStartTime.HasValue && DefaultEndTime.HasValue)
            {
                if (DefaultStartTime.Value == DefaultEndTime.Value)
                {
                    yield return new ValidationResult(
                        "Default start and end times cannot be identical",
                        new[] { nameof(DefaultStartTime), nameof(DefaultEndTime) }
                    );
                }
            }
            else if ((DefaultStartTime.HasValue && !DefaultEndTime.HasValue) || 
                     (!DefaultStartTime.HasValue && DefaultEndTime.HasValue))
            {
                yield return new ValidationResult(
                    "Both default start and end times must be set if either is provided",
                    new[] { nameof(DefaultStartTime), nameof(DefaultEndTime) }
                );
            }

            // Display order validation
            if (DisplayOrder < 0)
            {
                yield return new ValidationResult(
                    "Display order must be non-negative",
                    new[] { nameof(DisplayOrder) }
                );
            }
        }
    }
}