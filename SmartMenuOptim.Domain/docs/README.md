# SmartMenuOptim Domain Layer Documentation

> **Clean Architecture & Domain-Driven Design (DDD) Documentation**

This folder contains comprehensive documentation for the Domain Layer of SmartMenuOptim, organized by architectural concerns following Clean Architecture and DDD principles.

---

## 📁 Documentation Structure

```
SmartMenuOptim.Domain/docs/
├── 📄 README.md                          ← You are here
├── 📄 SMARTMENU.DOMAIN_RESUME.md         ← Layer overview
├── 📁 01-Entities/                       ← Base & Global Entities
├── 📁 02-Aggregates/                     ← Aggregate Roots & Patterns
├── 📁 03-ValueObjects/                   ← Immutable Value Objects
├── 📁 04-DomainServices/                 ← Stateless Business Logic
├── 📁 05-Specifications/                 ← Query Specifications Pattern
├── 📁 06-Events/                         ← Domain Events
└── 📁 07-MultiTenancy/                   ← Multi-Tenant Architecture
```

---

## 📚 Documentation Index

### 📁 01-Entities
*Base classes and global entity definitions*

| Document | Description |
|----------|-------------|
| [BASE_ENTITIES.md](01-Entities/BASE_ENTITIES.md) | `EntityBase` and `TenantEntityBase` documentation |
| [GLOBAL_ENTITIES.md](01-Entities/GLOBAL_ENTITIES.md) | Cross-tenant entities (ApplicationUser, etc.) |
| [ENTITIES_RELATIONSHIP.md](01-Entities/ENTITIES_RELATIONSHIP.md) | Entity relationship diagrams |

---

### 📁 02-Aggregates
*Aggregate Root patterns and specific aggregate documentation*

| Document | Description |
|----------|-------------|
| [AGGREGATES.md](02-Aggregates/AGGREGATES.md) | Overview of all aggregates in the domain |
| [LOOKUP_AGGREGATES.md](02-Aggregates/LOOKUP_AGGREGATES.md) | Tier 2 lightweight lookup aggregates |
| [MENU_AGGREGATE_NOTES.md](02-Aggregates/MENU_AGGREGATE_NOTES.md) | Menu aggregate implementation details |
| [ORDER_AGGREGATE_ENHANCEMENT_SUMMARY.md](02-Aggregates/ORDER_AGGREGATE_ENHANCEMENT_SUMMARY.md) | Order aggregate enhancements |
| [RESTAURANT_CONSOLIDATION.md](02-Aggregates/RESTAURANT_CONSOLIDATION.md) | Restaurant aggregate as tenant root |

---

### 📁 03-ValueObjects
*Immutable value objects with value equality*

| Document | Description |
|----------|-------------|
| [VALUE_OBJECTS.md](03-ValueObjects/VALUE_OBJECTS.md) | All value objects (Money, Email, Address, etc.) |

---

### 📁 04-DomainServices
*Stateless domain services with pure business logic*

| Document | Description |
|----------|-------------|
| [DOMAIN_SERVICE.md](04-DomainServices/DOMAIN_SERVICE.md) | Domain service patterns and guidelines |
| [MENU_COMPOSITION_VALIDATOR_IMPLEMENTATION.md](04-DomainServices/MENU_COMPOSITION_VALIDATOR_IMPLEMENTATION.md) | MenuCompositionValidatorService implementation |
| [MENU_COMPOSITION_VALIDATOR_USAGE.md](04-DomainServices/MENU_COMPOSITION_VALIDATOR_USAGE.md) | Usage examples and integration |
| [SUGGESTED_VALIDATION_SERVICES.md](04-DomainServices/SUGGESTED_VALIDATION_SERVICES.md) | Future validation services roadmap |

---

### 📁 05-Specifications
*Specification pattern for domain queries*

| Document | Description |
|----------|-------------|
| [SPECIFICATION_DOMAIN_QUERY_PATTERN.md](05-Specifications/SPECIFICATION_DOMAIN_QUERY_PATTERN.md) | Specification pattern overview |
| [SPECIFICATION_QUICK_REFERENCE.md](05-Specifications/SPECIFICATION_QUICK_REFERENCE.md) | Quick reference guide |
| [RESERVATION_SPECIFICATIONS.md](05-Specifications/RESERVATION_SPECIFICATIONS.md) | Reservation query specifications |
| [REVIEWS_CONTROLLER_MIGRATION.md](05-Specifications/REVIEWS_CONTROLLER_MIGRATION.md) | Reviews migration to specifications |
| [SALERECORDS_CONTROLLER_MIGRATION.md](05-Specifications/SALERECORDS_CONTROLLER_MIGRATION.md) | SaleRecords migration to specifications |

---

### 📁 06-Events
*Domain events for cross-aggregate communication*

| Document | Description |
|----------|-------------|
| [EVENTS_CLEAN.md](06-Events/EVENTS_CLEAN.md) | Domain events implementation guide |
| [DOMAIN_EVENTS_GUIDE.md](06-Events/DOMAIN_EVENTS_GUIDE.md) | Complete domain events guide |

---

### 📁 07-MultiTenancy
*Multi-tenant architecture patterns*

| Document | Description |
|----------|-------------|
| [MULTITENANT_DOMAIN_MODEL.md](07-MultiTenancy/MULTITENANT_DOMAIN_MODEL.md) | Multi-tenant domain model design |

---

## 🏗️ Architecture Overview

### Domain Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                      DOMAIN LAYER                           │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  Entities   │  │ Aggregates  │  │   Value Objects     │ │
│  │  (01-*)     │  │  (02-*)     │  │   (03-*)            │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐ │
│  │  Domain     │  │ Specifica-  │  │   Domain            │ │
│  │  Services   │  │ tions       │  │   Events            │ │
│  │  (04-*)     │  │  (05-*)     │  │   (06-*)            │ │
│  └─────────────┘  └─────────────┘  └─────────────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Multi-Tenancy (07-*)                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 3-Tier DDD Strategy

| Tier | Type | Example |
|------|------|---------|
| **Tier 1** | Full Aggregate Roots | Menu, Order, Restaurant |
| **Tier 2** | Simple Aggregates | Category, MenuType |
| **Tier 3** | Anemic Entities | SaleRecord, Review |

---

## 🚀 Quick Start

### For New Developers

1. Start with [AGGREGATES.md](02-Aggregates/AGGREGATES.md) - Understand the domain model
2. Read [BASE_ENTITIES.md](01-Entities/BASE_ENTITIES.md) - Learn the base classes
3. Review [DOMAIN_SERVICE.md](04-DomainServices/DOMAIN_SERVICE.md) - Understand service patterns
4. Check [MULTITENANT_DOMAIN_MODEL.md](07-MultiTenancy/MULTITENANT_DOMAIN_MODEL.md) - Multi-tenancy design

### For Feature Development

1. Identify the aggregate you're working with
2. Check the corresponding documentation in `02-Aggregates/`
3. Review domain services in `04-DomainServices/`
4. Use specifications from `05-Specifications/` for queries

---

## 📖 Related Documentation

| Layer | Location | Description |
|-------|----------|-------------|
| **Application** | `SmartMenuOptim.Application/docs/` | Application services, DTOs, Use Cases |
| **Infrastructure** | `SmartMenuOptim.Infrastructure/docs/` | Database, EF Core, Repositories |
| **Root** | `docs/` | Solution-wide documentation |

---

## 🔄 Documentation Updates

When updating domain documentation:

1. Place new docs in the appropriate numbered folder
2. Update this README index
3. Follow existing naming conventions (UPPERCASE_WITH_UNDERSCORES.md)
4. Include Clean Architecture context in each document

---

*Last Updated: February 2025*
