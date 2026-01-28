# Lookup Aggregates - Guide

## What Are Lookup Aggregates?

**Lookup Aggregates** are simpler aggregate roots that provide **reference data** and **categorization** for the main business aggregates. They follow the aggregate pattern but have a simpler structure.

### Characteristics

| Feature | Main Aggregate | Lookup Aggregate |
|---------|---------------|------------------|
| **Complexity** | High | Low |
| **Child Entities** | Usually has them | Usually standalone |
| **Business Logic** | Complex lifecycle | Simple categorization |
| **Purpose** | Core business operations | Reference/lookup data |
| **Example** | Order, Restaurant, Menu | Category, MenuType |

---

## Lookup Aggregates in This Project

### 🏷️ MenuType

**Purpose:** Categorize menus (Breakfast, Lunch, Dinner, Seasonal, etc.)

**Key Features:**
- Default time templates for menus
- Display ordering
- Multi-tenant (per restaurant)

**Usage:**
```csharp
// Create menu type with default times
var breakfast = new MenuType(
    restaurantId: 1,
    name: "Breakfast",
    description: "Morning meals from 6 AM to 11 AM"
);

// Set template times that menus can use
breakfast.SetDefaultTimes(
    TimeSpan.FromHours(6),   // 6:00 AM
    TimeSpan.FromHours(11)   // 11:00 AM
);

breakfast.UpdateDisplayOrder(1);

// Use in menu creation
var menu = new Menu(restaurantId, "Weekend Brunch", breakfast.Id);
// Menu can use breakfast's default times as starting point
if (breakfast.DefaultStartTime.HasValue)
{
    menu.SetAvailability(breakfast.DefaultStartTime, breakfast.DefaultEndTime);
}
```

**Business Methods:**
- `UpdateBasicInfo(name, description)` - Update name/description
- `SetDefaultTimes(start, end)` - Set template times
- `ClearDefaultTimes()` - Remove template times
- `UpdateDisplayOrder(order)` - Set sort order

---

### 📑 Category

**Purpose:** Organize dishes into logical groups (Appetizers, Main Course, Desserts, etc.)

**Key Features:**
- Organize dishes for navigation
- Display ordering
- Multi-tenant (per restaurant)

**Usage:**
```csharp
// Create category
var mainCourse = new Category(
    restaurantId: 1,
    name: "Main Course",
    description: "Our signature entrees",
    displayOrder: 2
);

// Update information
mainCourse.UpdateBasicInfo("Main Dishes", "Our best sellers");
mainCourse.UpdateDisplayOrder(3);

// Use in dish creation
var steak = new Dish(
    restaurantId: 1,
    name: "Ribeye Steak",
    categoryId: mainCourse.Id,
    price: 29.99m
);
```

**Business Methods:**
- `UpdateBasicInfo(name, description)` - Update name/description
- `UpdateDisplayOrder(order)` - Set sort order (max 1000)

---

### 🔄 OrderStatus

**Purpose:** Define workflow states for orders (Pending, Preparing, Ready, Completed, Cancelled, etc.)

**Key Features:**
- Order workflow management
- Display ordering
- Terminal state flag (for completed/cancelled states)
- Color coding for UI
- Multi-tenant (per restaurant)

**Usage:**
```csharp
// Create workflow statuses
var pending = new OrderStatus(
    restaurantId: 1,
    name: "Pending",
    displayOrder: 1,
    isTerminal: false,
    colorCode: "#FFA500",  // Orange
    description: "Order placed, awaiting confirmation"
);

var preparing = new OrderStatus(
    restaurantId: 1,
    name: "Preparing",
    displayOrder: 2,
    isTerminal: false,
    colorCode: "#007BFF"  // Blue
);

var completed = new OrderStatus(
    restaurantId: 1,
    name: "Completed",
    displayOrder: 10,
    isTerminal: true,  // Cannot transition from this state
    colorCode: "#28A745"  // Green
);

// Update status properties
pending.SetColorCode("#FF8C00");
preparing.SetTerminal(false);

// Use in order workflow
var order = new Order(restaurantId, customerId, pending.Id);
order.UpdateStatus(preparing.Id);
order.UpdateStatus(completed.Id);
```

**Business Methods:**
- `UpdateBasicInfo(name, description)` - Update name/description
- `UpdateDisplayOrder(order)` - Set sort order
- `SetTerminal(isTerminal)` - Mark as terminal state
- `SetColorCode(colorCode)` - Set UI color (hex format #RRGGBB)

---

## Why Separate from Main Aggregates?

### Location Strategy

**Main Aggregates** → `/Aggregates/`
- Complex business logic
- Have child entities
- Complex lifecycle management

**Lookup Aggregates** → `/Entities/TenantSpecificEntities/`
- Simple reference data
- Standalone entities
- Simple CRUD operations

### Benefits

1. **Clear Separation** - Easy to identify lookup/reference data
2. **Simpler Implementation** - Less complex than main aggregates
3. **Reusability** - Referenced by multiple other aggregates
4. **Performance** - Can be cached since they change infrequently

---

## Common Patterns

### Pattern 1: Seeding Lookup Data

```csharp
public class DataSeeder
{
    public static void SeedMenuTypes(int restaurantId, ApplicationDbContext context)
    {
        var menuTypes = new[]
        {
            new MenuType(restaurantId, "Breakfast", "Morning meals")
                { DisplayOrder = 1 },
            new MenuType(restaurantId, "Lunch", "Midday meals")
                { DisplayOrder = 2 },
            new MenuType(restaurantId, "Dinner", "Evening meals")
                { DisplayOrder = 3 },
        };

        foreach (var menuType in menuTypes)
        {
            context.MenuTypes.Add(menuType);
        }
        
        context.SaveChanges();
    }
}
```

### Pattern 2: Caching Lookup Data

```csharp
public class MenuTypeService
{
    private readonly IMemoryCache _cache;
    private readonly IMenuTypeRepository _repository;
    
    public async Task<IEnumerable<MenuType>> GetAllAsync(int restaurantId)
    {
        var cacheKey = $"MenuTypes_{restaurantId}";
        
        if (!_cache.TryGetValue(cacheKey, out IEnumerable<MenuType> menuTypes))
        {
            menuTypes = await _repository.GetAllByRestaurantAsync(restaurantId);
            
            _cache.Set(cacheKey, menuTypes, TimeSpan.FromHours(1));
        }
        
        return menuTypes;
    }
}
```

### Pattern 3: Dropdown/Select Lists

```razor
@* Blazor component using lookup aggregates *@
<select @bind="selectedMenuTypeId">
    @foreach (var menuType in menuTypes.OrderBy(m => m.DisplayOrder))
    {
        <option value="@menuType.Id">@menuType.Name</option>
    }
</select>

@code {
    private IEnumerable<MenuType> menuTypes;
    private int selectedMenuTypeId;
    
    protected override async Task OnInitializedAsync()
    {
        menuTypes = await MenuTypeService.GetAllAsync(restaurantId);
    }
}
```

---

## Validation Rules

### MenuType

✅ **Required:**
- Must belong to a restaurant
- Must have a name (1-100 chars)
- Display order must be non-negative

✅ **Optional:**
- Description (max 500 chars)
- Default start/end times (must be set together)

✅ **Business Rules:**
- Default times cannot be identical
- All menus must belong to same restaurant

### Category

✅ **Required:**
- Must belong to a restaurant
- Must have a name (2-50 chars, alphanumeric + spaces/hyphens)
- Display order must be non-negative (max 1000)

✅ **Optional:**
- Description (min 10 chars if provided, max 500)

✅ **Business Rules:**
- All dishes must belong to same restaurant
- Established categories should have at least one dish

### OrderStatus

✅ **Required:**
- Must belong to a restaurant
- Must have a name (1-50 chars)
- Display order must be non-negative
- Must specify IsTerminal flag

✅ **Optional:**
- Description (max 200 chars)
- ColorCode (hex format #RRGGBB)

✅ **Business Rules:**
- ColorCode must be valid hex format if provided
- All orders must belong to same restaurant
- Terminal statuses should not transition to other states (enforced by application logic)

---

## Testing Lookup Aggregates

```csharp
[TestClass]
public class MenuTypeTests
{
    [TestMethod]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var menuType = new MenuType(1, "Breakfast", "Morning meals");
        
        // Assert
        Assert.AreEqual("Breakfast", menuType.Name);
        Assert.AreEqual(1, menuType.RestaurantId);
    }
    
    [TestMethod]
    public void SetDefaultTimes_WithValidRange_ShouldSucceed()
    {
        // Arrange
        var menuType = new MenuType(1, "Breakfast");
        
        // Act
        menuType.SetDefaultTimes(
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(11)
        );
        
        // Assert
        Assert.AreEqual(TimeSpan.FromHours(6), menuType.DefaultStartTime);
        Assert.AreEqual(TimeSpan.FromHours(11), menuType.DefaultEndTime);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void SetDefaultTimes_WithIdenticalTimes_ShouldThrow()
    {
        // Arrange
        var menuType = new MenuType(1, "Lunch");
        
        // Act
        menuType.SetDefaultTimes(
            TimeSpan.FromHours(12),
            TimeSpan.FromHours(12)  // Same time!
        );
    }
}

[TestClass]
public class CategoryTests
{
    [TestMethod]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var category = new Category(1, "Main Course", "Our entrees");
        
        // Assert
        Assert.AreEqual("Main Course", category.Name);
        Assert.AreEqual(1, category.RestaurantId);
    }
    
    [TestMethod]
    public void UpdateDisplayOrder_WithValidOrder_ShouldSucceed()
    {
        // Arrange
        var category = new Category(1, "Desserts");
        
        // Act
        category.UpdateDisplayOrder(5);
        
        // Assert
        Assert.AreEqual(5, category.DisplayOrder);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void UpdateDisplayOrder_ExceedingMax_ShouldThrow()
    {
        // Arrange
        var category = new Category(1, "Drinks");
        
        // Act
        category.UpdateDisplayOrder(1001);  // Over max of 1000
    }
}
```

---

## Key Takeaways

1. **Simpler than main aggregates** - Less complexity, no child entities
2. **Reference data** - Provide categorization and lookups
3. **Follow aggregate pattern** - Private setters, behavioral methods, validation
4. **Cached frequently** - Change less often, good candidates for caching
5. **Referenced by ID** - Other aggregates use their IDs, not object references
6. **Per-tenant** - Each restaurant has its own set of lookup data

---

## When to Create a Lookup Aggregate

Create a lookup aggregate when you need:
- ✅ Reference data that's shared across entities
- ✅ Categorization/classification of domain concepts
- ✅ Dropdown/select list data
- ✅ Tenant-specific reference data
- ✅ Ordered/sortable categories

**Examples:**
- MenuType (categorizes menus)
- Category (categorizes dishes)
- OrderStatus (categorizes order states)
- PaymentMethod (categorizes payment types)
- DeliveryZone (categorizes delivery areas)
