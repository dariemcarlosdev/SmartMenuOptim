# Global Entities

Global entities represent system-wide actors and configuration that are not scoped to a single restaurant (tenant). They operate across tenants and manage cross-tenant functionality.

## Current Entities & Highlights
- `AdminUser.cs`
  - Global administrator model
  - Uses `AdminRole` and `AdminPermission` enums (type-safe RBAC)
  - Contains thresholds, default permission helpers, `LastLoginAt`, and validation
  - Can own/manage multiple `Restaurant` entities (navigation: `OwnedRestaurants`)
- `Customer.cs`
  - Global customer account used across restaurants
  - Inherits from `UserBase`
  - Can post reviews and make reservations at many restaurants
- `BusinessRule.cs`
  - Historical tracking for admin-managed configuration
  - Stores `BusinessRuleType`, `Version`, `CreatedAt` and links back to `AdminUser`
  - Useful for auditing changes to thresholds and policies
- `UserBase.cs`
  - Common authentication/profile properties for global user types
  - Normalized lookup helpers for emails/usernames
  - Validation attributes for common fields

## Key Conventions & Requirements
1. Global entities must NOT be scoped to a single `Restaurant`.
2. Prefer enums (`AdminRole`, `AdminPermission`, etc.) over raw strings for roles/permissions.
3. Use composite indexes on frequently queried combinations (e.g., `Email+Username`, `Role+IsActive`).
4. Document and store historical changes where business logic requires auditability (`BusinessRule`, `LoyaltyTransaction`, etc.).

## Relationships with Tenants
- Global entities may reference tenant entities (for example `AdminUser.OwnedRestaurants`), but never replace tenant-scoped ownership for access checks.
- Always validate tenant access in the service layer when a global actor performs tenant-scoped operations.

## Validation & Safety
- Apply validation attributes (`[Required]`, `[EmailAddress]`, `[MaxLength]`) consistently.
- Centralize audit fields via `EntityBase` to maintain consistent timestamps and soft-delete behavior.
- Use `IValidatableObject` for complex cross-field validations where needed.

## Migration & Schema Notes
- Changes to global entities that alter columns used across tenants (e.g., unique indexes on `Email`) must be coordinated with migrations.
- Adding historical tables (e.g., `BusinessRule`) is recommended over in-place replacements for auditability.

## Best Practices
- Keep authentication and authorization logic separate from entity definitions; use policy-based authorization and role checks in services/controllers.
- Prefer explicit permissions and helper methods for checking capabilities (e.g., `AdminUser.HasPermission(...)`).
- Document breaking changes to global indexes or enum values in `MIGRATION GUIDE.md` and release notes.