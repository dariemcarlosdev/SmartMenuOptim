namespace SmartMenuOptim.Domain.Events.MenuEvents;

/// <summary>
/// Domain event raised when a dish is added to a restaurant's menu.
/// </summary>
/// <remarks>
/// <para><strong>Event Trigger:</strong></para>
/// <para>This event is raised by the Menu aggregate when a new dish is added to the menu,
/// either as a new item or when an existing dish is linked to a different menu.</para>
/// 
/// <para><strong>Typical Event Handlers:</strong></para>
/// <list type="bullet">
///     <item><description><strong>SearchIndexHandler:</strong> Updates search indexes for menu discovery</description></item>
///     <item><description><strong>InventoryHandler:</strong> Verifies ingredient availability for the new dish</description></item>
///     <item><description><strong>PricingHandler:</strong> Validates pricing against category standards</description></item>
///     <item><description><strong>NotificationHandler:</strong> Notifies subscribed customers of new menu items</description></item>
///     <item><description><strong>AnalyticsHandler:</strong> Initializes tracking for new dish performance</description></item>
///     <item><description><strong>CacheHandler:</strong> Invalidates menu caches to reflect changes</description></item>
///     <item><description><strong>AIRecommendationHandler:</strong> Updates AI model with new menu item</description></item>
/// </list>
/// 
/// <para><strong>Business Rules:</strong></para>
/// <list type="bullet">
///     <item><description>Dish must have valid pricing before being added to menu</description></item>
///     <item><description>Dish must belong to a valid category</description></item>
///     <item><description>Seasonal items may have start/end dates</description></item>
///     <item><description>Special dietary information must be accurate (allergens, vegetarian, etc.)</description></item>
/// </list>
/// 
/// <para><strong>Menu Types:</strong></para>
/// <list type="bullet">
///     <item><description><strong>Regular:</strong> Standard all-day menu</description></item>
///     <item><description><strong>Breakfast:</strong> Morning menu items</description></item>
///     <item><description><strong>Lunch:</strong> Lunch specials and combos</description></item>
///     <item><description><strong>Dinner:</strong> Evening menu items</description></item>
///     <item><description><strong>Seasonal:</strong> Limited-time seasonal offerings</description></item>
///     <item><description><strong>Special:</strong> Holiday or event-specific items</description></item>
/// </list>
/// </remarks>
public sealed class DishAddedToMenuEvent : DomainEventBase
{
    /// <summary>
    /// Gets the unique identifier of the menu.
    /// </summary>
    public int MenuId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the dish being added.
    /// </summary>
    public int DishId { get; init; }

    /// <summary>
    /// Gets the restaurant (tenant) identifier.
    /// </summary>
    public int RestaurantId { get; init; }

    /// <summary>
    /// Gets the name of the dish being added.
    /// </summary>
    public string DishName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the description of the dish.
    /// </summary>
    public string? DishDescription { get; init; }

    /// <summary>
    /// Gets the price of the dish.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the currency code for the price.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Gets the category identifier of the dish.
    /// </summary>
    public int CategoryId { get; init; }

    /// <summary>
    /// Gets the category name of the dish.
    /// </summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the menu type.
    /// </summary>
    public string MenuType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the menu name.
    /// </summary>
    public string MenuName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display order/position of the dish on the menu.
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Gets whether this is a featured/highlighted item.
    /// </summary>
    public bool IsFeatured { get; init; }

    /// <summary>
    /// Gets whether this is a new item (for "NEW" badge display).
    /// </summary>
    public bool IsNewItem { get; init; } = true;

    /// <summary>
    /// Gets the availability start date for seasonal items.
    /// </summary>
    public DateTime? AvailableFrom { get; init; }

    /// <summary>
    /// Gets the availability end date for seasonal items.
    /// </summary>
    public DateTime? AvailableUntil { get; init; }

    /// <summary>
    /// Gets dietary flags for the dish.
    /// </summary>
    public List<string> DietaryFlags { get; init; } = new();

    /// <summary>
    /// Gets allergen information for the dish.
    /// </summary>
    public List<string> Allergens { get; init; } = new();

    /// <summary>
    /// Gets the staff member ID who added the dish.
    /// </summary>
    public int? AddedByStaffId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DishAddedToMenuEvent"/> class.
    /// </summary>
    public DishAddedToMenuEvent(
        int menuId,
        int dishId,
        int restaurantId,
        string dishName,
        decimal price,
        int categoryId,
        string categoryName,
        string menuType,
        string menuName,
        string currencyCode = "USD",
        string? dishDescription = null,
        int displayOrder = 0,
        bool isFeatured = false,
        bool isNewItem = true,
        DateTime? availableFrom = null,
        DateTime? availableUntil = null,
        List<string>? dietaryFlags = null,
        List<string>? allergens = null,
        int? addedByStaffId = null)
    {
        MenuId = menuId;
        DishId = dishId;
        RestaurantId = restaurantId;
        DishName = dishName;
        DishDescription = dishDescription;
        Price = price;
        CurrencyCode = currencyCode;
        CategoryId = categoryId;
        CategoryName = categoryName;
        MenuType = menuType;
        MenuName = menuName;
        DisplayOrder = displayOrder;
        IsFeatured = isFeatured;
        IsNewItem = isNewItem;
        AvailableFrom = availableFrom;
        AvailableUntil = availableUntil;
        DietaryFlags = dietaryFlags ?? new List<string>();
        Allergens = allergens ?? new List<string>();
        AddedByStaffId = addedByStaffId;
    }

    /// <summary>
    /// Private parameterless constructor for serialization support.
    /// </summary>
    private DishAddedToMenuEvent() { }
}
