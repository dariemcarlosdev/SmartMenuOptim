# Global Entities

Global entities represent system-wide actors and configuration that are not scoped to a single restaurant (tenant). They operate across tenants and manage cross-tenant functionality.

## Current Entities & Highlights
- `ApplicationUser.cs`
  - Core identity entity (inherits from IdentityUser)
  - Links to profile entities (AdminUser, Customer, StaffMember)
  - Manages profile synchronization and validation
  - Handles tenant context through TenantId
- `AdminUser.cs`
  - Global administrator model
  - Uses `AdminRole` and `AdminPermission` enums (type-safe RBAC)
  - Contains thresholds, default permission helpers, `LastLoginAt`, and validation
  - Can own/manage multiple `Restaurant` entities (navigation: `OwnedRestaurants`)
  - Links to ApplicationUser via one-to-one relationship
- `Customer.cs`
  - Global customer account used across restaurants
  - Links to ApplicationUser via one-to-one relationship
  - Can post reviews and make reservations at many restaurants
  - Manages preferences and loyalty across multiple restaurants
- `BusinessRule.cs`
  - Historical tracking for admin-managed configuration
  - Stores `BusinessRuleType`, `Version`, `CreatedAt` and links back to `AdminUser`
  - Useful for auditing changes to thresholds and policies
- `UserBase.cs`
  - Common authentication/profile properties for global user types
  - Normalized lookup helpers for emails/usernames
  - Validation attributes for common fields

## Identity Framework Integration
1. Core Authentication:
   - ApplicationUser is the central identity entity
   - Links to ASP.NET Core Identity tables:
     - Users (ApplicationUser)
     - Roles (IdentityRole)
     - UserRoles (junction table)
     - UserClaims, UserLogins, UserTokens

2. Profile Relationships:
   - One-to-one relationships between ApplicationUser and profiles
   - Profile types are mutually exclusive
   - Managed through ProfileType enum and navigation properties

3. Multi-Tenant Context:
   - ApplicationUser tracks TenantId for context
   - Profiles implement different tenant strategies:
     - AdminUser: Global scope, manages multiple tenants
     - Customer: Global scope, interacts with multiple tenants
     - StaffMember: Tenant-specific assignment

## Key Conventions & Requirements
1. Global entities must NOT be scoped to a single `Restaurant`
2. Prefer enums (`AdminRole`, `AdminPermission`, etc.) over raw strings for roles/permissions
3. Use composite indexes on frequently queried combinations (e.g., `Email+Username`, `Role+IsActive`)
4. Document and store historical changes where business logic requires auditability
5. Always implement proper navigation properties to ApplicationUser
6. Follow Identity Framework best practices for authentication

## Relationships with Tenants
- Global entities may reference tenant entities (for example `AdminUser.OwnedRestaurants`), but never replace tenant-scoped ownership for access checks
- Always validate tenant access in the service layer when a global actor performs tenant-scoped operations
- Use proper navigation properties and foreign keys for tenant relationships

## Validation & Safety
- Apply validation attributes (`[Required]`, `[EmailAddress]`, `[MaxLength]`) consistently
- Centralize audit fields via `EntityBase` to maintain consistent timestamps and soft-delete behavior
- Use `IValidatableObject` for complex cross-field validations where needed
- Implement proper profile synchronization through ApplicationUser

## Migration & Schema Notes
- Changes to global entities that alter columns used across tenants must be coordinated with migrations
- Adding historical tables (e.g., `BusinessRule`) is recommended over in-place replacements for auditability
- Consider impact on Identity Framework tables when making changes

## Best Practices
- Keep authentication and authorization logic separate from entity definitions
- Use policy-based authorization and role checks in services/controllers
- Prefer explicit permissions and helper methods for checking capabilities
- Document breaking changes to global indexes or enum values
- Implement proper profile management and synchronization
- Use appropriate eager loading strategies for related entities
- Consider caching strategies for frequently accessed global data

## Future Considerations
- Enhanced audit logging for identity operations
- Advanced permission management system
- Cross-tenant analytics and reporting
- Improved caching strategies
- Enhanced security features (2FA, external providers)
- Profile-specific customization options