# Profile Entities

This folder contains entities that represent different user profiles in the system. These entities are tightly integrated with ASP.NET Core Identity and form the foundation of the application's authentication and authorization system.

## Current Entities
- `ApplicationUser.cs` — Identity Framework Core Entity (in GlobalEntities)
  - Inherits from `IdentityUser`
  - Links to profile entities via one-to-one relationships
  - Manages profile type synchronization and validation
- `AdminUser.cs` — Administrative Profile
  - Inherits from `EntityBase`
  - Global entity (not tenant-specific)
  - Manages restaurants through `OwnedRestaurants` collection
  - Implements role-based and permission-based access control
- `Customer.cs` — Customer Profile
  - Inherits from `EntityBase`
  - Global entity that can interact with multiple restaurants
  - Tracks preferences, loyalty, and order history
- `StaffMember.cs` — Employee Profile
  - Inherits from `EntityBase`
  - Hybrid tenant model (assigned to specific restaurant)
  - Manages employment status, role, and schedules

## Identity Integration
1. Authentication Flow:
   ```
   ASP.NET Core Identity Tables
          ↓
   ApplicationUser (Users)
          ↓
   Profile Entities (AdminUser/Customer/StaffMember)
   ```

2. Key Relationships:
   - One ApplicationUser to One Profile Entity
   - Profile entity types are mutually exclusive
   - Cascading deletes from ApplicationUser to profiles

3. Navigation Properties:
   ```csharp
   // In ApplicationUser
   public virtual AdminUser? AdminProfile { get; set; }
   public virtual Customer? CustomerProfile { get; set; }
   public virtual StaffMember? StaffProfile { get; set; }

   // In Profile Entities
   public string ApplicationUserId { get; set; }
   public virtual ApplicationUser? ApplicationUser { get; set; }
   ```

## Multi-Tenant Considerations
- AdminUser: Global scope, manages multiple restaurants
- Customer: Global scope, interacts with multiple restaurants
- StaffMember: Restaurant-specific, but uses global authentication

## Key Features
1. AdminUser:
   - Role-based access (SystemAdmin, Owner, Manager, Supervisor)
   - Granular permissions using flags
   - Business rule management
   - Restaurant ownership tracking

2. Customer:
   - Preference management
   - Order history
   - Review tracking
   - Loyalty program participation
   - Multi-restaurant interaction

3. StaffMember:
   - Role assignment (Waiter, Chef, Manager, etc.)
   - Schedule management
   - Order handling
   - Restaurant-specific association

## Best Practices
1. Profile Management:
   - Use ApplicationUser.SynchronizeProfiles() for profile changes
   - Validate profile type transitions
   - Maintain referential integrity

2. Authorization:
   - Check both role and permissions for admin actions
   - Use the AdminPermission flags enum for granular control
   - Implement authorization policies using both Identity roles and custom permissions

3. Data Access:
   - Load profiles with appropriate includes based on need
   - Use projection for API responses to avoid overfetching
   - Cache frequently accessed profile data

## Validation & Constraints
1. AdminUser:
   - Required Role and ApplicationUserId
   - Valid threshold ranges
   - Permission flag validation

2. Customer:
   - Valid contact information
   - Date validation for registration/activity
   - Proper loyalty point tracking

3. StaffMember:
   - Required Role and RestaurantId
   - Valid employment status
   - Emergency contact validation

## Future Considerations
- Enhanced profile analytics and reporting
- Additional profile types (e.g., Vendor, Supplier)
- Extended permission system
- Role-based UI customization
- Profile-specific audit logging
- Enhanced security features (2FA, external login providers)

## Indexing Strategy
- Optimize for authentication queries
- Support efficient profile lookups
- Enable fast role/permission checks
- Facilitate tenant-aware queries

## Security Notes
- Never expose sensitive ApplicationUser data
- Validate profile operations against current user context
- Implement proper authorization checks
- Maintain audit trail for profile changes
- Enforce proper password policies
- Implement rate limiting for authentication attempts