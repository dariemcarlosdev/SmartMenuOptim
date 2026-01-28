# Domain Events - Clean Architecture Guide

## Overview
This folder contains **Domain Events** that represent significant state changes or business occurrences within the SmartMenuOptim domain. Domain events are a cornerstone of event-driven architecture and Domain-Driven Design (DDD).

---

## What Are Domain Events?

**Domain Events** are immutable objects that capture something meaningful that happened in the domain. They:
- Represent facts about the past (e.g., "OrderPlaced", "DishAddedToMenu", "ReviewSubmitted")
- Are named in past tense to reflect completed actions
- Contain only the data necessary to describe what happened
- Enable loose coupling between different parts of the system
- Support eventual consistency and asynchronous processing

---

## What Should Be Included in This Folder

### ✅ **Domain Events**
Events that represent significant business occurrences:

#### **Restaurant Events**
- `RestaurantCreatedEvent` - When a new restaurant is registered
- `RestaurantActivatedEvent` - When a restaurant becomes active
- `RestaurantDeactivatedEvent` - When a restaurant is deactivated
- `RestaurantInformationUpdatedEvent` - When restaurant details change

#### **Menu Events**
- `MenuCreatedEvent` - When a new menu is created
- `MenuActivatedEvent` - When a menu becomes active
- `MenuDeactivatedEvent` - When a menu is deactivated
- `DishAddedToMenuEvent` - When a dish is added to a menu
- `DishRemovedFromMenuEvent` - When a dish is removed from a menu
- `MenuPriceChangedEvent` - When menu pricing is updated

#### **Dish Events**
- `DishCreatedEvent` - When a new dish is created
- `DishNameChangedEvent` - When a dish name is updated
- `DishPriceChangedEvent` - When a dish price changes
- `DishActivatedEvent` - When a dish becomes available
- `DishDeactivatedEvent` - When a dish is made unavailable
- `DishDeletedEvent` - When a dish is soft-deleted

#### **Sales Events**
- `SaleRecordedEvent` - When a sale transaction occurs
- `SaleAmountCorrectedEvent` - When a sale amount is adjusted
- `SaleQuantityCorrectedEvent` - When sale quantity is corrected

#### **Review Events**
- `ReviewSubmittedEvent` - When a customer submits a review
- `ReviewUpdatedEvent` - When a review is edited
- `ReviewDeletedEvent` - When a review is removed
- `ReviewSentimentAnalyzedEvent` - When sentiment analysis completes

#### **Customer Events**
- `CustomerRegisteredEvent` - When a new customer signs up
- `CustomerProfileUpdatedEvent` - When customer details change
- `CustomerDeactivatedEvent` - When a customer account is deactivated

#### **Promotion Events**
- `PromotionCreatedEvent` - When a new promotion is created
- `PromotionActivatedEvent` - When a promotion goes live
- `PromotionExpiredEvent` - When a promotion ends
- `PromotionAppliedEvent` - When a promotion is used

#### **Reservation Events** (if applicable)
- `ReservationCreatedEvent` - When a table is reserved
- `ReservationConfirmedEvent` - When a reservation is confirmed
- `ReservationCancelledEvent` - When a reservation is cancelled

---

## What Should NOT Be Included

❌ **Infrastructure Events** - System-level events (database connections, network failures)  
❌ **Integration Events** - Events for external system communication (use Application layer)  
❌ **UI Events** - User interface interactions (button clicks, form submissions)  
❌ **Application Events** - Application workflow events (use Application layer)  
❌ **Event Handlers** - Event processing logic (belongs in Application layer)  
❌ **Event Publishing Infrastructure** - Message broker code (belongs in Infrastructure layer)

---

## Event Structure Guidelines

### Basic Event Template
```csharp
namespace SmartMenuOptim.Domain.Events
{
    /// <summary>
    /// Raised when [business action occurs].
    /// </summary>
    public class [EntityName][ActionPastTense]Event : DomainEvent
    {
        // === Event Properties ===
        
        /// <summary>
        /// Unique identifier of the affected entity.
        /// </summary>
        public int EntityId { get; }
        
        /// <summary>
        /// Tenant/Restaurant identifier for multi-tenant isolation.
        /// </summary>
        public int RestaurantId { get; }
        
        /// <summary>
        /// Additional relevant data about what happened.
        /// </summary>
        public string RelevantData { get; }
        
        /// <summary>
        /// When the event occurred (UTC).
        /// </summary>
        public DateTime OccurredOn { get; }
        
        // === Constructor ===
        
        public [EntityName][ActionPastTense]Event(
            int entityId,
            int restaurantId,
            string relevantData)
        {
            EntityId = entityId;
            RestaurantId = restaurantId;
            RelevantData = relevantData;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
```

### Example: DishCreatedEvent
```csharp
namespace SmartMenuOptim.Domain.Events
{
    /// <summary>
    /// Raised when a new dish is created in a restaurant.
    /// </summary>
    public class DishCreatedEvent : DomainEvent
    {
        public int DishId { get; }
        public int RestaurantId { get; }
        public string DishName { get; }
        public decimal DishPrice { get; }
        public int CategoryId { get; }
        public DateTime OccurredOn { get; }
        
        public DishCreatedEvent(
            int dishId,
            int restaurantId,
            string dishName,
            decimal dishPrice,
            int categoryId)
        {
            DishId = dishId;
            RestaurantId = restaurantId;
            DishName = dishName;
            DishPrice = dishPrice;
            CategoryId = categoryId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
```

---

## Domain Event Best Practices

### 1. **Naming Conventions**
- Use past tense: `DishCreated`, not `DishCreate` or `CreateDish`
- Be specific: `DishPriceChanged` rather than generic `DishUpdated`
- Include entity name: `ReviewSubmittedEvent` not just `SubmittedEvent`

### 2. **Immutability**
- All properties should be `{ get; }` (read-only)
- Set values only in constructor
- Events represent immutable facts about the past

### 3. **Data Inclusion**
- Include only data relevant to what happened
- Include enough context for handlers to react appropriately
- Avoid including entire entity graphs (use IDs)
- Consider including both old and new values for change events

### 4. **Multi-Tenant Awareness**
- Always include `RestaurantId` for tenant isolation
- Ensure event handlers respect tenant boundaries

### 5. **Time Tracking**
- Include `OccurredOn` timestamp (UTC) for event ordering
- Consider adding `EventId` (GUID) for idempotency

---

## Event Usage Patterns

### Raising Events in Entities
```csharp
public class Dish : TenantEntityBase
{
    private readonly List<DomainEvent> _domainEvents = new();
    
    public IReadOnlyCollection<DomainEvent> DomainEvents => 
        _domainEvents.AsReadOnly();
    
    public void ChangePrice(Money newPrice)
    {
        var oldPrice = Price;
        Price = newPrice;
        
        // Raise domain event
        _domainEvents.Add(new DishPriceChangedEvent(
            Id,
            RestaurantId,
            oldPrice,
            newPrice,
            DateTime.UtcNow
        ));
    }
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### Publishing Events (Application Layer)
```csharp
// In Application Layer Service
public async Task UpdateDishPriceAsync(int dishId, Money newPrice)
{
    var dish = await _dishRepository.GetByIdAsync(dishId);
    dish.ChangePrice(newPrice);
    
    await _dishRepository.SaveAsync(dish);
    
    // Publish events (handled by infrastructure)
    foreach (var domainEvent in dish.DomainEvents)
    {
        await _eventPublisher.PublishAsync(domainEvent);
    }
    
    dish.ClearDomainEvents();
}
```

---

## Benefits of Domain Events

### 1. **Decoupling**
- Entities don't need to know about side effects
- Handlers can be added/removed without modifying domain logic
- Supports Open/Closed Principle

### 2. **Audit Trail**
- Events provide natural audit log
- Track what changed, when, and why
- Support compliance and debugging

### 3. **Event Sourcing (Future)**
- Events can be stored as source of truth
- Enables time-travel debugging
- Supports replay and reconstruction

### 4. **Integration**
- Events can trigger external system notifications
- Support microservices communication
- Enable real-time updates to clients

### 5. **Business Insights**
- Events capture business activities
- Support analytics and reporting
- Enable business process monitoring

---

## Common Use Cases in SmartMenuOptim

### Scenario 1: Dish Price Change
```
1. User updates dish price
2. DishPriceChangedEvent raised
3. Handlers execute:
   - Update search index
   - Notify menu planners
   - Log for audit
   - Update analytics dashboard
```

### Scenario 2: Review Submission
```
1. Customer submits review
2. ReviewSubmittedEvent raised
3. Handlers execute:
   - Trigger sentiment analysis
   - Update dish rating calculation
   - Notify restaurant manager
   - Add to moderation queue
```

### Scenario 3: Sale Recorded
```
1. POS system records sale
2. SaleRecordedEvent raised
3. Handlers execute:
   - Update inventory forecast
   - Trigger low-stock alerts
   - Update sales analytics
   - Calculate daily revenue
```

---

## Integration with Application Layer

Domain events are **raised** in the Domain layer but **handled** in the Application layer:

**Domain Layer (this folder):**
- Define event classes
- Raise events from entities/aggregates

**Application Layer:**
- Implement event handlers
- Coordinate side effects
- Call infrastructure services

**Infrastructure Layer:**
- Event publishing mechanism
- Message broker integration
- Event persistence

---

## Event vs. Integration Event

| Aspect | Domain Event | Integration Event |
|--------|--------------|-------------------|
| **Scope** | Within bounded context | Across bounded contexts |
| **Location** | Domain layer | Application layer |
| **Purpose** | Domain logic decoupling | External system integration |
| **Example** | `DishCreatedEvent` | `OrderPlacedIntegrationEvent` |

---

## Next Steps

1. Create base `DomainEvent` abstract class
2. Implement events for critical business operations
3. Add event collection to aggregate roots
4. Create event handlers in Application layer
5. Configure event publishing in Infrastructure layer

---

## References

- **Domain-Driven Design** by Eric Evans
- **Implementing Domain-Driven Design** by Vaughn Vernon
- **Microsoft Architecture Guide**: [Domain Events Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

---

*This folder represents the Domain Events of SmartMenuOptim according to Clean Architecture and DDD principles.*
