# Tenant-Specific Entities

This folder contains entities that are specific to individual restaurants (tenants). These entities ensure proper data isolation between different restaurants and are the core of the multi-tenant domain model.

## Current Entities
- `Restaurant.cs` — tenant root
  - Acts as the tenant identifier and owns tenant-scoped entities
  - Contains contact, timezone, and business metadata
- `Dish.cs` — menu item
  - Inherits from `TenantEntityBase`
  - Holds pricing, category and menu relationships
- `Category.cs` — menu grouping
  - Inherits from `TenantEntityBase`
  - Enforces unique names per restaurant
- `Review.cs` — customer feedback
  - Inherits from `TenantEntityBase`
  - Links to `Dish` and optionally `Customer`
  - Uses validation and soft-delete
- `SaleRecord.cs` — sales history
  - Inherits from `TenantEntityBase`
  - Tracks `DishId`, `QuantitySold`, `SaleDate`
- `Menu.cs` — aggregated menu (seasonal/serving times)
  - Inherits from `TenantEntityBase`
  - Many-to-many with `Dish` (join table `MenuDishes`)
- `MenuType.cs` — Breakfast/Lunch/Dinner logical grouping
  - Inherits from `TenantEntityBase`
  - Enforced unique per restaurant
- `Table.cs` — seating and floor-plan
  - Inherits from `TenantEntityBase`
  - Tracks `TableNumber`, `Capacity`, `IsAvailable`
- `Reservation.cs` — table reservations
  - Inherits from `TenantEntityBase`
  - References `TableId` and optional `CustomerId`
  - Validated for reasonable reservation windows
- `Order.cs` — orders placed at a restaurant
  - Inherits from `TenantEntityBase`
  - References `CustomerId`, `OrderStatusId`, `OrderItems`
  - Implements `IValidatableObject` to validate totals/date
- `OrderItem.cs` — items within an order
  - Inherits from `TenantEntityBase`
  - Keeps `UnitPrice` and `Quantity` at time of order
- `StaffMember.cs` — employees
  - Inherits from `TenantEntityBase`
  - Includes `StaffRole`, `EmploymentStatus`, contact and emergency info
- `StaffSchedule.cs` — staff shifts
  - Inherits from `TenantEntityBase`
  - Validates shift boundaries and recurrence
  - Tracks created/modified admin/staff for audit
- `Promotion.cs` — restaurant promotions/offers
  - Inherits from `TenantEntityBase`
  - Validates date ranges and discount constraints
- `CustomerLoyalty.cs` and `LoyaltyTransaction.cs` — loyalty program
  - Inherit from `TenantEntityBase`
  - `CustomerLoyalty` is unique per restaurant/customer
  - `LoyaltyTransaction` records point changes and preserves history
- `OrderStatus.cs` — tenant-specific status catalog
  - Inherits from `TenantEntityBase`
  - Allows per-restaurant status customization (Pending, Completed, etc.)

## Key Conventions & Requirements
1. All tenant-scoped entities MUST inherit from `TenantEntityBase`.
2. Every tenant-scoped entity MUST include a `RestaurantId` foreign key and be scoped to a single restaurant.
3. Use explicit mapping and validation attributes:
   - Add `[Table("...")]` when table name differs from class or when documenting schema.
   - Use `[Required]`, `[MaxLength]`, `[Range]`, and `[ForeignKey]` to enforce integrity.
4. Implement `IValidatableObject` for domain-specific validation (dates, totals, shift durations).
5. Implement soft-delete semantics (`IsDeleted`) and standardized audit fields (`CreatedAt`, `UpdatedAt`, `IsActive`) via `EntityBase`/`AppDbContext.SaveChangesAsync`.
6. Use `[InverseProperty]` to disambiguate navigation properties and prevent circular reference issues.
7. Favor explicit FK scalar properties for indexes and query performance (e.g., `OrderStatusId`).
8. Ensure tenant filtering at repository/service layer — never trust client-supplied `RestaurantId` without server-side tenant validation.

## Indexes & Performance
- Favor composite indexes for common tenant queries, e.g.:
  - `(RestaurantId, CreatedAt)`, `(RestaurantId, IsActive, DisplayOrder)`, `(RestaurantId, DishId, CreatedAt)`
- Use join-table shadow properties when helpful (e.g., `MenuDishes` may expose `RestaurantId` as shadow property).
- Consider `EF.Functions.ILike(...)` for case-insensitive Postgres matching where appropriate.

## Seeding & Migrations
- Seed data must respect FK ordering: AdminUsers -> Restaurants -> MenuTypes/Menu -> Categories -> Dishes -> Tables -> StaffMembers -> StaffSchedules -> OrderStatuses -> Orders/OrderItems -> Reviews/SaleRecords -> Loyalty.
- When adding new tenant entities, update `DbSeeder` accordingly and add a matching EF migration in `Migrations/`.

## Best Practices
- Validate tenant context in service layer before any tenant-scoped operation.
- Use repository/unit-of-work patterns with tenant filters to centralize multi-tenant logic.
- Keep tenant-scoped DTOs free of navigation cycles; map to flattened DTOs for API responses.
- Document cross-tenant interactions and avoid any design that exposes data beyond tenant boundaries.

## Future Considerations
- Consider adding `Ingredient`, `InventoryTransaction`, `PaymentTransaction` and enhanced reporting entities with tenant-aware partitioning.
- Investigate partitioning strategies or read replicas if tenant data volume grows significantly.