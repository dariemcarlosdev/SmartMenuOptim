# SmartMenuOptim Multi-Tenant Architecture

## Overview
This document provides architectural guidance for expanding the SmartMenuOptim application with multi-tenant support. It outlines which entities should be tenant-specific and best practices for data isolation and extensibility.

## Current Domain Entities

The following entities are currently defined in the `SmartMenuOptim.Shared.Data.Entities` folder:

- **AdminUser**: Represents an admin user for business/admin logic and sensitive features. Not tenant-specific; acts as owner/manager of one or more restaurants (tenants).
- **BusinessRule**: (If used) Represents business rules or policies in the system.
- **Category**: Represents a category of dishes (e.g., Italian, Salad) for a specific restaurant. Tenant-specific.
- **Customer**: Represents a customer in the system. Global (shared tenancy); can interact with multiple restaurants using the same account.
- **Dish**: Represents a dish offered by a restaurant. Tenant-specific.
- **InsightResponse**: (If used) Represents AI or analytics insights returned to the system.
- **Restaurant**: Represents a restaurant (tenant) in the system. Root tenant entity.
- **Review**: Represents a customer review for a dish in a specific restaurant. Tenant-specific.
- **SaleRecord**: Represents a sales record for a dish. Tenant-specific (by association with Dish/Restaurant).
- **UserBase**: Abstract base class for shared user properties.

> _Note: Some entities (e.g., BusinessRule, InsightResponse) may be utility or supporting types. Review their usage for tenancy relevance as the app evolves._

## Multi-Tenant Entity Reference

The following entities are (or can be) tenant-specific in a multi-tenant restaurant application:

- **Menu**: Each restaurant (tenant) can have its own set of menus (e.g., breakfast, lunch, dinner, seasonal).
- **Ingredient**: If ingredients are managed per restaurant (e.g., inventory, supplier), they should be tenant-specific.
- **Order**: Orders placed by customers are specific to a restaurant.
- **OrderItem**: Items within an order, linked to dishes of a specific restaurant.
- **Reservation**: Table reservations are specific to a restaurant.
- **Table**: Physical tables in a restaurant, if you manage seating/floor plans.
- **Promotion/Discount**: Special offers or discounts that apply only to a specific restaurant.
- **Staff/User**: Employees or users (e.g., waiters, managers) assigned to a specific restaurant.
- **Notification**: System or user notifications scoped to a restaurant.
- **Payment/Transaction**: Payments processed for orders in a specific restaurant.
- **Customer Loyalty Program**: If loyalty points or rewards are tracked per restaurant.

> **Best Practice:**
> Any entity that represents data or business logic unique to a single restaurant (tenant) should be considered tenant-specific to ensure proper data isolation and multi-tenancy support.

## Model Design Principle
- The `Restaurant` entity is the root tenant entity. All tenant-specific data should reference the restaurant.
- The `AdminUser` entity is global and acts as the owner/manager of one or more restaurants (tenants).
- The `Customer` entity is global (shared tenancy) and can interact with multiple restaurants using the same account. Relationships (e.g., reviews, orders) link the customer to a specific restaurant.

## Extending the Model
When adding new features or entities, always consider whether the data should be tenant-specific. If so, add a foreign key to the `Restaurant` entity and document the relationship clearly in code and documentation.

---
_Last updated: 2025-08-02_
