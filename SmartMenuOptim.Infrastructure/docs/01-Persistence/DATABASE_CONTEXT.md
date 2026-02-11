# Database Context Documentation

## Overview
The `AppDbContext` serves as the main database context for the SmartMenuOptim application, integrating ASP.NET Core Identity with our custom entity framework implementation. It provides centralized data access and management for both authentication/authorization and business domain entities.

## Architecture

### Identity Framework Integration
The context extends `IdentityDbContext<ApplicationUser>` and manages the following Identity tables:

1. **Users** (`AspNetUsers`)
   - Stores core user information
   - Properties: Username, Email, PasswordHash, SecurityStamp
   - Primary authentication store
   - Custom extensions for profile associations

2. **Roles** (`AspNetRoles`)
   - Role definitions (e.g., Admin, Customer, Staff)
   - Properties: RoleId, Name, NormalizedName
   - Supports role-based authorization

3. **UserRoles** (`AspNetUserRoles`)
   - Many-to-many relationship between Users and Roles
   - Enables multiple roles per user
   - Supports flexible role assignments

4. **UserClaims** (`AspNetUserClaims`)
   - User-specific permissions and attributes
   - Custom claims for fine-grained authorization
   - Extended user metadata storage

5. **UserLogins** (`AspNetUserLogins`)
   - External authentication provider data
   - OAuth/social login support
   - Links external accounts to local users

6. **UserTokens** (`AspNetUserTokens`)
   - Security token storage
   - Supports: 
     - Password reset
     - Email confirmation
     - Two-factor authentication
     - Custom token types

7. **RoleClaims** (`AspNetRoleClaims`)
   - Role-based permissions
   - Claims inherited by users in roles
   - Role-specific attributes

### Entity Organization

#### Global Entities
- ApplicationUsers
- AdminUsers
- Customers
- StaffMembers
- BusinessRules

#### Tenant-Specific Entities
- Restaurants
- Categories
- Dishes
- Menus
- MenuTypes
- Orders
- Reviews
- Tables
- StaffSchedules
- Promotions
- CustomerLoyalties

## Multi-Tenancy Implementation

### Design Patterns
1. **Tenant Hierarchy**
   - Restaurant acts as tenant root
   - TenantEntityBase provides RestaurantId scoping
   - Global entities remain tenant-independent

2. **Data Isolation**
   - Per-tenant data separation
   - Cross-tenant relationship validation
   - Tenant-aware queries and indexes

### Key Features

#### 1. Audit Trail
- Automatic timestamp management
- Creation/modification tracking
- Soft delete implementation
- Optimistic concurrency support

#### 2. Profile Management
- One-to-one relationship with ApplicationUser
- Profile type validation
- Profile synchronization
- Clean profile transitions

#### 3. Business Rule Management
- Rule versioning
- Active rule tracking
- Admin user synchronization
- Historical record maintenance

## Database Design

### Indexing Strategy
1. **Centralized Index Definition**
   - All indexes defined in OnModelCreating
   - Consistent naming conventions
   - Performance-optimized structures

2. **Index Types**
   - Composite indexes for tenant queries
   - Unique constraints for business rules
   - Covering indexes for common queries
   - FK indexes for relationship navigation

### Cascade Behaviors
1. **Tenant-Specific Relationships**
   - DeleteBehavior.Cascade within tenant boundary
   - Automatic cleanup of related records

2. **Cross-Tenant Relationships**
   - DeleteBehavior.Restrict or SetNull
   - Preserves referential integrity
   - Prevents accidental data loss

3. **Profile Relationships**
   - DeleteBehavior.Cascade for profile cleanup
   - Maintains data consistency

## Performance Considerations

### SaveChanges Implementation
1. **Audit Field Management**
   - Automatic timestamp updates
   - Soft delete conversion
   - Creation tracking

2. **Concurrency Handling**
   - Optimistic concurrency checks
   - Version tracking
   - Conflict detection

### Query Optimization
1. **Eager Loading Patterns**
   - Include statements for related data
   - Composite indexes support
   - Query projection optimization

2. **Tenant Filtering**
   - Efficient tenant boundary enforcement
   - Index utilization for tenant queries
   - Optimal join strategies

## Best Practices

### Data Access
1. Use navigation properties for related data access
2. Leverage composite indexes for filtered queries
3. Implement proper tenant boundary checks
4. Maintain audit trail consistency

### Validation
1. Entity-level validation in domain models
2. Cross-entity validation in context
3. Tenant boundary validation
4. Profile consistency checks

### Security
1. Proper tenant isolation
2. Role-based access control
3. Claim-based permissions
4. Token management

## Migration Management
- Centralized index definitions
- Tenant-aware migrations
- Profile synchronization handling
- Business rule versioning

## Error Handling
1. Concurrency conflict detection
2. Validation error aggregation
3. Tenant boundary violation checks
4. Profile consistency enforcement