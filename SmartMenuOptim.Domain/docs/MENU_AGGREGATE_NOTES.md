# Menu Aggregate - Implementation Notes

## Overview

The `Menu` entity in `SmartMenuOptim.Domain/Entities/TenantSpecificEntities` has been enhanced with **Aggregate Pattern** from Domain-Driven Design while maintaining compatibility with your existing data model.

## Data Model

```
Menu (Aggregate Root)
├── Has many MenuDish (Join Entity)
│   ├── MenuId (FK to Menu)
│   ├── DishId (FK to Dish)
│   ├── DisplayOrder
│   ├── SpecialPrice
│   └── Notes
└── Through MenuDish, relates to many Dish entities
```

## Why This Design?

Your existing model uses a **many-to-many relationship with a join entity** (MenuDish), which is MORE sophisticated than a simple collection:

- **Menu** can have multiple **Dishes**
- **Dish** can appear on multiple **Menus**
- **MenuDish** stores extra info (special price, display order, notes) for each menu-dish combination

This is the **correct DDD approach** - Dish is a separate entity (potentially a separate aggregate) that can exist independently and be shared across menus.

## Aggregate Pattern Features Added

### 1. **Private Setters**
```csharp
public string Name { get; private set; }  // Can only be changed through methods
```

### 2. **Encapsulated Collections**
```csharp
private readonly List<MenuDish> _menuDishes = new();
public IReadOnlyCollection<MenuDish> MenuDishItems => _menuDishes.AsReadOnly();
```

### 3. **Behavioral Methods**
```csharp
// ✅ DO: Use methods that enforce business rules
menu.AddDish(dish, displayOrder: 1);
menu.RemoveDish(dishId);
menu.MakeAvailable();

// ❌ DON'T: Direct property access
menu.Name = "New Name";  // Won't compile - private setter
menu.MenuDishes.Add(...); // Won't work - read-only collection
```

### 4. **Business Rules Enforced**
- Can't add dish from different restaurant
- Can't add same dish twice
- Can't make menu available without active dishes
- Validates time ranges (supports overnight schedules)

## Usage Examples

### Creating a Menu
```csharp
var menu = new Menu(
    restaurantId: 1,
    name: "Dinner Menu",
    menuTypeId: dinnerTypeId,
    description: "Our evening specials"
);
```

### Adding Dishes
```csharp
// Get the dish entity first
var burger = await dishRepository.GetByIdAsync(burgerId);

// Add to menu with special pricing
menu.AddDish(
    dish: burger,
    displayOrder: 1,
    specialPrice: 13.99m,  // Override default price
    notes: "Chef's recommendation"
);
```

### Setting Availability
```csharp
// Available from 5 PM to 10 PM
menu.SetAvailability(
    TimeSpan.FromHours(17),  // 5:00 PM
    TimeSpan.FromHours(22)   // 10:00 PM
);

// Make available for ordering
menu.MakeAvailable();
```

### Checking Availability
```csharp
// Check if available now
if (menu.IsAvailableAt(DateTime.Now.TimeOfDay))
{
    var dishes = menu.GetActiveDishes();
    // Show menu to customers
}
```

### Loading from Database
```csharp
var menu = await context.Menus
    .Include(m => m.MenuDishes)
        .ThenInclude(md => md.Dish)  // Load related dishes
    .Include(m => m.MenuType)
    .FirstOrDefaultAsync(m => m.Id == menuId);
```

## Comparison with Original Aggregate Examples

The example aggregates created earlier (in `/Aggregates/MenuAggregate/`) were simpler:
- `Menu` contained `MenuItem` directly
- `MenuItem` had all properties (price, description, etc.)

Your actual model is better because:
- **Dish can exist independently** - a dish can be created once and added to multiple menus
- **MenuDish stores menu-specific data** - same dish can have different prices/notes on different menus
- **Better normalization** - dish information isn't duplicated across menus
- **More flexible** - dishes can be managed separately from menus

## Migration from Old Code

If you have code using the old aggregate pattern:

**Old (from examples):**
```csharp
var burger = new MenuItem("Burger", price, taxRate, "Main");
menu.AddItem(burger);
```

**New (your actual model):**
```csharp
// Dish is created separately and can be reused
var burger = new Dish(restaurantId, "Burger", categoryId, price);
await dishRepository.SaveAsync(burger);

// Then added to menus
menu.AddDish(burger, displayOrder: 1);
```

## Key Takeaways

✅ **Your Menu entity** is now an aggregate root with:
- Proper encapsulation
- Business rules enforcement
- Validation logic
- Behavioral methods

✅ **MenuDish** remains a join entity for many-to-many relationship

✅ **Dish** is a separate entity (potentially its own aggregate) that can be shared across menus

✅ **Compatible with EF Core** - all existing database mappings still work

This gives you the **best of both worlds**: 
- DDD aggregate pattern for business logic protection
- Flexible many-to-many relationship for your data model
