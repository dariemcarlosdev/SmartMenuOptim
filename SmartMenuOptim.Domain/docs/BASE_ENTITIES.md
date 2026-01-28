# Base Entities

This folder contains foundation-level classes and DTOs that are shared across the domain and do not belong to a specific tenant or global-only grouping.

## Entity Hierarchy
```
EntityBase (abstract)
├── TenantEntityBase (abstract)
│   └── All tenant-specific entities
├── Global entities (AdminUser, Customer, etc.)
└── Pure DTOs and value objects
```

## Purpose
Base entities provide core functionality and patterns that are shared across the domain:
- Common entity properties and behavior
- Audit trail and soft-delete support
- Multi-tenant data isolation
- Concurrency control
- Shared DTOs and value objects

## Core Base Classes

### EntityBase
- Foundation for all persistent entities
- Standardizes audit fields and concurrency:
  ```csharp
  public abstract class EntityBase
  {
      public int Id { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
      public bool IsDeleted { get; set; }
      public bool IsActive { get; set; }
      [Timestamp]
      public uint xmin { get; set; } // PostgreSQL MVCC
  }
  ```
- Managed automatically by `AppDbContext.SaveChangesAsync`:
  - Sets CreatedAt on new entities
  - Updates UpdatedAt on changes
  - Converts deletes to soft-deletes
  - Protects audit field integrity

### TenantEntityBase
- Inherits from EntityBase
- Base for all tenant-scoped entities:
  ```csharp
  public abstract class TenantEntityBase : EntityBase
  {
      [Required]
      public int RestaurantId { get; set; }
      
      [ForeignKey(nameof(RestaurantId))]
      public virtual Restaurant? Restaurant { get; set; }
  }
  ```
- Enforces tenant isolation through RestaurantId
- Provides navigation to owning Restaurant
- Used by all entities in TenantSpecificEntities

## Current Pure Base Types
- `InsightResponse.cs` — DTO for recommendation responses
- Common enums used across layers
- Shared interfaces and contracts
- Value objects and helper types

## Guidelines

### When to Use EntityBase
- For any persisted entity that needs:
  - Unique identification (Id)
  - Audit trail (CreatedAt, UpdatedAt)
  - Soft-delete support
  - Active/inactive status
  - Concurrency control

### When to Use TenantEntityBase
- For any entity that:
  - Belongs to a specific restaurant
  - Requires tenant isolation
  - Needs restaurant navigation property
  - Must support multi-tenant queries

### When to Create Pure Base Types
- DTOs shared across layers
- Value objects without persistence
- Cross-cutting enums and constants
- Shared interfaces and contracts

## Testing & Validation
- Audit Trail
  - Test automatic timestamp management
  - Verify soft-delete behavior
  - Assert concurrency control
- Tenant Isolation
  - Verify RestaurantId constraints
  - Test multi-tenant queries
  - Validate tenant boundaries
- Migration Considerations
  - Coordinate base class changes
  - Handle null migration cases
  - Test upgrade paths

## Best Practices
1. Keep base classes focused and minimal
2. Document inheritance requirements
3. Test automatic behaviors thoroughly
4. Consider impact across all derived classes
5. Maintain backward compatibility
6. Document breaking changes

## Future Considerations
- Enhanced audit trail (who made changes)
- Hierarchical tenant support
- Additional concurrency strategies
- Extended soft-delete patterns
- Improved migration paths