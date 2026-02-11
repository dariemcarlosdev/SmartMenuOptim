# SmartMenuOptim Application Layer Documentation

> **Clean Architecture - Application Layer Documentation**

This folder contains comprehensive documentation for the Application Layer of SmartMenuOptim, organized by architectural concerns following Clean Architecture principles.

---

## 📁 Documentation Structure

```
SmartMenuOptim.Application/docs/
├── 📄 README.md                          ← You are here
├── 📁 01-ApplicationServices/            ← Application Services (Use Cases)
├── 📁 02-DTOs/                           ← Data Transfer Objects
├── 📁 03-EventHandlers/                  ← Domain Event Handlers
├── 📁 04-Contracts/                      ← Service Interfaces
└── 📁 05-Integration/                    ← Layer Integration Guides
```

---

## 📚 Documentation Index

### 📁 01-ApplicationServices
*Application services implementing use cases*

| Document | Description |
|----------|-------------|
| *(Coming soon)* | Application service patterns and guidelines |

---

### 📁 02-DTOs
*Data Transfer Objects for API communication*

| Document | Description |
|----------|-------------|
| *(Coming soon)* | DTO design patterns and mapping strategies |

---

### 📁 03-EventHandlers
*Domain event handlers for cross-aggregate communication*

| Document | Description |
|----------|-------------|
| [EVENT_HANDLER_IMPLEMENTATION.md](03-EventHandlers/EVENT_HANDLER_IMPLEMENTATION.md) | Event handler implementation guide |

---

### 📁 04-Contracts
*Service interfaces and contracts*

| Document | Description |
|----------|-------------|
| *(Coming soon)* | Application service interfaces |

---

### 📁 05-Integration
*Cross-layer integration documentation*

| Document | Description |
|----------|-------------|
| [APPLICATION_LAYER_SERVICE_DOMAIN_MENUPRICING_INTEGRATION.md](05-Integration/APPLICATION_LAYER_SERVICE_DOMAIN_MENUPRICING_INTEGRATION.md) | Menu pricing integration between Application and Domain layers |

---

## 🏗️ Architecture Overview

### Application Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                         │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │           Application Services (01-*)                │   │
│  │  Use Cases, Orchestration, Transaction Management   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                  DTOs (02-*)                         │   │
│  │  Data Transfer Objects, Request/Response Models     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │             Event Handlers (03-*)                    │   │
│  │  Domain Event Handlers, Side Effects, Notifications │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │               Contracts (04-*)                       │   │
│  │  Service Interfaces, Ports for External Services    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture - Layer Dependencies

```
┌────────────────────────────────────────────┐
│           Presentation (API/Blazor)        │
│                    ↓                        │
├────────────────────────────────────────────┤
│          APPLICATION LAYER ← You are here  │
│    (Use Cases, DTOs, Event Handlers)       │
│                    ↓                        │
├────────────────────────────────────────────┤
│              DOMAIN LAYER                   │
│    (Entities, Aggregates, Domain Services) │
├────────────────────────────────────────────┤
│           INFRASTRUCTURE LAYER              │
│    (Database, External Services)            │
└────────────────────────────────────────────┘

Dependencies flow INWARD only
Application depends on Domain (never the reverse)
```

---

## 🔌 Key Application Components

### Application Services (Use Cases)

| Service | Location | Description |
|---------|----------|-------------|
| `ReviewApplicationService` | `Services/Reviews/` | Review management use cases |
| `DishPricingApplicationService` | `Services/Pricing/` | Dish pricing calculations |
| `MenuAnalyticsApplicationService` | `Services/Analytics/` | Menu performance analytics |
| `ReservationReportingService` | `Services/Reservations/` | Reservation reporting |
| `OrderPricingApplicationService` | `Services/Orders/` | Order pricing logic |
| `PromotionPricingApplicationService` | `Services/Promotions/` | Promotion pricing |
| `AiImprovementService` | `Services/` | AI-powered recommendations |
| `AdminAuthorizationService` | `Services/` | Admin authorization logic |

### Event Handlers

| Handler | Event | Description |
|---------|-------|-------------|
| `OrderCompletedHandler` | `OrderCompletedEvent` | Post-order completion actions |
| `OrderCancelledHandler` | `OrderCancelledEvent` | Order cancellation handling |
| `AwardLoyaltyPointsHandler` | `OrderCompletedEvent` | Award loyalty points |
| `SendOrderConfirmationHandler` | `OrderPlacedEvent` | Send confirmation email |
| `SendKitchenNotificationHandler` | `OrderPlacedEvent` | Notify kitchen |
| `LoyaltyPointsEarnedHandler` | `LoyaltyPointsEarnedEvent` | Loyalty points processing |
| `LoyaltyTierChangedHandler` | `LoyaltyTierChangedEvent` | Tier change notifications |
| `DishAddedToMenuHandler` | `DishAddedToMenuEvent` | Menu update handling |
| `DishRemovedFromMenuHandler` | `DishRemovedFromMenuEvent` | Menu cleanup |
| `SaleRecordedHandler` | `SaleRecordedEvent` | Sale recording processing |
| `DailySalesSummarizedHandler` | `DailySalesSummarizedEvent` | Daily sales summary |

### DTOs

| DTO | Description |
|-----|-------------|
| `DishDTO` | Dish data transfer object |
| `CategoryDTO` | Category data transfer object |
| `ReviewDTO` | Review data transfer object |
| `OrderDTO` | Order data transfer object |
| `CustomerDTO` | Customer data transfer object |
| `RestaurantDTO` | Restaurant data transfer object |
| `SaleRecordDTO` | Sale record data transfer object |
| `AdminUserDTO` | Admin user data transfer object |
| `AiRecomendationRequestDTO` | AI recommendation request |
| `AiRecomendationResponseDTO` | AI recommendation response |

### Contracts (Interfaces)

| Contract | Description |
|----------|-------------|
| `IAiImprovementService` | AI improvement service interface |
| `IDomainEventDispatcher` | Domain event dispatcher interface |
| `IEmailService` | Email service interface |
| `INotificationService` | Notification service interface |
| `ICacheService` | Cache service interface |
| `IDeadLetterQueueService` | Dead letter queue interface |

---

## 🚀 Quick Start

### For New Developers

1. Understand the layer responsibilities (see Architecture Overview above)
2. Review existing application services in `Services/` folder
3. Check event handlers in `Handlers/` folder
4. Review DTOs in `Dtos/` folder

### For Feature Development

1. **New Use Case**: Create application service in appropriate `Services/` subfolder
2. **New DTO**: Add to `Dtos/` folder with mapping logic
3. **New Event Handler**: Add to `Handlers/` subfolder by aggregate
4. **New Contract**: Add interface to `Contracts/` folder

---

## 🔧 Common Patterns

### Application Service Pattern

```csharp
public class MyApplicationService
{
    private readonly IRepository<MyEntity> _repository;
    private readonly IUnityOfWork _unitOfWork;
    private readonly MyDomainService _domainService; // Domain service

    public MyApplicationService(
        IRepository<MyEntity> repository,
        IUnityOfWork unitOfWork,
        MyDomainService domainService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _domainService = domainService;
    }

    public async Task<Result<MyDTO>> ExecuteUseCaseAsync(MyRequest request)
    {
        // 1. Load aggregate from repository
        var entity = await _repository.GetByIdAsync(request.Id);
        
        // 2. Execute domain logic (via domain service or aggregate)
        var validationResult = _domainService.Validate(entity);
        if (!validationResult.IsValid)
            return Result<MyDTO>.Failure(validationResult.Errors);
        
        // 3. Apply changes
        entity.ApplyChanges(request);
        
        // 4. Persist changes
        await _unitOfWork.CompleteAsync();
        
        // 5. Return DTO
        return Result<MyDTO>.Success(MapToDTO(entity));
    }
}
```

### Event Handler Pattern

```csharp
public class MyEventHandler : ResilientEventHandlerBase<MyDomainEvent>
{
    private readonly INotificationService _notificationService;

    public MyEventHandler(
        INotificationService notificationService,
        IDeadLetterQueueService deadLetterQueue,
        ILogger<MyEventHandler> logger)
        : base(deadLetterQueue, logger)
    {
        _notificationService = notificationService;
    }

    protected override async Task HandleEventAsync(MyDomainEvent domainEvent)
    {
        await _notificationService.SendAsync(
            $"Event occurred: {domainEvent.EntityId}");
    }
}
```

### DTO Mapping Pattern

```csharp
public static class MyEntityMapper
{
    public static MyDTO ToDTO(this MyEntity entity)
    {
        return new MyDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            // Map other properties
        };
    }

    public static MyEntity ToEntity(this MyDTO dto)
    {
        return new MyEntity(dto.Name);
    }
}
```

---

## 📖 Related Documentation

| Layer | Location | Description |
|-------|----------|-------------|
| **Domain** | `SmartMenuOptim.Domain/docs/` | Entities, Aggregates, Domain Services |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/docs/` | Database, External Services |
| **Root** | `docs/` | Solution-wide documentation |

---

## 🔄 Documentation Updates

When updating application documentation:

1. Place new docs in the appropriate numbered folder
2. Update this README index
3. Follow existing naming conventions (UPPERCASE_WITH_UNDERSCORES.md)
4. Include Clean Architecture context in each document

---

*Last Updated: February 2025*
