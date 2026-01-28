# Tenant-Specific Entities

This document describes the tenant-specific entities that form the core of the SmartMenuOptim multi-tenant domain model. These entities ensure proper data isolation between different restaurants (tenants) and implement Domain-Driven Design (DDD) principles.

## Table of Contents
1. [Entity Inheritance](#entity-inheritance)
2. [DDD Architecture Overview](#ddd-architecture-overview)
3. [Entity Categories](#entity-categories)
4. [Entity Details](#entity-details)
5. [Multi-Tenant Patterns](#multi-tenant-patterns)
6. [Best Practices](#best-practices)

---

## Entity Inheritance

All tenant-specific entities inherit from `TenantEntityBase`:

```csharp
public abstract class TenantEntityBase : EntityBase
{
    [Required]
    public int RestaurantId { get; set; }
    
    [ForeignKey(nameof(RestaurantId))]
    public virtual Restaurant? Restaurant { get; set; }
}
```

**Key Features:**
- **Restaurant Ownership**: Required RestaurantId FK for tenant scoping
- **Tenant Isolation**: Automatic data isolation per restaurant
- **Audit Trail**: Inherited from EntityBase (CreatedAt, UpdatedAt)
- **Soft Delete**: IsDeleted flag for logical deletion
- **Optimistic Concurrency**: PostgreSQL xmin-based concurrency control
- **PostgreSQL MVCC Integration**: Leverages xmin/xmax for transaction tracking

---

## DDD Architecture Overview

The domain implements a **3-Tier DDD Strategy**:


### Tier 1: Full Aggregate Roots
Complex aggregates with child entities and rich domain behavior:
- **Restaurant** - Tenant root with BusinessHours children
- **Menu** - Contains MenuDish join entities
- **Order** - Contains OrderItem children
- **CustomerLoyalty** - Contains LoyaltyTransaction children
- **Table** - Contains Reservation children
- **Promotion** - Standalone aggregate with rich behavior
- **Dish** - Dish aggregate with menu relationships

### Tier 2: Simple Aggregates (Lightweight DDD)
Entities with encapsulation and behavioral methods but no child entities:
- **Category** - Lookup/reference data for dish classification
- **MenuType** - Lookup/reference data for menu scheduling
- **OrderStatus** - Lookup/reference data for order workflow
- **Review** - Customer feedback with validation
- **SaleRecord** - Sales transaction tracking
- **Promotion** - Standalone aggregate with business rules
- **Reservation** - Time-based table booking (questionable - may be Tier 1)


### Tier 3: Simple Entities
POCO-style entities with public setters and data-focused design:
- **StaffSchedule** - Staff scheduling data
- **StaffMember** - Staff profile data (ProfileEntities folder)

Example Entities by Tier:

┌─────────────────────────────────────────────────────────┐
│ Tier 1: Full Aggregate Roots (Rich DDD)                │
│ ├─ CustomerLoyalty (root)                              │
│ │  └─ LoyaltyTransaction (child) ← NOT a separate tier │
│ ├─ Restaurant (root)                                    │
│ │  └─ BusinessHours (child) ← NOT a separate tier      │
│ ├─ Order (root)                                         │
│ │  └─ OrderItem (child) ← NOT a separate tier          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Tier 2: Simple Aggregates (Lightweight DDD)            │
│ ├─ SaleRecord (standalone entity, no children)         │
│ ├─ Review (standalone entity, no children)             │
│ └─ Promotion (standalone entity, no children)          │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Tier 3: Anemic Entities (Pragmatic Entities)           │
│ ├─ OrderStatus (simple data carrier)                   │
│ └─ MenuType (simple configuration data)                │
└─────────────────────────────────────────────────────────┘

---

## Entity Categories

### 1. Aggregate Roots (located in Aggregates folder) ( Aggregate Roots Entities are those that encapsulate related entities and enforce business rules). They are the main entry points for interacting with the domain model.

According to 3-Tier DDD Strategy, the following entities are classified as Tier 1 - Full Aggregate Roots:

- `Restaurant` (RestaurantAggregate)
- `Dish` (DishAggregate)
- `Menu` (MenuAggregate)
- `Order` (OrderAggregate)
- `Table` (TableAggregate)
- `Reservation` (TableAggregate - child entity)
- `CustomerLoyalty` (CustomerLoyaltyAggregate)
- `Promotion` (PromotionAggregate)

### 2. Lookup/Reference Entities (located in RestaurantEntities folder). Lookup/Reference Entities are those that provide reference data and encapsulate validation/business rules without child entities.

According to 3-Tier DDD Strategy, the following entities are classified as Tier 2 - Lightweight DDD:

- `Category`
- `MenuType`
- `OrderStatus`

### 3. Transaction/Event Entities (located in RestaurantEntities folder). Transaction/Event Entities capture specific events or transactions related to business operations.

According to 3-Tier DDD Strategy, the following entities are classified as Tier 2 - Lightweight DDD:

- `Review`
- `SaleRecord`
- `StaffSchedule`

### 4. Child Join Entities (located in Aggregates folder)

**Join Entities** facilitate many-to-many relationships between aggregates **with additional properties** beyond the relationship itself.

#### 🔗 Join Entity Purpose

```
┌──────────────────────────────────────────────────────────────┐
│ Pure Join Table       vs.    Join Entity (Child)            │
├──────────────────────────────────────────────────────────────┤
│ MenuId + DishId              MenuId + DishId                │
│ (FK only)                    + DisplayOrder                 │
│                              + SpecialPrice                 │
│                              + Notes                        │
│                              + IsAvailable                  │
└──────────────────────────────────────────────────────────────┘

Rule: If you need EXTRA DATA beyond the relationship → Join Entity
      If you only need the relationship → Pure join table (EF Core automatic)
```

#### Registered Join Entities

| Entity | Parent Aggregate | Relates To | Additional Properties |
|--------|------------------|------------|----------------------|
| **MenuDish** | Menu | Dish | DisplayOrder, SpecialPrice, Notes, IsAvailable |
| **OrderItem** | Order | Dish | Quantity, UnitPrice, Subtotal, SpecialInstructions |

**Note:** `OrderItem` is primarily a **child entity** (line item) that references Dish, not a pure join entity.

#### Key Characteristics

```
┌─────────────────────────────────────────────────────────────┐
│ ✅ Composite primary key (MenuId + DishId)                 │
│ ✅ Managed by parent aggregate (Menu owns MenuDish)        │
│ ✅ References another aggregate (Dish)                     │
│ ✅ Stores relationship metadata (order, pricing)           │
│ ✅ No tier classification (part of parent)                 │
│ ✅ No dedicated repository                                 │
└─────────────────────────────────────────────────────────────┘
```

#### Creation Pattern

```csharp
// ❌ WRONG - Direct instantiation
var menuDish = new MenuDish { MenuId = 1, DishId = 5, DisplayOrder = 3 };
dbContext.MenuDishes.Add(menuDish);

// ✅ CORRECT - Through parent aggregate
menu.AddDish(
    dishId: 5,
    displayOrder: 3,
    specialPrice: 14.99m,
    notes: "Limited time special"
);
await menuRepository.UpdateAsync(menu);
```

#### MenuDish vs OrderItem

```
MenuDish (Pure Join Entity):
├─ Many-to-Many: Menu ↔ Dish
├─ Metadata: Display order, special pricing
├─ Mutability: Can reorder, update prices
└─ Purpose: Menu presentation configuration

OrderItem (Child Entity with Reference):
├─ One-to-Many: Order → OrderItem → Dish (reference)
├─ Line Item: Quantity, prices, subtotal calculation
├─ Mutability: Quantity updates trigger total recalc
└─ Purpose: Order composition and billing
```

#### Blazor Usage

```razor
<!-- MenuDish Display -->
<h3>@menu.Name</h3>
@foreach (var menuDish in menu.MenuDishes.OrderBy(md => md.DisplayOrder))
{
    <div class="menu-item">
        <span>@menuDish.Dish.Name</span>
        @if (menuDish.SpecialPrice.HasValue)
        {
            <s>$@menuDish.Dish.DishPrice</s>
            <strong>$@menuDish.SpecialPrice</strong>
        }
        else
        {
            <span>$@menuDish.Dish.DishPrice</span>
        }
    </div>
}

<!-- Update via parent -->
<button @onclick="ReorderDishes">Reorder Menu</button>

@code {
    private async Task ReorderDishes()
    {
        menu.ReorderDishes(newDishOrder);
        await menuRepository.UpdateAsync(menu);
    }
}
```

#### Common Operations

```csharp
// Adding to relationship
menu.AddDish(dishId, displayOrder, specialPrice, notes);
order.AddItem(dishId, dishName, unitPrice, quantity, instructions);

// Removing from relationship
menu.RemoveDish(dishId);
order.RemoveItem(orderItemId);

// Updating metadata
menu.UpdateDishPrice(dishId, newSpecialPrice);
order.UpdateItemQuantity(orderItemId, newQuantity);
```

#### Why Not Independent?

Join entities are **managed by parent** because:
- ✅ Relationship ownership: Menu decides which dishes to show
- ✅ Consistency: Display order must be unique within menu
- ✅ Validation: Special price can't exceed regular price
- ✅ Transaction boundary: Menu changes are atomic
- ✅ Business rules: Parent enforces constraints


### 5. Child Entities (located in Aggregates folder)

**Child Entities** exist only within the context of their parent aggregate root and are managed entirely by the parent.

#### ⚠️ IMPORTANT: Child Entities Don't Have Tier Classifications

Child entities are **NOT classified** in the 3-tier DDD strategy - they are **internal implementation details** of Tier 1 aggregates.

**Why No Tier?**
- No independent existence (can't exist without parent)
- No repository (accessed only through parent)
- Shared transaction boundary (committed with parent)
- Created only by parent aggregate methods
- Part of parent's implementation detail

#### Child Entity Characteristics

**All child entities share:**
```
┌─────────────────────────────────────────────────────────────┐
│ 🧩 CHILD ENTITY PATTERN                                    │
├─────────────────────────────────────────────────────────────┤
│ ✅ Created by parent aggregate methods only                │
│ ✅ Persisted atomically with parent                        │
│ ✅ No dedicated repository                                 │
│ ✅ Encapsulated in parent's private collection             │
│ ✅ Cannot be instantiated from outside aggregate           │
│ ✅ Foreign key to parent (e.g., OrderId, TableId)          │
└─────────────────────────────────────────────────────────────┘
```

#### Child Entity Spectrum

Child entities range from **value-object-like** to **full entities**:

```
Value-Object-Like ────────────────────► Full Entity
       │                │                    │
LoyaltyTransaction  BusinessHours      OrderItem
 (immutable)       (semi-mutable)      (mutable)
```

**Spectrum Characteristics:**

| Position | Mutability | Identity | Behavior | Example |
|----------|------------|----------|----------|---------|
| **Value-Object-Like** | Immutable | Id for EF Core only | None | LoyaltyTransaction |
| **Middle** | Semi-mutable | Id required | Simple validation | BusinessHours |
| **Full Entity** | Mutable | Id essential | Rich behavior | OrderItem |

#### Registered Child Entities

The following entities are classified as **Child Entities** - managed only via parent aggregate:

- `LoyaltyTransaction` (CustomerLoyaltyAggregate) - Immutable audit trail
- `OrderItem` (OrderAggregate) - Mutable line items
- `Reservation` (TableAggregate) - **⚠️ Questionable** - May be Tier 2
- `BusinessHours` (RestaurantAggregate) - Configuration data
- `MenuDish` (MenuAggregate) - Join entity with metadata

#### Creation Pattern

```csharp
// ❌ WRONG - Direct instantiation
var item = new OrderItem(...);  // Violates aggregate boundary

// ✅ CORRECT - Through parent aggregate
order.AddItem(dishId, dishName, price, quantity);
// Parent creates child internally and manages collection
```

#### Common Mistakes to Avoid

```
❌ DON'T:
• Create child entities directly in application code
• Have a repository for child entities
• Modify child state without going through parent
• Load children separately from parent
• Persist children independently
• Use child navigation properties in domain logic

✅ DO:
• Access children through parent aggregate
• Use parent's behavioral methods to create/modify
• Load parent with children using Include()
• Persist through parent's repository
• Keep child collections private with read-only interface
```

#### Blazor Usage Pattern

```razor
<!-- ✅ CORRECT - Display via parent -->
@foreach (var item in order.OrderItems)
{
    <div>@item.DishName - @item.Quantity</div>
}

<!-- ❌ WRONG - Can't bind to private setters -->
<InputNumber @bind-Value="item.Quantity" />

<!-- ✅ CORRECT - Use parent method -->
<button @onclick="() => UpdateQuantity(itemId, newQty)">Update</button>

@code {
    private async Task UpdateQuantity(int itemId, int quantity)
    {
        order.UpdateItemQuantity(itemId, quantity);
        await orderRepository.UpdateAsync(order);
    }
}
```

#### Child Entity Comparison Table

| Entity | Parent | Mutability | Spectrum | Primary Purpose |
|--------|--------|------------|----------|-----------------|
| **LoyaltyTransaction** | CustomerLoyalty | Immutable | ◄●────────────► | Audit trail |
| **BusinessHours** | Restaurant | Mutable | ◄───●─────────► | Config data |
| **OrderItem** | Order | Mutable | ◄────────●────► | Line items |
| **MenuDish** | Menu | Mutable | ◄──────●──────► | Join + metadata |
| **Reservation** | Table (?) | Semi-mutable | ◄─────●───────► | **Debatable** |

**Legend:**
- `◄●────────────►` = Almost value object (immutable, append-only)
- `◄───●─────────►` = Entity with identity (some mutability)
- `◄────────●────►` = Full entity (complex lifecycle)

#### ⚠️ Special Note: Reservation Entity

`Reservation` is currently classified as a child of `Table` aggregate, but this is **questionable**:

**Red Flags:**
- Inherits from `TenantEntityBase` (child entities typically don't)
- References multiple aggregates (Table, Customer, Restaurant)
- Has complex multi-tenant validation across entities
- Represents coordination between entities, not part of one

**Recommendation:** Consider reclassifying as **Tier 2 - Simple Aggregate** with its own repository.

See Reservation entity documentation for detailed analysis.

---

## Entity Details

### Restaurant Aggregate (`RestaurantAggregate\Restaurant.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: EntityBase (NOT TenantEntityBase - it IS the tenant)

**Description**: The tenant root entity representing a physical restaurant. Special case: inherits directly from EntityBase because it defines the tenant boundary itself.

**Key Characteristics**:
- Contains contact information via Value Objects (Address, Email, PhoneNumber)
- Manages child BusinessHours entities
- Controls order acceptance through business rules
- Timezone-aware operations
- Owned by global AdminUser entity

**Child Entities**:
- `BusinessHours` - Operating hours per day of week

**Referenced By**: All TenantEntityBase entities via RestaurantId FK

---

### Dish Aggregate (`DishAggregate\Dish.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: TenantEntityBase

**Description**: Represents a menu item offered by a restaurant with rich domain behavior and menu relationships.

**Key Properties**:
- Name, Description, Price (DishPrice)
- CategoryId (FK to Category)
- Nutritional info (Calories, IsVegetarian, IsSpicy, etc.)
- Preparation time

**Relationships**:
- Many-to-many with Menu via MenuDish join entity
- One-to-many with Reviews
- One-to-many with SaleRecords
- Belongs to one Category

---

### Category Entity (`RestaurantEntities\Category.cs`)
**Type**: Simple Aggregate (Lookup/Reference Data)  
**DDD Tier**: Tier 2 - Lightweight DDD  
**Inheritance**: TenantEntityBase

**Description**: Organizes dishes into logical groupings (e.g., Appetizers, Main Course, Desserts).

**Key Properties**:
- Name (2-50 chars, alphanumeric)
- Description (optional, min 10 chars)
- DisplayOrder (for UI presentation)

**Business Rules**:
- Unique name per restaurant
- Cannot be deleted if referenced by active dishes
- Alphanumeric name format with spaces and hyphens

**Behavioral Methods**:
- `UpdateBasicInfo(name, description)`
- `UpdateDisplayOrder(order)`
- `ValidateTenantConsistency()`

---

### Menu Aggregate (`MenuAggregate\Menu.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: TenantEntityBase

**Description**: Represents a restaurant's menu with time-based availability and dish associations.

**Key Properties**:
- Name, Description
- AvailableFrom, AvailableTo (TimeSpan - local restaurant time)
- IsAvailable (active status)
- MenuTypeId (FK to MenuType)

**Relationships**:
- Many-to-many with Dish via MenuDish join entity
- Belongs to one MenuType

**Child Entities**:
- `MenuDish` - Join entity with special pricing and display order

**Behavioral Methods**:
- `AddDish(dish, displayOrder, specialPrice, notes)`
- `RemoveDish(dishId)`
- `SetAvailability(from, to)`
- `MakeAvailable()` / `MakeUnavailable()`

---

### MenuType Entity (`RestaurantEntities\MenuType.cs`)
**Type**: Simple Aggregate (Lookup/Reference Data)  
**DDD Tier**: Tier 2 - Lightweight DDD  
**Inheritance**: TenantEntityBase

**Description**: Categorizes menus by service period (e.g., Breakfast, Lunch, Dinner, Brunch).

**Key Properties**:
- Name, Description
- DefaultStartTime, DefaultEndTime (optional TimeSpan templates)
- DisplayOrder

**Business Rules**:
- Default times must be set together or both null
- Start and end times cannot be identical
- Cannot be deleted if referenced by active menus

**Behavioral Methods**:
- `UpdateBasicInfo(name, description)`
- `SetDefaultTimes(startTime, endTime)`
- `ClearDefaultTimes()`
- `UpdateDisplayOrder(order)`

---

### Order Aggregate (`OrderAggregate\Order.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: TenantEntityBase

**Description**: Manages customer orders with lifecycle tracking and business rules.

**Key Properties**:
- CustomerId (FK to global Customer)
- OrderStatusId (FK to OrderStatus)
- TotalAmount (auto-calculated from items)
- OrderDate, SpecialInstructions
- HandledByStaffId (optional FK to StaffMember)

**Relationships**:
- Links to global Customer entity
- References OrderStatus lookup
- Optionally links to StaffMember

**Child Entities**:
- `OrderItem` - Line items with dish, quantity, price

**Behavioral Methods**:
- `AddItem(dishId, dishName, unitPrice, quantity, instructions)`
- `RemoveItem(itemId)`
- `UpdateItemQuantity(itemId, quantity)`
- `SetSpecialInstructions(instructions)`
- `RecalculateTotals()`

**Lifecycle States**:
Pending ? Confirmed ? Preparing ? Ready ? In Delivery ? Completed (or Cancelled)

---

### OrderStatus Entity (`RestaurantEntities\OrderStatus.cs`)
**Type**: Simple Aggregate (Lookup/Reference Data)  
**DDD Tier**: Tier 2 - Lightweight DDD  
**Inheritance**: TenantEntityBase

**Description**: Defines workflow states for order management system.

**Key Properties**:
- Name, Description
- IsTerminal (prevents further transitions)
- ColorCode (hex format #RRGGBB for UI)
- DisplayOrder

**Business Rules**:
- Color must be valid hex format
- Terminal statuses prevent state changes
- Cannot be deleted if referenced by active orders

**Behavioral Methods**:
- `UpdateBasicInfo(name, description)`
- `SetTerminal(isTerminal)`
- `SetColorCode(color)`
- `UpdateDisplayOrder(order)`

**Common Statuses**: Pending, Preparing, Ready, In Delivery, Completed, Cancelled

---

### Review Entity (`RestaurantEntities\Review.cs`)
**Type**: Simple Aggregate  
**DDD Tier**: Tier 2 - Lightweight DDD  
**Inheritance**: TenantEntityBase

**Description**: Captures customer feedback for dishes with ratings and sentiment analysis.

**Key Properties**:
- DishId (FK to Dish)
- CustomerId (optional FK to global Customer)
- CustomerName (for anonymous reviews)
- Rating (1-5 stars)
- Comment (min 10 chars)
- SentimentScore (0.0-1.0, optional)

**Business Rules**:
- Rating must be 1-5
- Comment minimum 10 characters
- Review date cannot be in future
- Either CustomerId or CustomerName required

**Behavioral Methods**:
- `UpdateReview(rating, comment)`
- `UpdateSentiment(score)`
- `UpdateCustomerInfo(customerId, customerName)`
- `IsPositive()` - Helper to check rating ? 4

---

### SaleRecord Entity (`RestaurantEntities\SaleRecord.cs`)
**Type**: Simple Aggregate  
**DDD Tier**: Tier 2 - Lightweight DDD  
**Inheritance**: TenantEntityBase

**Description**: Tracks sales transactions for dishes to support analytics and revenue tracking.

**Key Properties**:
- DishId (FK to Dish)
- QuantitySold (positive integer)
- SaleAmount (Money value object)
- SaleDate (UTC, auto-set to DateTime.UtcNow)

**Business Rules**:
- Quantity must be positive
- Sale amount cannot be negative
- Sale date cannot be in future (1-minute grace period)
- Cannot be older than 5 years

**Behavioral Methods**:
- `UpdateSaleAmount(amount)` - For corrections
- `UpdateQuantity(quantity)` - For adjustments
- `ValidateTenantConsistency()`

**Use Cases**: Sales analytics, revenue tracking, dish performance analysis

---

### StaffSchedule Entity (`RestaurantEntities\StaffSchedule.cs`)
**Type**: Simple Entity  
**DDD Tier**: Tier 3 - Anemic/Data-Focused  
**Inheritance**: TenantEntityBase

**Description**: Manages staff work schedules with shift tracking and status workflow.

**Key Properties**:
- StaffMemberId (FK to StaffMember)
- ShiftStart, ShiftEnd (UTC DateTime)
- Status (ScheduleStatus enum)
- IsRecurring, DayOfWeek (for recurring schedules)
- CreatedByAdminUserId, LastModifiedByAdminUserId

**Status Workflow**:
Pending ? Approved ? Completed (or Cancelled, SickLeave, Vacation, NeedsCoverage)

**Business Rules**:
- Shift end must be after shift start
- Shift duration: 30 minutes to 24 hours
- Cannot schedule more than 6 months in advance
- No overlapping shifts for same staff member
- Recurring schedules must specify day of week

**Validation**: Implements IValidatableObject for complex business rules

---

### Table Aggregate (`TableAggregate\Table.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: TenantEntityBase

**Description**: Manages physical tables with capacity tracking and reservation management.

**Key Properties**:
- TableNumber (unique identifier)
- Capacity (1-100 seats)
- Status (TableStatus enum: Available, Occupied, Reserved, OutOfService)

**Child Entities**:
- `Reservation` - Time-based booking commitments

**Behavioral Methods**:
- `MarkOccupied()` / `MarkAvailable()`
- `Reserve()` / `ClearReservation()`
- `MakeReservation(time, customerId/customerName, partySize)`
- `CancelReservation(reservationId)`
- `IsAvailable()` / `CanAccommodate(partySize)`

---

### Reservation Entity (`TableAggregate\Reservation.cs`)
**Type**: Child Entity  
**DDD Tier**: Tier 1 - Child Entity of Table Aggregate  
**Inheritance**: TenantEntityBase

**Description**: Represents a time-based table booking, managed entirely by Table aggregate.

**Key Properties**:
- TableId (FK to parent Table)
- ReservationTime (future DateTime)
- CustomerId (optional FK to global Customer)
- CustomerName, CustomerPhone (for anonymous reservations)
- PartySize (optional)

**Business Rules**:
- Must be in future (15-minute grace period)
- Cannot be more than 6 months in advance
- Must match parent Table's restaurant
- Requires either CustomerId OR CustomerName+CustomerPhone

**Access Pattern**: Created and managed ONLY through Table aggregate methods

---

### CustomerLoyalty Aggregate (`CustomerLoyaltyAggregate\CustomerLoyalty.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root  
**Inheritance**: TenantEntityBase

**Description**: Manages customer loyalty program membership with points and tier progression.

**Key Properties**:
- CustomerId (FK to global Customer)
- Points (current balance)
- CurrentTier (LoyaltyTier enum)

**Tier Progression**:
- Bronze: 0-99 points
- Silver: 100-499 points
- Gold: 500-999 points
- Platinum: 1000+ points

**Child Entities**:
- `LoyaltyTransaction` - Point earning/redemption history

**Behavioral Methods**:
- `AddPoints(amount, description)` - Earn points, auto-creates transaction
- `RedeemPoints(amount, description)` - Redeem points, validates balance
- Auto-updates tier based on current points

**Business Rules**:
- Points cannot go negative
- Unique per customer/restaurant combination
- Tier auto-calculated on point changes

---

### LoyaltyTransaction Entity (`CustomerLoyaltyAggregate\LoyaltyTransaction.cs`)
**Type**: Child Entity  
**DDD Tier**: Tier 1 - Child Entity  
**Inheritance**: TenantEntityBase

**Description**: Immutable audit trail of loyalty point changes.

**Key Properties**:
- CustomerLoyaltyId (FK to parent)
- PointsChange (can be positive or negative)
- BalanceAfter (snapshot)
- Type (TransactionType enum: OrderEarning, RewardRedemption, Bonus)
- Description
- OrderId (optional FK to Order)

**Business Rules**:
- PointsChange cannot be zero
- BalanceAfter must be non-negative
- Immutable after creation (append-only)
- Created ONLY through CustomerLoyalty aggregate

---

### Promotion Aggregate (`PromotionAggregate\Promotion.cs`)
**Type**: Aggregate Root  
**DDD Tier**: Tier 1 - Full Aggregate Root (no children)  
**Inheritance**: TenantEntityBase

**Description**: Manages promotional offers with fixed discount amounts and date ranges.

**Key Properties**:
- Name, Description
- DiscountAmount (decimal 0 to 1,000,000)
- ValidFrom, ValidTo (DateTime range)
- IsActive (internal field, managed via methods)

**Behavioral Methods**:
- `Activate()` - Validates start date before activation
- `Deactivate()`
- `UpdateDetails(name, discountAmount, validFrom, validTo, description)`
- `UpdateNotes(notes)`
- `IsActive()` / `IsValidAt(date)` / `CanBeActivated()`

**Business Rules**:
- ValidTo must be after ValidFrom
- Can only activate if current date ? ValidFrom
- Cannot update details while active
- Maximum 1-year extension from current date

---

### StaffMember Entity (`ProfileEntities\StaffMember.cs`)
**Type**: Profile Entity (Links to Identity)  
**DDD Tier**: Tier 3 - Data-Focused  
**Inheritance**: TenantEntityBase  
**Special Note**: Located in ProfileEntities folder but scoped to tenant

**Description**: Represents a staff member employed at a specific restaurant with identity integration.

**Key Properties**:
- ApplicationUserId (string FK to ASP.NET Identity)
- Email, Username (authentication fields)
- PhoneNumber, PhoneNumberConfirmed
- Role (StaffRole enum: Manager, Waiter, Chef, etc.)
- EmploymentStatus (FullTime, PartTime, Terminated, OnLeave)
- HireDate, TerminationDate
- Salary, IsActive

**Relationships**:
- One-to-One with ApplicationUser (Identity)
- One-to-many with StaffSchedule
- One-to-many with Orders (via HandledByStaffId)

**Business Rules**:
- Unique email and username
- Phone verification support
- Single restaurant scope (no cross-tenant staff)

**Indexes**:
- IX_StaffMembers_Email_Username_Unique
- IX_StaffMembers_Restaurant_Role_Status
- IX_StaffMembers_Phone_Verified

---

## Multi-Tenant Patterns

### Data Isolation Strategies

#### 1. Entity Level Isolation
```csharp
// Required RestaurantId on all tenant entities
[Required(ErrorMessage = "RestaurantId is required for tenant-scoped entities")]
[ForeignKey(nameof(Restaurant))]
public int RestaurantId { get; set; }

// Foreign key constraint enforces referential integrity
public virtual Restaurant? Restaurant { get; set; }
```

#### 2. Query Level Isolation
Always filter queries by RestaurantId:
```csharp
// ? CORRECT - Tenant-scoped query
var dishes = await dbContext.Dishes
    .Where(d => d.RestaurantId == currentRestaurantId)
    .ToListAsync();

// ? WRONG - Cross-tenant data leak
var allDishes = await dbContext.Dishes.ToListAsync();
```

#### 3. Unique Constraints with Tenant Scope
```csharp
// Unique within tenant boundary
CREATE UNIQUE INDEX IX_Categories_RestaurantId_Name 
ON Categories (RestaurantId, Name) 
WHERE IsDeleted = false;
```

#### 4. Validation Patterns
```csharp
// Validate tenant consistency across relationships
public void ValidateTenantConsistency()
{
    if (Dish?.RestaurantId != RestaurantId)
    {
        throw new InvalidOperationException(
            "Dish must belong to the same restaurant as the review.");
    }
}
```

### Identity Integration Patterns

#### Global Entities (No RestaurantId)
- `Customer` - Can interact with multiple restaurants
- `AdminUser` - Can own multiple restaurants
- `ApplicationUser` - ASP.NET Identity base

#### Tenant-Scoped Entities
- `StaffMember` - Scoped to single restaurant via RestaurantId
- All business entities inherit TenantEntityBase

#### Cross-Tenant Relationships
```csharp
// Global Customer can have orders in multiple restaurants
public class Order : TenantEntityBase
{
    public int CustomerId { get; set; }  // Global customer
    public Customer? Customer { get; set; }
}

// Validate: Order.RestaurantId defines the tenant context
```

---

## Best Practices

### 1. Entity Design Principles

#### Aggregate Root Selection
- **Use Full Aggregate (Tier 1)** when:
  - Entity has child entities
  - Complex business rules across multiple entities
  - Transactional consistency required
  - Examples: Order, Menu, Table, Restaurant

- **Use Simple Aggregate (Tier 2)** when:
  - No child entities
  - Rich behavior and validation needed
  - Encapsulation important
  - Examples: Category, MenuType, Review

- **Use Simple Entity (Tier 3)** when:
  - Data transfer focused
  - Minimal business logic
  - Integration with external systems
  - Examples: StaffSchedule

#### Encapsulation Guidelines
```csharp
// ? CORRECT - Private setters, behavioral methods
public class Category : TenantEntityBase
{
    public string Name { get; private set; }
    
    public void UpdateBasicInfo(string name, string? description)
    {
        // Validation logic
        Name = name;
    }
}

// ? WRONG - Public setters bypass validation
public string Name { get; set; }
```

### 2. Validation Strategies

#### Constructor Validation (Tier 1 & 2)
```csharp
public Category(int restaurantId, string name, string? description)
{
    ArgumentNullException.ThrowIfNull(name);
    if (name.Length < 2 || name.Length > 50)
        throw new ArgumentException("Name must be 2-50 characters");
    
    RestaurantId = restaurantId;
    Name = name;
    Description = description;
}
```

#### IValidatableObject (Tier 2 & 3)
```csharp
public IEnumerable<ValidationResult> Validate(ValidationContext context)
{
    if (ShiftEnd <= ShiftStart)
    {
        yield return new ValidationResult(
            "Shift end must be after shift start",
            new[] { nameof(ShiftEnd) });
    }
}
```

### 3. Repository Patterns

#### Tenant-Aware Repositories
```csharp
public interface ITenantRepository<T> where T : TenantEntityBase
{
    Task<T?> GetByIdAsync(int id, int restaurantId);
    Task<IEnumerable<T>> GetAllAsync(int restaurantId);
    Task<bool> ExistsAsync(int id, int restaurantId);
}
```

#### Aggregate-Specific Repositories
```csharp
public interface IOrderRepository : ITenantRepository<Order>
{
    Task<IEnumerable<Order>> GetByCustomerAsync(int customerId, int restaurantId);
    Task<IEnumerable<Order>> GetByStatusAsync(int statusId, int restaurantId);
}
```

### 4. Index Strategy

#### Multi-Tenant Index Patterns
```csharp
// Always include RestaurantId as first column
CREATE INDEX IX_Dishes_RestaurantId_CategoryId 
ON Dishes (RestaurantId, CategoryId)
WHERE IsDeleted = false;

// Unique constraints scoped to tenant
CREATE UNIQUE INDEX IX_MenuTypes_RestaurantId_Name 
ON MenuTypes (RestaurantId, Name)
WHERE IsDeleted = false;
```

### 5. Query Performance

#### Efficient Tenant Queries
```csharp
// ? GOOD - Single query with includes
var menu = await dbContext.Menus
    .Where(m => m.Id == menuId && m.RestaurantId == restaurantId)
    .Include(m => m.MenuDishes)
        .ThenInclude(md => md.Dish)
    .Include(m => m.MenuType)
    .FirstOrDefaultAsync();

// ? BAD - Multiple queries, N+1 problem
var menu = await dbContext.Menus.FindAsync(menuId);
foreach (var menuDish in menu.MenuDishes)
{
    var dish = await dbContext.Dishes.FindAsync(menuDish.DishId);
}
```

### 6. Child Entity Management

#### Access Only Through Aggregate Root
```csharp
// ? CORRECT - Through aggregate
var table = await tableRepository.GetByIdAsync(tableId, restaurantId);
var reservation = table.MakeReservation(time, customerId, partySize);
await tableRepository.SaveAsync(table);

// ? WRONG - Direct child entity creation
var reservation = new Reservation(...);  // Won't compile - internal constructor
```

### 7. Transaction Boundaries

#### Aggregate Transaction Scope
```csharp
// ? CORRECT - Single aggregate transaction
var order = await orderRepository.GetByIdAsync(orderId, restaurantId);
order.AddItem(dishId, dishName, price, quantity);
order.RecalculateTotals();
await orderRepository.SaveAsync(order);

// ? WRONG - Cross-aggregate transaction
var order = await orderRepository.GetByIdAsync(orderId, restaurantId);
var dish = await dishRepository.GetByIdAsync(dishId, restaurantId);
order.AddItem(dish.Id, dish.Name, dish.DishPrice, quantity);
dish.UpdateSomeProperty(); // Modifying multiple aggregates
await orderRepository.SaveAsync(order);
await dishRepository.SaveAsync(dish);
```

### 8. Soft Delete Handling

#### Query Patterns
```csharp
// Include soft delete filter
var activeCategories = await dbContext.Categories
    .Where(c => c.RestaurantId == restaurantId && !c.IsDeleted)
    .ToListAsync();

// Or use global query filters in DbContext
modelBuilder.Entity<Category>()
    .HasQueryFilter(c => !c.IsDeleted);
```

---

## Key Conventions & Requirements

### 1. Entity Structure
- ? Inherit from TenantEntityBase (except Restaurant)
- ? Include RestaurantId foreign key validation
- ? Use appropriate DDD tier pattern
- ? Implement proper encapsulation
- ? Add business validation
- ? Use proper navigation properties

### 2. Naming Conventions
- Entity files: PascalCase (e.g., `OrderStatus.cs`)
- Properties: PascalCase (e.g., `RestaurantId`)
- Methods: PascalCase verbs (e.g., `UpdateBasicInfo()`)
- Private fields: _camelCase (e.g., `_menuDishes`)

### 3. Folder Organization
```
SmartMenuOptim.Domain/
??? Aggregates/
?   ??? RestaurantAggregate/
?   ?   ??? Restaurant.cs (Root)
?   ?   ??? BusinessHours.cs (Child)
?   ??? DishAggregate/
?   ?   ??? Dish.cs (Root)
?   ??? MenuAggregate/
?   ?   ??? Menu.cs (Root)
?   ?   ??? MenuDish.cs (Join/Child)
?   ??? OrderAggregate/
?   ?   ??? Order.cs (Root)
?   ?   ??? OrderItem.cs (Child)
?   ??? TableAggregate/
?   ?   ??? Table.cs (Root)
?   ?   ??? Reservation.cs (Child)
?   ??? CustomerLoyaltyAggregate/
?   ?   ??? CustomerLoyalty.cs (Root)
?   ?   ??? LoyaltyTransaction.cs (Child)
?   ??? PromotionAggregate/
?       ??? Promotion.cs (Root)
??? Entities/
?   ??? Base/
?   ?   ??? EntityBase.cs
?   ?   ??? TenantEntityBase.cs
?   ??? RestaurantEntities/
?   ?   ??? Category.cs (Tier 2)
?   ?   ??? MenuType.cs (Tier 2)
?   ?   ??? OrderStatus.cs (Tier 2)
?   ?   ??? Review.cs (Tier 2)
?   ?   ??? SaleRecord.cs (Tier 2)
?   ?   ??? StaffSchedule.cs (Tier 3)
?   ??? ProfileEntities/
?   ?   ??? StaffMember.cs (Tenant-scoped)
?   ?   ??? Customer.cs (Global)
?   ?   ??? AdminUser.cs (Global)
?   ??? GlobalEntities/
?       ??? ApplicationUser.cs
?       ??? BusinessRule.cs
?       ??? UserPermission.cs
??? ValueObjects/
    ??? Address.cs
    ??? Email.cs
    ??? PhoneNumber.cs
    ??? Money.cs
    ??? Percentage.cs
```

---

## Future Considerations

### Planned Enhancements
- [ ] Enhanced tenant operations (bulk operations, migrations)
- [ ] Improved reporting capabilities with pre-aggregated views
- [ ] Advanced partitioning strategies for large datasets
- [ ] Cross-tenant features (marketplace, shared menus)
- [ ] Tenant-specific customization framework
- [ ] Multi-region support with data residency
- [ ] Real-time analytics and event sourcing

### Migration Path
When moving from simple entities to aggregates:
1. Identify consistency boundaries
2. Add behavioral methods
3. Encapsulate collections (if children exist)
4. Add validation logic
5. Update repository interfaces
6. Refactor application services

---

## Related Documentation
- [Domain Events](EVENTS_CLEAN.md) - Event-driven architecture patterns
- [Value Objects](../ValueObjects/) - Immutable domain value objects
- [Base Entities](../Entities/Base/) - Entity inheritance hierarchy
- Entity Framework configurations in Infrastructure layer