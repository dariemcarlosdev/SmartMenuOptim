# SmartMenuOptim.Domain_Resume

## Project Intent
This project contains the core business logic and domain entities for SmartMenuOptim. It is the heart of the application and should be independent of any other layer.

## Clean Architecture Guidance
- **Domain Layer:**
  - **Entities:** Represent core business objects with a unique identity. Example: `Order`, `Customer`, or `Restaurant` in a food ordering system.
  - **Value Objects:** Represent descriptive aspects of the domain with no identity, defined only by their properties. Example: `Money` (with currency and amount), `Address`, or `MenuItemPrice`.
  - **Aggregates:** A cluster of related entities and value objects treated as a single unit for data changes. Example: `Order` aggregate containing `OrderItems` (entities) and `ShippingAddress` (value object).
  - **Domain Services:** Contain domain logic that doesn’t naturally fit within an entity or value object. Example: `OrderPricingService` to calculate discounts or taxes for an order, or `ReservationService` to handle table bookings.
- **No dependencies on infrastructure, UI, or application layers.**
- **Defines business rules and invariants.**

## What Should Be Included
- Domain entities and value objects
- Domain services
- Business rules and validation logic
- Domain events

## What Should NOT Be Included
- Infrastructure code (e.g., data access, logging)
- API controllers
- UI components
- Application services

---
This file describes the intent and boundaries of the SmartMenuOptim.Domain project according to Clean Architecture principles.
