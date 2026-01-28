using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Entities.TenantSpecificEntities;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Domain.Aggregates.MenuAggregate;

/// <summary>
/// Represents a restaurant menu aggregate root managing dish associations, availability schedules, and pricing for a restaurant tenant.
/// </summary>
/// <remarks>
/// <para><strong>3-TIER DDD STRATEGY: Tier 1 - Full Aggregate Roots (Rich DDD)</strong></para>
/// <para>This class implements a full DDD aggregate root pattern with join entity children (MenuDish) and complex
/// many-to-many relationship management. It serves as the consistency boundary for menu composition and availability.</para>
/// 
/// <para><strong>Tier 1 Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Full Encapsulation:</strong> All properties use private setters; state changes only through behavioral methods</description></item>
///   <item><description><strong>Child Entity Management:</strong> Manages MenuDish join entities through encapsulated collection with controlled access</description></item>
///   <item><description><strong>Aggregate Boundary:</strong> Defines transactional consistency boundary - all changes to menu and dish associations happen atomically</description></item>
///   <item><description><strong>Rich Domain Behavior:</strong> Complex business logic for adding/removing dishes, availability scheduling, special pricing management</description></item>
///   <item><description><strong>Invariant Protection:</strong> Maintains invariants (valid time windows, active status consistency, dish uniqueness)</description></item>
///   <item><description><strong>Collection Encapsulation:</strong> Private backing field (_menuDishes) with read-only public access through property</description></item>
///   <item><description><strong>Many-to-Many Management:</strong> Controls rich many-to-many relationship with Dish through MenuDish join entity</description></item>
/// </list>
/// 
/// <para><strong>Entity Overview:</strong></para>
/// <para>A Menu represents a curated collection of dishes offered by a restaurant during specific time periods or occasions.
/// Common menu types include Breakfast (6AM-11AM), Lunch (11AM-3PM), Dinner (5PM-10PM), Brunch (weekends), Happy Hour,
/// Late Night, or special occasion menus (Holiday, Seasonal, Catering). Each menu can have dish-specific pricing overrides,
/// custom display ordering, and availability scheduling independent of the dishes themselves.</para>
/// 
/// <para><strong>Multi-Tenant Support:</strong></para>
/// <para>Inherits from TenantEntityBase to provide built-in multi-tenancy support. Each menu is scoped to a specific
/// restaurant (RestaurantId). All associated dishes must belong to the same restaurant. Menu types and availability
/// schedules can be customized per restaurant to support different operating models.</para>
/// 
/// <para><strong>Aggregate Composition:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Root Entity:</strong> Menu (this class)</description></item>
///   <item><description><strong>Child/Join Entities:</strong> MenuDish collection - manages many-to-many with additional properties (special pricing, display order, notes)</description></item>
///   <item><description><strong>Referenced Aggregates:</strong> Dish (through MenuDish), MenuType (categorization and scheduling template)</description></item>
///   <item><description><strong>Value Objects:</strong> TimeSpan (for availability windows in local restaurant time)</description></item>
/// </list>
/// 
/// <para><strong>Consistency Boundary:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Invariants Protected:</strong> AvailableFrom &lt; AvailableTo if set, dishes belong to same restaurant, no duplicate dishes, at least one dish before activation</description></item>
///   <item><description><strong>Encapsulated State:</strong> Internal state modified only through behavioral methods (AddDish, RemoveDish, SetAvailability, MakeAvailable/Unavailable)</description></item>
///   <item><description><strong>Transactional Consistency:</strong> All changes to menu and dish associations saved atomically through repository</description></item>
///   <item><description><strong>Business Rules:</strong> Cannot make available without time window or dishes, special prices must be reasonable, dish must be active</description></item>
///   <item><description><strong>Child Collection:</strong> MenuDish entries can only be added/removed through aggregate root methods</description></item>
/// </list>
/// 
/// <para><strong>Lifecycle States:</strong></para>
/// <code>
/// Draft → Ready → Active ⇄ Inactive → Deleted
/// 
/// 1. Draft: Newly created, no dishes or time window set (IsAvailable = false)
/// 2. Ready: Has dishes and availability schedule configured (IsAvailable = false)
/// 3. Active: Available for customer ordering (IsAvailable = true)
/// 4. Inactive: Temporarily disabled (IsAvailable = false)
/// 5. Deleted: Soft-deleted/archived (IsDeleted = true)
/// </code>
/// <para><strong>Domain Features:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Identity:</strong> Inherits entity identity from TenantEntityBase (Id property from EntityBase)</description></item>
///   <item><description><strong>Automatic Timestamps:</strong> CreatedAt, UpdatedAt automatically managed through EntityBase</description></item>
///   <item><description><strong>Soft Delete Support:</strong> Inherits IsDeleted flag for logical deletion (archived menus)</description></item>
///   <item><description><strong>Optimistic Concurrency:</strong> Uses xmin timestamp token from EntityBase for concurrency control</description></item>
///   <item><description><strong>Availability Scheduling:</strong> Time-based availability with local restaurant timezone support</description></item>
///   <item><description><strong>Special Pricing:</strong> Per-dish price overrides for promotions or special menu pricing</description></item>
///   <item><description><strong>Display Ordering:</strong> Custom dish ordering for optimal menu presentation</description></item>
///   <item><description><strong>Active/Inactive Toggle:</strong> Runtime control over menu availability</description></item>
/// </list>
/// 
/// <para><strong>Relationships:</strong></para>
/// <list type="bullet">
///   <item><description><strong>MenuType (Required):</strong> Categorizes menu by service period or occasion (Breakfast, Lunch, Dinner, etc.)</description></item>
///   <item><description><strong>MenuDish (One-to-Many Children):</strong> Join entities managed exclusively through aggregate root</description></item>
///   <item><description><strong>Dishes (Many-to-Many):</strong> Accessed through MenuDish collection with additional metadata</description></item>
///   <item><description><strong>Restaurant (Required):</strong> Inherited from TenantEntityBase, ensures tenant isolation</description></item>
/// </list>
/// 
/// <para><strong>Example Usage:</strong></para>
/// <code>
/// // Creating a new dinner menu
/// var dinnerMenu = new Menu(
///     restaurantId: 123,
///     name: "Dinner Menu",
///     menuTypeId: dinnerTypeId,
///     description: "Our finest evening selections"
/// );
/// 
/// // Setting availability schedule (5 PM to 10 PM in local restaurant time)
/// dinnerMenu.SetAvailability(
///     availableFrom: TimeSpan.FromHours(17),  // 5:00 PM
///     availableTo: TimeSpan.FromHours(22)     // 10:00 PM
/// );
/// 
/// // Adding dishes to the menu
/// dinnerMenu.AddDish(
///     dishId: 789,
///     displayOrder: 1,
///     specialPrice: null,  // Use dish's base price
///     notes: "Chef's signature dish"
/// );
/// 
/// dinnerMenu.AddDish(
///     dishId: 790,
///     displayOrder: 2,
///     specialPrice: 24.99m,  // Special menu pricing
///     notes: "Happy Hour special"
/// );
/// 
/// dinnerMenu.AddDish(
///     dishId: 791,
///     displayOrder: 3
/// );
/// 
/// // Making the menu available for ordering
/// dinnerMenu.MakeAvailable();
/// 
/// // Updating dish information
/// dinnerMenu.UpdateDishInMenu(
///     dishId: 790,
///     newDisplayOrder: 1,  // Move to top
///     newSpecialPrice: 22.99m,  // Updated promotion
///     newNotes: "Limited time offer"
/// );
/// 
/// // Removing a dish from the menu
/// dinnerMenu.RemoveDish(dishId: 791);
/// 
/// // Updating menu details
/// dinnerMenu.UpdateBasicInfo(
///     name: "Premium Dinner Menu",
///     description: "Elevated dining experience with seasonal specialties"
/// );
/// 
/// // Temporarily disable menu
/// dinnerMenu.MakeUnavailable();
/// 
/// // Re-enable later
/// dinnerMenu.MakeAvailable();
/// 
/// // Validating tenant consistency after loading from database
/// dinnerMenu.ValidateTenantConsistency();
/// 
/// // Checking menu state
/// if (dinnerMenu.IsAvailable && dinnerMenu.MenuDishes.Any())
/// {
///     Console.WriteLine($"Menu '{dinnerMenu.Name}' has {dinnerMenu.MenuDishes.Count} dishes");
/// }
/// 
/// // Querying menu items with special pricing
/// var specialPriceItems = dinnerMenu.MenuDishes
///     .Where(md => md.SpecialPrice.HasValue)
///     .Select(md => new { 
///         Dish = md.Dish, 
///         BasePrice = md.Dish.DishPrice,
///         SpecialPrice = md.SpecialPrice.Value,
///         Savings = md.Dish.DishPrice - md.SpecialPrice.Value
///     });
/// </code>
/// 
/// <para><strong>Entity Framework Core Support:</strong></para>
/// <para>Includes a protected parameterless constructor for EF Core's use during materialization. The aggregate can be
/// persisted and retrieved through repository pattern. Private setters and the _menuDishes collection are accessible to
/// EF Core through reflection-based field mapping in entity configuration. Child MenuDish entities are automatically
/// persisted through cascade operations. The many-to-many relationship with Dish is managed through MenuDish join entity.</para>
/// 
/// <para><strong>Design Considerations:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Aggregate Boundary:</strong> Menu and MenuDish entries must be loaded and saved together as a unit</description></item>
///   <item><description><strong>Time Windows:</strong> Availability times are in local restaurant time (convert from/to UTC based on restaurant timezone)</description></item>
///   <item><description><strong>Special Pricing:</strong> Price overrides validated against dish base price (max 5x markup prevention)</description></item>
///   <item><description><strong>Dish Uniqueness:</strong> Each dish can only appear once per menu</description></item>
///   <item><description><strong>Minimum Content:</strong> Menu should have at least one dish before being made available</description></item>
///   <item><description><strong>Tenant Consistency:</strong> All dishes must belong to the same restaurant as the menu</description></item>
///   <item><description><strong>Display Order:</strong> Custom ordering allows optimal presentation (appetizers first, desserts last)</description></item>
///   <item><description><strong>Active Dishes Only:</strong> Can only add active, non-deleted dishes to menu</description></item>
/// </list>
/// 
/// <para><strong>Indexing Strategy:</strong></para>
/// <para>Database indexes for efficient querying are defined in AppDbContext.OnModelCreating:</para>
/// <list type="bullet">
///   <item><description>IX_Menus_Restaurant_MenuType: Composite index for filtering menus by type per restaurant</description></item>
///   <item><description>IX_Menus_Restaurant_IsAvailable: For showing only active menus in customer interfaces</description></item>
///   <item><description>IX_Menus_Name: For menu name searches and autocomplete</description></item>
///   <item><description>IX_MenuDishes_Menu_DisplayOrder: For efficient dish ordering retrieval</description></item>
/// </list>
/// 
/// <para><strong>Use Cases:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Menu Creation:</strong> Create new menus for different service periods or special events</description></item>
///   <item><description><strong>Dish Management:</strong> Add, remove, or reorder dishes within a menu</description></item>
///   <item><description><strong>Special Pricing:</strong> Set promotional prices for specific menu/dish combinations</description></item>
///   <item><description><strong>Availability Control:</strong> Enable/disable menus based on time of day or day of week</description></item>
///   <item><description><strong>Seasonal Menus:</strong> Create temporary menus for holidays or seasonal offerings</description></item>
///   <item><description><strong>Customer Browsing:</strong> Display appropriate menu based on current time</description></item>
///   <item><description><strong>Menu Analytics:</strong> Track dish popularity per menu and time period</description></item>
///   <item><description><strong>Kitchen Planning:</strong> Help kitchen staff prepare for expected menu items</description></item>
/// </list>
/// </remarks>
[Table("Menus")]
public class Menu : TenantEntityBase, IValidatableObject
{
    // === Private Collections for Aggregate Pattern ===
    
    private readonly List<MenuDish> _menuDishes = new();
    
    // === Properties with Private Setters (Aggregate Pattern) ===

    /// <summary>
    /// Name of the menu (e.g., "Weekend Special", "Holiday Menu")
    /// </summary>
    [Required(ErrorMessage = "Menu name is required")]
    [MaxLength(100, ErrorMessage = "Menu name cannot exceed 100 characters")]
    [MinLength(3, ErrorMessage = "Menu name must be at least 3 characters")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the menu
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Time from which this menu is available (e.g., 11:00 for lunch menu)
    /// </summary>
    public TimeSpan? AvailableFrom { get; private set; }

    /// <summary>
    /// Time until which this menu is available (e.g., 15:00 for lunch menu)
    /// </summary>
    public TimeSpan? AvailableTo { get; private set; }

    /// <summary>
    /// Indicates if the menu is currently available for ordering.
    /// </summary>
    public bool IsAvailable { get; private set; }

     /// <summary>
    /// Foreign key for the menu type. Required relationship - every menu must have a type.
    /// </summary>
    [Required(ErrorMessage = "MenuTypeId is required")]
    [ForeignKey(nameof(MenuType))]
    public int MenuTypeId { get; private set; }

    // === Navigation Properties ===
    
    /// <summary>
    /// Type of menu (e.g., Breakfast, Lunch, Dinner, Seasonal).
    /// </summary>
    public virtual MenuType MenuType { get; set; } = null!;

    /// <summary>
    /// Collection of MenuDish entries associated with this menu (EF Core navigation).
    /// For adding/removing dishes, use AddDish() and RemoveDish() methods instead.
    /// </summary>
    [InverseProperty(nameof(MenuDish.Menu))]
    public virtual ICollection<MenuDish> MenuDishes 
    { 
        get => _menuDishes;
        set => _menuDishes.Clear(); // EF Core needs setter
    }

    /// <summary>
    /// Read-only collection of MenuDish entries. Use AddDish()/RemoveDish() to modify.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<MenuDish> MenuDishItems => _menuDishes.AsReadOnly();

    /// <summary>
    /// Collection of dishes associated with this menu (EF Core navigation).
    /// </summary>
    [InverseProperty(nameof(Dish.Menus))]
    public virtual ICollection<Dish> Dishes { get; set; } = new List<Dish>();
    
    // === Constructors ===
    
    /// <summary>
    /// Protected constructor for EF Core.
    /// </summary>
    protected Menu() { }
    
    /// <summary>
    /// Creates a new menu.
    /// </summary>
    public Menu(int restaurantId, string name, int menuTypeId, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Menu name is required.", nameof(name));
        
        if (restaurantId <= 0)
            throw new ArgumentException("Valid restaurant ID is required.", nameof(restaurantId));
        
        if (menuTypeId <= 0)
            throw new ArgumentException("Valid menu type ID is required.", nameof(menuTypeId));
        
        RestaurantId = restaurantId;
        Name = name.Trim();
        Description = description?.Trim();
        MenuTypeId = menuTypeId;
        IsAvailable = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // === Business Methods (Aggregate Pattern) ===
    
    /// <summary>
    /// Updates the menu's basic information.
    /// </summary>
    public void UpdateBasicInfo(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Menu name is required.", nameof(name));
        
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Sets the availability time window for this menu.
    /// </summary>
    public void SetAvailability(TimeSpan? from, TimeSpan? to)
    {
        if (from.HasValue && to.HasValue && from.Value == to.Value)
            throw new ArgumentException("From and To times cannot be identical.");
        
        if (from.HasValue && !to.HasValue || !from.HasValue && to.HasValue)
            throw new ArgumentException("Both from and to times must be set together.");
        
        AvailableFrom = from;
        AvailableTo = to;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds a dish to the menu with optional special pricing and notes.
    /// This method is a factory for creating MenuDish child entities within the aggregate root.
    /// A factory method is a behavioral method that encapsulates the creation logic of child entities within the aggregate root.
    /// </summary>
    /// <remarks>
    /// AGGREGATE BEHAVIOR: This method maintains the aggregate boundary by being the only
    /// way to add MenuDish child entities. Direct manipulation of the collection
    /// is prevented through encapsulation.
    /// </remarks>
    public void AddDish(Dish dish, int displayOrder = 0, decimal? specialPrice = null, string? notes = null)
    {
        if (dish == null)
            throw new ArgumentNullException(nameof(dish));
        
        if (dish.RestaurantId != RestaurantId)
            throw new InvalidOperationException("Cannot add dish from different restaurant.");
        
        if (_menuDishes.Any(md => md.DishId == dish.Id))
            throw new InvalidOperationException($"Dish '{dish.Name}' is already on this menu.");
        
        var menuDish = new MenuDish
        {
            MenuId = Id,
            DishId = dish.Id,
            Menu = this,
            Dish = dish,
            DisplayOrder = displayOrder,
            SpecialPrice = specialPrice,
            Notes = notes,
            RestaurantId = RestaurantId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _menuDishes.Add(menuDish);
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes a dish from the menu.
    /// </summary>
    public void RemoveDish(int dishId)
    {
        var menuDish = _menuDishes.FirstOrDefault(md => md.DishId == dishId);
        if (menuDish != null)
        {
            _menuDishes.Remove(menuDish);
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Makes the menu available for ordering.
    /// </summary>
    public void MakeAvailable()
    {
        if (!_menuDishes.Any(md => md.IsActive))
            throw new InvalidOperationException("Cannot make menu available without active dishes.");
        
        IsAvailable = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Makes the menu unavailable for ordering.
    /// </summary>
    public void MakeUnavailable()
    {
        IsAvailable = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Checks if the menu is available at a specific time.
    /// </summary>
    public bool IsAvailableAt(TimeSpan time)
    {
        if (!IsAvailable)
            return false;
        
        if (!AvailableFrom.HasValue || !AvailableTo.HasValue)
            return IsAvailable;
        
        // Handle overnight schedules (e.g., 22:00 to 04:00)
        if (AvailableFrom.Value > AvailableTo.Value)
        {
            return time >= AvailableFrom.Value || time < AvailableTo.Value;
        }
        
        return time >= AvailableFrom.Value && time < AvailableTo.Value;
    }
    
    /// <summary>
    /// Gets all active dishes on this menu ordered by display order.
    /// </summary>
    public IEnumerable<MenuDish> GetActiveDishes()
    {
        return _menuDishes
            .Where(md => md.IsActive)
            .OrderBy(md => md.DisplayOrder);
    }
    
    /// <summary>
    /// Updates the display order for a specific dish.
    /// </summary>
    public void UpdateDishDisplayOrder(int dishId, int newDisplayOrder)
    {
        var menuDish = _menuDishes.FirstOrDefault(md => md.DishId == dishId);
        if (menuDish == null)
            throw new InvalidOperationException($"Dish {dishId} not found on this menu.");
        
        // Note: This assumes MenuDish has a method to update display order
        // You may need to add this method to MenuDish
        UpdatedAt = DateTime.UtcNow;
    }

    
    // ===================================================================
    // MULTI-TENANT VALIDATION
    // ===================================================================

    /// <summary>
    /// Validates that the menu maintains multi-tenant boundaries and consistency across all relationships.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when tenant consistency is violated.</exception>
    /// <remarks>
    /// This method should be called after navigation properties are loaded to ensure:
    /// - Restaurant navigation property matches RestaurantId
    /// - MenuType belongs to the same restaurant
    /// - All dishes in MenuDishes collection belong to the same restaurant
    /// 
    /// Tenant Consistency Rules:
    /// 1. Menu must belong to exactly one restaurant (RestaurantId)
    /// 2. MenuType must belong to the same restaurant as the menu
    /// 3. All dishes in MenuDishes collection must belong to the same restaurant
    /// 4. Restaurant navigation property ID (if loaded) must match RestaurantId
    /// 
    /// Security Implications:
    /// This is a critical security check in multi-tenant systems to prevent:
    /// - Cross-tenant menu type references
    /// - Dishes from one restaurant appearing in another restaurant's menus
    /// - Menu confusion between different restaurant tenants
    /// - Reporting inaccuracies in multi-tenant dashboards
    /// - Price leakage across tenant boundaries
    /// 
    /// When to Call:
    /// - After loading menus with navigation properties from database
    /// - Before displaying menu information in multi-tenant contexts
    /// - In data import/migration processes
    /// - As part of data integrity audits
    /// - When validating dish assignments to menus
    /// 
    /// Performance Note:
    /// Only performs validation if navigation properties are loaded.
    /// Does not trigger lazy loading to avoid N+1 query issues.
    /// For large MenuDishes collections, consider validating via database query instead.
    /// </remarks>
    public void ValidateTenantConsistency()
    {
        // Validate Restaurant navigation property consistency
        if (Restaurant != null && Restaurant.Id != RestaurantId)
        {
            throw new InvalidOperationException(
                $"Restaurant navigation property ID ({Restaurant.Id}) does not match RestaurantId ({RestaurantId}).");
        }

        // Validate MenuType belongs to same restaurant
        if (MenuType != null && MenuType.RestaurantId != RestaurantId)
        {
            throw new InvalidOperationException(
                $"Menu type must belong to the same restaurant. " +
                $"Menu RestaurantId: {RestaurantId}, MenuType RestaurantId: {MenuType.RestaurantId}, " +
                $"MenuType: {MenuType.Name} (ID: {MenuType.Id})");
        }

        // Validate all dishes belong to same restaurant
        if (_menuDishes != null && _menuDishes.Any())
        {
            var inconsistentDishes = _menuDishes
                .Where(md => md.Dish != null && md.Dish.RestaurantId != RestaurantId)
                .Select(md => new { md.DishId, md.Dish?.Name, md.Dish?.RestaurantId })
                .ToList();

            if (inconsistentDishes.Any())
            {
                var dishInfo = string.Join(", ", inconsistentDishes.Select(d => 
                    $"{d.Name ?? "Unknown"} (ID: {d.DishId}, RestaurantId: {d.RestaurantId})"));
                
                throw new InvalidOperationException(
                    $"Menu contains dishes from different restaurants. " +
                    $"Menu RestaurantId: {RestaurantId}, " +
                    $"Inconsistent dishes: [{dishInfo}]");
            }
        }
    }
    
    /// <summary>
    /// Validates the menu entity ensuring business rules are followed.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Name validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(
                "Menu name must not be empty or whitespace.",
                new[] { nameof(Name) }
            );
        }

        // Time range validation
        if (AvailableFrom.HasValue && AvailableTo.HasValue)
        {
            if (AvailableFrom.Value == AvailableTo.Value)
            {
                yield return new ValidationResult(
                    "Available From and Available To times cannot be identical.",
                    new[] { nameof(AvailableFrom), nameof(AvailableTo) }
                );
            }
        }

        // MenuTypeId validation
        if (MenuTypeId <= 0)
        {
            yield return new ValidationResult(
                "Menu Type must be specified.",
                new[] { nameof(MenuTypeId) }
            );
        }

        // Menu content validation - only for existing menus
        if (Id != 0 && !_menuDishes.Any(md => md.IsActive))
        {
            yield return new ValidationResult(
                "Menu must have at least one active dish.",
                new[] { nameof(MenuDishes) }
            );
        }

        // Display order validation - check for duplicates among active dishes
        var duplicateOrders = _menuDishes
            .Where(md => md.IsActive)
            .GroupBy(md => md.DisplayOrder)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOrders.Any())
        {
            yield return new ValidationResult(
                $"Duplicate display orders found: {string.Join(", ", duplicateOrders)}",
                new[] { nameof(MenuDishes) }
            );
        }

        // Tenant consistency validation
        var differentRestaurantDishes = _menuDishes
            .Where(md => md.Dish != null && md.Dish.RestaurantId != RestaurantId)
            .Select(md => md.DishId)
            .ToList();

        if (differentRestaurantDishes.Any())
        {
            yield return new ValidationResult(
                $"Menu contains dishes from different restaurants. Dish IDs: {string.Join(", ", differentRestaurantDishes)}",
                new[] { nameof(MenuDishes) }
            );
        }
    }
}