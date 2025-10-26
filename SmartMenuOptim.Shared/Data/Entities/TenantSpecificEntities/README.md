# Tenant-Specific Entities

This folder contains entities that are specific to individual restaurants (tenants). These entities ensure proper data isolation between different restaurants.

## Current Entities
- `Restaurant.cs`: The root tenant entity
  - Acts as the tenant identifier
  - Contains restaurant-specific information
  - Links to other tenant-specific entities
  - Primary coordinator of tenant isolation

- `Dish.cs`: Restaurant-specific menu items
  - Inherits from `TenantEntityBase`
  - Belongs to a single restaurant
  - Contains pricing and menu information
  - Supports multiple categories

- `Category.cs`: Restaurant-specific menu categories
  - Inherits from `TenantEntityBase`
  - Organizes dishes within a restaurant
  - Scoped to a single restaurant
  - Enables menu organization

- `Review.cs`: Restaurant-specific customer reviews
  - Inherits from `TenantEntityBase`
  - Links reviews to specific dishes and restaurants
  - Contains review-specific data
  - Maintains customer feedback isolation

- `SaleRecord.cs`: Restaurant-specific sales data
  - Inherits from `TenantEntityBase`
  - Tracks sales for specific dishes
  - Isolated per restaurant
  - Maintains financial data separation

- `Menu.cs`: Restaurant-specific menus
  - Inherits from `TenantEntityBase`
  - Groups dishes for different serving times
  - Supports seasonal and special menus
  - Restaurant-specific pricing and availability

- `Table.cs`: Restaurant-specific seating
  - Inherits from `TenantEntityBase`
  - Manages restaurant floor plans
  - Tracks table status and capacity
  - Supports reservation management

- `Order.cs`: Restaurant-specific orders
  - Inherits from `TenantEntityBase`
  - Links customers to specific restaurants
  - Contains order details and status
  - Maintains order history per restaurant

- `OrderItem.cs`: Restaurant-specific order details
  - Inherits from `TenantEntityBase`
  - Links to specific dishes in orders
  - Contains item-specific customizations
  - Maintains pricing at time of order

## Guidelines for Tenant-Specific Entities
1. Must inherit from `TenantEntityBase`
2. Must have a `RestaurantId` foreign key
3. Must be isolated to a single restaurant
4. Should implement proper multi-tenant data access controls
5. Should include XML documentation explaining tenant relationship
6. Should follow consistent naming conventions
7. Must not expose data across tenant boundaries

## Best Practices
1. Always validate tenant context in service layer
2. Use repository patterns with tenant filtering
3. Implement soft delete for data retention
4. Include audit trails for tenant operations
5. Consider performance impact of tenant isolation
6. Document any cross-tenant dependencies

## Future Considerations
Consider adding these tenant-specific entities as needed:
- `Promotion.cs`: Restaurant-specific discounts and offers
- `StaffMember.cs`: Restaurant-specific employees
- `Ingredient.cs`: Restaurant-specific inventory
- `LoyaltyProgram.cs`: Restaurant-specific rewards
- `PaymentTransaction.cs`: Restaurant-specific payments