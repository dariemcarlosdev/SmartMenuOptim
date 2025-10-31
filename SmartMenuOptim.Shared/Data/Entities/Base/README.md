# Base Entities

This folder contains foundation-level classes and DTOs that are shared across the domain and do not belong to a specific tenant or global-only grouping.

## Purpose
Base entities are small, well-scoped models used for cross-cutting concerns such as:
- Shared DTOs and response models
- Value objects and helper types
- Common enums and constants
- Low-level helpers that have no navigation to tenant/global entities

## Notable Base Concepts
- `EntityBase` (introduced)
  - Standardizes audit fields and concurrency:
    - `int Id` (PK)
    - `DateTime CreatedAt`
    - `DateTime UpdatedAt`
    - `bool IsDeleted` (soft-delete)
    - `bool IsActive`
    - `uint xmin` — mapped with `[Timestamp]` for PostgreSQL optimistic concurrency (uses Postgres MVCC)
  - `SaveChangesAsync` behavior (in `AppDbContext`) centralizes:
    - Setting `CreatedAt` / `UpdatedAt`
    - Converting deletes into soft-deletes (`IsDeleted = true`)
    - Protecting immutable audit fields

## Current Entities
- `InsightResponse.cs` — DTO for recommendation responses (pure data container)

## Guidelines for Base Entities
1. Must NOT inherit from `TenantEntityBase` or `GlobalEntity`.
2. Should not carry navigation properties to tenant-specific entities.
3. Should be serializable and safe to use across layers (API, services, tests).
4. Shared enums and DTOs that are consumed cross-cuttingly belong here.

## When to extend Base
- Add shared enums (e.g., `LoyaltyTier`, `StaffRole`) here if they are used by multiple folders.
- Add common DTOs used by multiple APIs or services.

## Testing & Migration Notes
- Audit and soft-delete behavior is centralized — unit and integration tests should assert expected timestamps and soft-delete semantics.
- If a migration introduces or alters `EntityBase` columns (e.g., adding `xmin` mapping), coordinate EF migrations and deployment.