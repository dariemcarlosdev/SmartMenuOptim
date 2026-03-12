# Restaurant Management - Architecture Decision Record

> **Date**: 2025-02-28  
> **Status**: Approved  
> **Decision**: Hybrid Approach (Service Layer for MVP, CQRS for Post-MVP)

---

## Context

The Restaurant Management feature is the foundational MVP feature for SmartMenuOptimizer. We needed to decide on the architecture pattern for the Application and API layers.

## Decision

**Chosen: Option C - Hybrid Approach**

| Layer | MVP Pattern | Post-MVP Pattern |
|-------|-------------|------------------|
| Application | Service Layer (IRestaurantService, etc.) | CQRS + MediatR |
| API | Controllers → Services | Controllers → ISender |
| Validation | DataAnnotations | FluentValidation + Pipeline |
| Structure | By Layer | Feature-Slice (Vertical) |

---

## Rationale

### Why Service Layer for MVP?
1. **Faster delivery** - Less boilerplate than full CQRS
2. **Simpler for CRUD** - Restaurant Management is primarily CRUD operations
3. **Already implemented** - Services (Phase 2) completed and tested
4. **Team familiarity** - Traditional service pattern is well understood

### Why CQRS for Post-MVP?
1. **Scalability** - One handler per operation scales better
2. **Testability** - Individual handlers are easier to unit test
3. **Separation** - Read (Query) vs Write (Command) optimization
4. **Pipeline behaviors** - Validation, logging, caching via MediatR
5. **Domain events** - Better support for event-driven architecture

---

## Implementation Status

### ✅ Completed (MVP)

```
SmartMenuOptim.Application/
├── Common/
│   └── Result.cs                     ✅ Result pattern
├── Extensions/
│   ├── RestaurantMappingExtensions.cs ✅ Entity-DTO mapping
│   └── ApplicationServiceCollectionExtensions.cs ✅ DI registration
└── Services/
    └── Restaurant/
        ├── IRestaurantService.cs     ✅
        ├── RestaurantService.cs      ✅
        ├── IMenuService.cs           ✅
        ├── MenuService.cs            ✅
        ├── ICategoryService.cs       ✅
        └── CategoryService.cs        ✅

SmartMenuOptim.API/
└── Controllers/v1/
    ├── RestaurantsController.cs      ✅
    ├── MenusController.cs            ✅
    └── CategoriesController.cs       ✅
```

### 🔮 Future (Post-MVP CQRS Refactoring)

```
SmartMenuOptim.Application/
└── Features/
    └── Restaurant/
        ├── Commands/
        │   ├── CreateRestaurant/
        │   │   ├── CreateRestaurantCommand.cs
        │   │   ├── CreateRestaurantCommandHandler.cs
        │   │   └── CreateRestaurantCommandValidator.cs
        │   ├── UpdateRestaurant/
        │   └── DeleteRestaurant/
        └── Queries/
            ├── GetRestaurantById/
            │   ├── GetRestaurantByIdQuery.cs
            │   └── GetRestaurantByIdQueryHandler.cs
            └── GetAllRestaurants/
```

---

## CQRS Refactoring Checklist (Post-MVP)

### Phase 1: Infrastructure
- [ ] Create `ICommand<TResponse>` interface
- [ ] Create `IQuery<TResponse>` interface  
- [ ] Create `ICommandHandler<TCmd, TRes>` interface
- [ ] Create `IQueryHandler<TQuery, TRes>` interface
- [ ] Create `ValidationBehavior<TReq, TRes>` pipeline

### Phase 2: Commands
- [ ] CreateRestaurantCommand + Handler + Validator
- [ ] UpdateRestaurantCommand + Handler + Validator
- [ ] DeleteRestaurantCommand + Handler
- [ ] SetBusinessHoursCommand + Handler + Validator
- [ ] (Repeat for Menu and Category)

### Phase 3: Queries
- [ ] GetRestaurantByIdQuery + Handler
- [ ] GetAllRestaurantsQuery + Handler
- [ ] GetRestaurantDetailQuery + Handler
- [ ] (Repeat for Menu and Category)

### Phase 4: Controllers
- [ ] Replace IRestaurantService with ISender
- [ ] Replace IMenuService with ISender
- [ ] Replace ICategoryService with ISender

### Phase 5: Domain Events
- [ ] RestaurantCreatedEvent
- [ ] RestaurantUpdatedEvent
- [ ] MenuCreatedEvent
- [ ] DishAddedToMenuEvent (already exists)
- [ ] Create event handlers

---

## References

- [REST-API-QUICK-PROMPT.md](../../../AI/Prompts/REST-API-QUICK-PROMPT.md)
- Framework: .NET 8/9 | Clean Architecture + DDD + CQRS + MediatR
- Structure: Feature-Slice (Vertical Slice) Modularity

---

## Decision Outcome

✅ **MVP delivered with Service Layer pattern**
- Build successful
- All CRUD operations implemented
- RFC 7807 ProblemDetails for errors
- Proper HTTP status codes
- CancellationToken support

🔮 **Post-MVP refactoring planned**
- CQRS + MediatR pattern
- FluentValidation pipeline
- Domain events dispatched
- Feature-Slice organization
