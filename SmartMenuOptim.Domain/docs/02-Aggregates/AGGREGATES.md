# Aggregates - Simple Guide

**TL;DR:** Aggregates are "packages" of related objects that must stay consistent. Like an order with its items - they're always saved and loaded together, and you can only change them through the "boss" (aggregate root).

## Quick Navigation
- [What Are They?](#what-are-aggregates)
- [The 3 Aggregates in This Project](#our-aggregates)
- [How to Use Them](#how-to-use-aggregates)
- [Golden Rules](#golden-rules)
- [Common Patterns](#common-patterns)

---

## What Are Aggregates?

An **aggregate** = related objects that must always be consistent together.

**Real-world example:** A restaurant order
- **Order** (the boss/root) → controls everything
- **OrderItems** (the workers) → managed by the order
- **Money, Address** (the tools) → used by the order

```csharp
// ✅ RIGHT: Always go through the boss
order.AddItem(burger, 2);
order.Confirm();

// ❌ WRONG: Don't bypass the boss  
var item = new OrderItem(...);
database.Save(item);  // Breaks the rules!
```

**Why?** So the order total ALWAYS matches its items. No orphaned items, no broken totals.

---

## DDD Best Practices - What All Aggregates Follow

**All aggregates in this project follow Domain-Driven Design (DDD) best practices:**

### 1. ✅ Private Setters (Encapsulation)
Properties can only be changed through behavioral methods, not direct property assignment.

```csharp
// ✅ Enforced in all aggregates
public string Name { get; private set; }  // Can't set directly

// ❌ Won't compile
restaurant.Name = "New Name";  // ERROR!

// ✅ Must use behavioral method
restaurant.UpdateBasicInfo("New Name");  // Works!
```

**Why?** Ensures business rules are always enforced. Can't bypass validation.

### 2. ✅ Value Objects for Domain Concepts
Use Value Objects instead of primitive types for domain concepts.

```csharp
// ❌ Primitive obsession (old way)
public string Email { get; set; }
public string PhoneNumber { get; set; }

// ✅ Value Objects (our way)
public Email ContactEmail { get; private set; }
public PhoneNumber ContactPhone { get; private set; }
public Address Location { get; private set; }
```

**Why?** 
- Email validation is in `Email` class, not scattered everywhere
- Can't accidentally swap phone and email
- Type safety and reusability

**Our Value Objects:**
- `Email` - Validates email format
- `PhoneNumber` - Validates phone format
- `Address` - Encapsulates full address (Street, City, State, PostalCode, Country)
- `Money` - Handles currency amounts
- `Percentage` - Handles percentage values

### 3. ✅ Encapsulated Collections
Collections are private; access is through read-only interfaces.

```csharp
// ❌ Direct collection access (old way)
public ICollection<BusinessHours> OperatingHours { get; set; }

// ✅ Encapsulated collection (our way)
private readonly List<BusinessHours> _operatingHours = new();
public IReadOnlyCollection<BusinessHours> OperatingHours => _operatingHours.AsReadOnly();

// Add through method only
public void SetBusinessHours(DayOfWeek day, TimeSpan open, TimeSpan close) { }
```

**Why?** 
- Can't do `restaurant.OperatingHours.Add(...)` from outside
- Aggregate maintains consistency
- Business rules enforced

### 4. ✅ Rich Behavioral Methods
Aggregates have meaningful methods, not just getters/setters.

```csharp
// ✅ Rich behavior
public void StartAcceptingOrders()
{
    if (!_operatingHours.Any())
        throw new InvalidOperationException("Cannot accept orders without business hours.");
    
    IsAcceptingOrders = true;
    UpdatedAt = DateTime.UtcNow;
}

public bool IsOpenAt(DateTime dateTime) { /* business logic */ }
```

**Why?** Domain logic lives in the domain, not in application services.

### 5. ✅ Aggregate Boundaries Respected
Aggregates reference other aggregates by ID only, not by navigation properties.

```csharp
// ✅ Reference by ID
public int CustomerId { get; private set; }  // Just the ID
public int OrderStatusId { get; private set; }

// ⚠️ Navigation properties exist for EF Core only (hybrid pattern)
// See Restaurant.cs for detailed explanation
public virtual Customer? Customer { get; set; }  // FOR EF CORE ONLY
```

**Why?** 
- Maintains aggregate boundaries
- Prevents loading entire object graphs
- Forces use of repositories

### 6. ✅ Invariants Always Maintained
Aggregate ensures its rules are NEVER violated.

```csharp
// Restaurant aggregate
public void StartAcceptingOrders()
{
    // Invariant: Can't accept orders without business hours
    if (!_operatingHours.Any())
        throw new InvalidOperationException("Cannot accept orders without business hours.");
    
    IsAcceptingOrders = true;
}

// Order aggregate
private void RecalculateTotals()
{
    // Invariant: Total always matches sum of items
    TotalAmount = _orderItems.Sum(oi => oi.Subtotal);
}
```

**Why?** Data is always valid; can't get into inconsistent state.

### 7. ✅ Constructor Validation
Objects are valid from the moment they're created.

```csharp
public Restaurant(
    int ownerId,
    string name,
    Address location,
    PhoneNumber contactPhone,
    Email contactEmail,
    ...)
{
    // Validate immediately
    if (ownerId <= 0)
        throw new ArgumentException("Valid owner ID is required.");
    
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Restaurant name is required.");
    
    // Set properties
    OwnerId = ownerId;
    Name = name.Trim();
    // ...
}
```

**Why?** Can't create invalid objects; fail fast.

### 8. ✅ EF Core Compatibility (Hybrid Pattern)
Aggregates work directly with Entity Framework Core while maintaining DDD principles.

```csharp
// Protected parameterless constructor for EF Core
protected Restaurant() 
{
    Name = string.Empty;
    Location = null!;
}

// Public constructor for domain logic
public Restaurant(int ownerId, string name, ...) { }
```

**Why?** 
- No separate DTO needed for database
- EF Core can materialize aggregates directly
- Single source of truth

**See:** `Restaurant.cs` for full EF Core configuration examples with Value Objects.

### 9. ✅ Clear Documentation
Every aggregate has comprehensive documentation explaining its purpose and usage.

```csharp
/// <summary>
/// Restaurant Aggregate Root - Represents a restaurant entity with its business rules.
/// </summary>
/// <remarks>
/// Aggregate Characteristics:
/// - Root Entity: Restaurant
/// - Child Entities: BusinessHours
/// - Value Objects: Address, Email, PhoneNumber
/// 
/// Lifecycle States: ...
/// Aggregate Pattern Features: ...
/// Example Usage: ...
/// EF Core Configuration: ...
/// </remarks>
```

**Why?** Developers understand how to use aggregates correctly.

### Practical Example - All Practices Together

```csharp
// Create with validation (Practice #7)
var restaurant = new Restaurant(
    ownerId: 1,
    name: "Joe's Pizza",
    location: new Address(...),        // Value Object (Practice #2)
    contactPhone: new PhoneNumber(...), // Value Object (Practice #2)
    contactEmail: new Email(...)        // Value Object (Practice #2)
);

// Can't modify directly (Practice #1)
// restaurant.Name = "New Name";  // ERROR!

// Must use behavioral method (Practice #4)
restaurant.UpdateBasicInfo("Joe's Famous Pizza");

// Can't add hours directly (Practice #3)
// restaurant.OperatingHours.Add(...);  // ERROR!

// Must use method (Practice #4)
restaurant.SetBusinessHours(DayOfWeek.Monday, open, close);

// Invariant enforced (Practice #6)
restaurant.StartAcceptingOrders();  // Throws if no business hours

// Reference by ID only (Practice #5)
var order = new Order(restaurant.Id, customerId, statusId);  // Not restaurant object!
```

### Summary Table

| Practice | All Aggregates | Benefit |
|----------|----------------|---------|
| **Private Setters** | ✅ Restaurant, Menu, Order, Dish | Forces business logic through methods |
| **Value Objects** | ✅ Restaurant uses Email, Phone, Address | Type safety, validation, reusability |
| **Encapsulated Collections** | ✅ Restaurant (_operatingHours), Order (_orderItems) | Maintains consistency |
| **Rich Behavioral Methods** | ✅ All aggregates | Domain logic in domain |
| **ID References** | ✅ Between aggregates | Respects boundaries |
| **Invariants** | ✅ All aggregates | Always valid state |
| **Constructor Validation** | ✅ All aggregates | Fail fast |
| **EF Core Compatible** | ✅ All aggregates | No separate DTOs |
| **Well Documented** | ✅ All aggregates | Easy to understand |

### Related Documentation

- **Hybrid Pattern Explanation:** See `Restaurant.cs` navigation properties section
- **Lookup Aggregates:** See `LOOKUP_AGGREGATES.md` for MenuType, Category, OrderStatus
- **Consolidation Decision:** See `RESTAURANT_CONSOLIDATION.md` for architectural decisions

---

## Our Aggregates

### Main Aggregates

These are the core business aggregates with complex lifecycles and child entities:

#### 🏪 Restaurant
**What:** Restaurant info + operating hours  
**Root:** Restaurant  
**Children:** BusinessHours

```csharp
// Create restaurant
var restaurant = new Restaurant(
    ownerId: adminUserId,
    name: "Joe's Pizza",
    location: new Address("123 Main St", "New York", "NY", "10001", "US"),
    contactPhone: new PhoneNumber("+1-212-555-1234"),
    contactEmail: new Email("contact@joespizza.com"),
    maxSimultaneousOrders: 50,
    description: "Authentic New York style pizza",
    timeZoneId: "America/New_York"
);

// Set business hours
restaurant.SetBusinessHours(DayOfWeek.Monday, TimeSpan.FromHours(11), TimeSpan.FromHours(22));
restaurant.SetBusinessHours(DayOfWeek.Tuesday, TimeSpan.FromHours(11), TimeSpan.FromHours(22));

// Start accepting orders
restaurant.StartAcceptingOrders();

// Update operations
restaurant.UpdateBasicInfo("Joe's Famous Pizza", "Best pizza in town!");
restaurant.UpdateTimeZone("America/Chicago");
restaurant.UpdateContactInfo(newEmail, newPhone);

// Check if open
if (restaurant.IsOpenAt(DateTime.Now))
{
    Console.WriteLine("We're open for business!");
}
```

#### 🍔 Menu
**What:** Menu with dishes and pricing  
**Root:** Menu  
**Children:** MenuDish (join entity linking to Dish)
**Related:** Dish (separate entity, referenced by ID)

```csharp
// Create menu
var menu = new Menu(restaurantId, "Dinner Menu", menuTypeId, "Our evening specials");

// Add dish to menu (creates MenuDish join entity)
var burger = dishRepository.GetById(burgerId);
menu.AddDish(burger, displayOrder: 1, specialPrice: 13.99m, notes: "Chef's special");

// Set availability window
menu.SetAvailability(TimeSpan.FromHours(17), TimeSpan.FromHours(22));

// Make available
menu.MakeAvailable();

// Check availability at specific time
if (menu.IsAvailableAt(DateTime.Now.TimeOfDay))
{
    var activeDishes = menu.GetActiveDishes();
}
```

#### 📦 Order
**What:** Customer order from start to finish  
**Root:** Order  
**Children:** OrderItem

```csharp
// Create order
var order = new Order(restaurantId, customerId, pendingStatusId);

// Add items
order.AddItem(dishId: 1, dishName: "Burger", unitPrice: 12.99m, quantity: 2);
order.AddItem(dishId: 5, dishName: "Fries", unitPrice: 4.99m, quantity: 1);

// Set instructions
order.SetSpecialInstructions("Ring doorbell twice");

// Assign staff
order.AssignStaffMember(staffId);

// Update status through workflow
order.UpdateStatus(preparingStatusId);
order.UpdateStatus(readyStatusId);

// Check totals (automatically calculated)
var total = order.TotalAmount;  // Always matches items
var itemCount = order.GetItemCount();
```

---

### Lookup Aggregates

These are simpler aggregates that provide reference/categorization data:

#### 🏷️ MenuType (Lookup)
**What:** Menu categorization (Breakfast, Lunch, Dinner, etc.)  
**Root:** MenuType  
**Referenced by:** Menu

```csharp
// Create menu type
var breakfast = new MenuType(restaurantId, "Breakfast", "Morning meals");
breakfast.SetDefaultTimes(TimeSpan.FromHours(6), TimeSpan.FromHours(11));
breakfast.UpdateDisplayOrder(1);

// Use in menu
var menu = new Menu(restaurantId, "Weekend Brunch", breakfast.Id);
```

#### 📑 Category (Lookup)
**What:** Dish categorization (Appetizers, Main Course, Desserts, etc.)  
**Root:** Category  
**Referenced by:** Dish

```csharp
// Create category
var mainCourse = new Category(restaurantId, "Main Course", "Our signature entrees");
mainCourse.UpdateDisplayOrder(2);

// Use in dish
var dish = new Dish(restaurantId, "Steak", mainCourse.Id, price);
```

#### 🔄 OrderStatus (Lookup)
**What:** Order workflow states (Pending, Preparing, Completed, etc.)  
**Root:** OrderStatus  
**Referenced by:** Order

```csharp
// Create status
var pending = new OrderStatus(restaurantId, "Pending", displayOrder: 1, isTerminal: false, "#FFA500");
var completed = new OrderStatus(restaurantId, "Completed", displayOrder: 10, isTerminal: true, "#28A745");

// Use in order workflow
var order = new Order(restaurantId, customerId, pending.Id);
order.UpdateStatus(completed.Id);
```

---

## How to Use Aggregates

### Creating
```csharp
// Use the constructor
var menu = new Menu(restaurantId, "Lunch", MenuType.Lunch);
```

### Modifying
```csharp
// Use behavioral methods (not property setters!)
menu.AddItem(item);
menu.MakeAvailable();
```

### Loading
```csharp
// Always load root + children together
var menu = await _context.Menus
    .Include(m => m.Items)  // ⭐ Important!
    .FirstOrDefaultAsync(m => m.Id == id);
```

### Saving
```csharp
// One save for the whole aggregate
await menuRepository.SaveAsync(menu);  // Saves menu + all items
```

---

## Golden Rules

### ✅ DO

| Rule | Example |
|------|---------|
| Keep small | Menu + MenuItems (not Menu + MenuItems + Orders + ...) |
| Use private setters | `public string Name { get; private set; }` |
| Reference by ID | `public int CustomerId { get; private set; }` |
| One repository per root | `IMenuRepository` ✅, `IMenuItemRepository` ❌ |

### ❌ DON'T

| Mistake | Why |
|---------|-----|
| `order.Customer` | Use `order.CustomerId` (ID only) |
| `menu.Items.Add(item)` | Use `menu.AddItem(item)` (method) |
| `menu.IsAvailable = true` | Use `menu.MakeAvailable()` (method) |
| Save items separately | Save the whole aggregate at once |

### Quick Decision Tree
```
Do these ALWAYS need to be consistent?
├─ YES → Same aggregate
└─ NO  → Separate aggregates
```

---

## Common Patterns

### 1. Factory Methods
```csharp
public static Order CreateDeliveryOrder(int restaurantId, int customerId, ...)
{
    return new Order(restaurantId, customerId, ..., OrderType.Delivery, ...);
}

// Usage
var order = Order.CreateDeliveryOrder(...);
```

### 2. Validation
```csharp
public void MakeAvailable()
{
    if (!_items.Any())
        throw new InvalidOperationException("Menu needs items!");
    
    IsAvailable = true;
}
```

### 3. Idempotent Operations
```csharp
public void Confirm()
{
    if (Status == OrderStatus.Confirmed)
        return;  // Already done
    
    Status = OrderStatus.Confirmed;
}
```

### 4. Repository (Only for Roots!)
```csharp
public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(int id);
    Task<IEnumerable<Menu>> GetAllByRestaurantAsync(int restaurantId);
    Task SaveAsync(Menu menu);
}
```

---

## EF Core Configuration for Aggregates

All aggregates are configured in `Infrastructure/Data/AppDbContext.cs` using the **Fluent API** in `OnModelCreating`.

### Basic Pattern

```csharp
// Infrastructure/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure each aggregate
    ConfigureRestaurantAggregate(modelBuilder);
    ConfigureMenuAggregate(modelBuilder);
    ConfigureOrderAggregate(modelBuilder);
    ConfigureDishAggregate(modelBuilder);
    
    base.OnModelCreating(modelBuilder);
}
```

### 1. Aggregate with Value Objects

```csharp
private void ConfigureRestaurantAggregate(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Restaurant>(entity =>
    {
        entity.ToTable("Restaurants");
        
        // ===== Value Object: Email =====
        entity.OwnsOne(r => r.ContactEmail, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(150)
                .IsRequired();
        });
        
        // ===== Value Object: PhoneNumber =====
        entity.OwnsOne(r => r.ContactPhone, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(50)
                .IsRequired();
        });
        
        // ===== Value Object: Address (Complex - Multiple Columns) =====
        entity.OwnsOne(r => r.Location, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(50);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100);
        });
        
        // ===== Child Entity: BusinessHours (Encapsulated Collection) =====
        entity.HasMany<BusinessHours>("_operatingHours")
            .WithOne()
            .HasForeignKey("RestaurantId")
            .OnDelete(DeleteBehavior.Cascade);
        
        // ===== Navigation Properties (Hybrid Pattern) =====
        entity.HasOne(r => r.Owner)
            .WithMany(a => a.OwnedRestaurants)
            .HasForeignKey(r => r.OwnerId);
            
        // ===== Indexes =====
        entity.HasIndex(r => r.OwnerId);
        entity.HasIndex(r => r.Name);
    });
}
```

### 2. Aggregate with Child Entities

```csharp
private void ConfigureOrderAggregate(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>(entity =>
    {
        entity.ToTable("Orders");
        
        // ===== Child Entities (Encapsulated Collection) =====
        // Option 1: Using backing field name
        entity.HasMany<OrderItem>("_orderItems")
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ===== Computed Properties (Stored) =====
        entity.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        // ===== Foreign Keys to Other Aggregates (ID Only) =====
        entity.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId);
            
        entity.HasOne<OrderStatus>()
            .WithMany()
            .HasForeignKey(o => o.OrderStatusId);
    });
    
    // Configure child entity
    modelBuilder.Entity<OrderItem>(entity =>
    {
        entity.ToTable("OrderItems");
        
        entity.Property(oi => oi.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
            
        entity.Property(oi => oi.Subtotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    });
}
```

### 3. Aggregate with Join Entity (Many-to-Many)

```csharp
private void ConfigureMenuAggregate(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Menu>(entity =>
    {
        entity.ToTable("Menus");
        
        // ===== Many-to-Many with Join Entity =====
        entity.HasMany(m => m.Dishes)
            .WithMany(d => d.Menus)
            .UsingEntity<MenuDish>(
                j => j.HasOne(md => md.Dish)
                      .WithMany(d => d.MenuDishes)
                      .HasForeignKey(md => md.DishId),
                j => j.HasOne(md => md.Menu)
                      .WithMany(m => m.MenuDishes)
                      .HasForeignKey(md => md.MenuId),
                j =>
                {
                    j.ToTable("MenuDishes");
                    j.HasKey(md => new { md.MenuId, md.DishId });
                    
                    // Additional properties on join entity
                    j.Property(md => md.DisplayOrder).HasDefaultValue(0);
                    j.Property(md => md.IsActive).HasDefaultValue(true);
                    j.Property(md => md.SpecialPrice).HasColumnType("decimal(18,2)");
                }
            );
    });
}
```

### 4. Lookup Aggregate (Simple)

```csharp
private void ConfigureLookupAggregates(ModelBuilder modelBuilder)
{
    // MenuType
    modelBuilder.Entity<MenuType>(entity =>
    {
        entity.ToTable("MenuTypes");
        entity.HasIndex(mt => new { mt.RestaurantId, mt.Name }).IsUnique();
    });
    
    // Category
    modelBuilder.Entity<Category>(entity =>
    {
        entity.ToTable("Categories");
        entity.HasIndex(c => new { c.RestaurantId, c.Name }).IsUnique();
    });
    
    // OrderStatus
    modelBuilder.Entity<OrderStatus>(entity =>
    {
        entity.ToTable("OrderStatuses");
        entity.HasIndex(os => new { os.RestaurantId, os.Name }).IsUnique();
    });
}
```

### Key Configuration Principles

| Aspect | Configuration | Example |
|--------|--------------|---------|
| **Value Objects** | `OwnsOne()` | Email, PhoneNumber, Address |
| **Child Entities** | `HasMany()` with backing field | BusinessHours, OrderItems |
| **Join Entities** | `UsingEntity<T>()` | MenuDish |
| **ID References** | `HasOne().WithMany()` without navigation | Customer, OrderStatus |
| **Encapsulated Collections** | Use `"_privateField"` name | `"_operatingHours"`, `"_orderItems"` |
| **Indexes** | `HasIndex()` | Foreign keys, unique constraints |
| **Decimal Precision** | `HasColumnType("decimal(18,2)")` | Money amounts |
| **Cascade Delete** | `OnDelete(DeleteBehavior.Cascade)` | Child entities |

### Common Patterns

**Pattern 1: Value Object as Single Column**
```csharp
entity.OwnsOne(r => r.ContactEmail, email =>
{
    email.Property(e => e.Value).HasColumnName("Email");
});
```

**Pattern 2: Value Object as Multiple Columns**
```csharp
entity.OwnsOne(r => r.Location, address =>
{
    address.Property(a => a.Street).HasColumnName("Street");
    address.Property(a => a.City).HasColumnName("City");
    // ... more columns
});
```

**Pattern 3: Encapsulated Collection**
```csharp
entity.HasMany<BusinessHours>("_operatingHours")  // Private field name
    .WithOne()
    .HasForeignKey("RestaurantId");
```

**Pattern 4: Reference by ID Only**
```csharp
entity.HasOne<Customer>()      // No navigation property in aggregate
    .WithMany()
    .HasForeignKey(o => o.CustomerId);
```

### Complete Example

See `Restaurant.cs` for complete EF Core configuration documentation including:
- Full Value Object mapping examples
- Child entity relationships
- Navigation property configuration
- Index definitions

### Migration Generation

After configuring aggregates:

```bash
# Add migration
dotnet ef migrations add AddRestaurantAggregate

# Update database
dotnet ef database update
```

### Verification

Check that EF Core correctly understands your configuration:

```csharp
// In a test or startup
using var context = new AppDbContext(options);
var model = context.Model;

// Verify entity types
var restaurantType = model.FindEntityType(typeof(Restaurant));
var properties = restaurantType.GetProperties();

// Verify owned types
var emailOwned = restaurantType.FindNavigation(nameof(Restaurant.ContactEmail));
```

---

## Testing

```csharp
[TestMethod]
public void AddItem_WhenPending_ShouldSucceed()
{
    var order = new Order(...);
    order.AddItem(1, "Burger", price, 2);
    Assert.AreEqual(1, order.Items.Count);
}

[TestMethod]
[ExpectedException(typeof(InvalidOperationException))]
public void AddItem_WhenConfirmed_ShouldThrow()
{
    var order = new Order(...);
    order.Confirm();
    order.AddItem(1, "Burger", price, 1);  // Should throw!
}
```

---

## Key Takeaways

1. **Package deal** - Save and load together
2. **One boss** - All changes through the root
3. **Keep small** - Only what MUST be consistent
4. **ID references** - Between aggregates, use IDs not objects
5. **Private setters** - Force changes through methods
6. **One save** - Saves root + all children

---

## Project Structure

```
SmartMenuOptim.Domain/
├── Aggregates/
│   ├── RestaurantAggregate/
│   │   ├── Restaurant.cs        (Root)
│   │   └── BusinessHours.cs     (Entity)
│   ├── MenuAggregate/
│   │   ├── Menu.cs              (Root)
│   │   └── MenuDish.cs          (Join Entity)
│   ├── DishAggregate/
│   │   └── Dish.cs              (Root)
│   └── OrderAggregate/
│       ├── Order.cs             (Root)
│       └── OrderItem.cs         (Entity)
└── Entities/TenantSpecificEntities/
    ├── MenuType.cs              (Lookup Aggregate Root)
    ├── Category.cs              (Lookup Aggregate Root)
    └── OrderStatus.cs           (Lookup Aggregate Root)
```

**Aggregate Types:**
- **Main Aggregates** (in `/Aggregates/`) - Complex business logic with child entities
- **Lookup Aggregates** (in `/Entities/TenantSpecificEntities/`) - Reference data, simpler structure

---

## Further Reading

- [Domain-Driven Design](https://www.domainlanguage.com/ddd/) by Eric Evans
- [Effective Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/) by Vaughn Vernon
- [Microsoft DDD Guide](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)

---

**Remember:** If unsure whether something belongs in an aggregate, keep it separate. It's easier to merge later than to split!
