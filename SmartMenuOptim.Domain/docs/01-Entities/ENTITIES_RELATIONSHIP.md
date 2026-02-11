# Entity Relationships in SmartMenuOptim

## 1. User Management and Identity

### Identity Framework Integration
1. ApplicationUser (Core Identity)
   - Inherits from IdentityUser
   - Uses string GUID as primary key (Id)
   - One-to-One relationships with profiles via ProfileId/ProfileType:
     * ApplicationUser → AdminProfile (string Id → int Id)
     * ApplicationUser → CustomerProfile (string Id → int Id)
     * ApplicationUser → StaffProfile (string Id → int Id)

### Profile Entity Pattern
The system uses a consistent profile-based architecture:

1. Identity (Authentication)
   - ApplicationUser handles core identity (inherits IdentityUser)
   - Uses string GUIDs as primary keys
   - ProfileType enum determines active profile type
   - ProfileId links to specific profile entity

2. Profile Entities (Business Logic)
   All profile entities (AdminUser, Customer, StaffMember) follow this pattern:
   - Integer Id as primary key for efficient indexing
   - String ApplicationUserId (required, 450 max length) for foreign key
   - Virtual ApplicationUser navigation property (required)
   - Delegated Email/UserName properties from ApplicationUser
   - Inherit from EntityBase for audit properties

3. Profile-ApplicationUser Data Access Pattern
   The system implements a clean separation between authentication and business logic through:

   a) ApplicationUser Structure:
      ```csharp
      public class ApplicationUser : IdentityUser
      {
          // Determines active profile type
          public ProfileType ProfileType { get; set; }
          
          // Navigation properties to different profiles
          public virtual AdminUser? AdminProfile { get; set; }
          public virtual Customer? CustomerProfile { get; set; }
          public virtual StaffMember? StaffProfile { get; set; }
      }
      ```

   b) Profile Implementation Pattern:
      ```csharp
      public class StaffMember : EntityBase
      {
          // Links to ApplicationUser
          [Required]
          [MaxLength(450)]
          public string ApplicationUserId { get; set; }

          // Navigation property
          [ForeignKey(nameof(ApplicationUserId))]
          public virtual ApplicationUser ApplicationUser { get; set; }

          // Delegated identity properties
          [NotMapped]
          public string Email { get; set; }
          [NotMapped]
          public string UserName { get; set; }
          
          // Business-specific properties
          public DateTime HireDate { get; set; }
          public string Role { get; set; }
          // Other staff-specific fields...
      }
      ```

   c) Data Access Examples:
      ```csharp
      // Accessing identity information through ApplicationUser
      staffMember.Email = staffMember.ApplicationUser.Email;
      staffMember.UserName = staffMember.ApplicationUser.UserName;
      
      // Business logic stays in profile
      staffMember.HireDate = DateTime.Now;
      staffMember.Role = "Chef";
      ```

   d) Common Pattern Across Profiles:
      All profile types (AdminUser, Customer, StaffMember):
      1. Reference ApplicationUser through ApplicationUserId
      2. Access identity information via ApplicationUser relationship
      3. Store only business-specific data in their tables
      4. Delegate identity operations to ApplicationUser

   Key Benefits:
   - Authentication data (email, password, claims) remains in ApplicationUser
   - Business data (role, hire date, etc.) lives in profile entities
   - Profiles access identity information through ApplicationUser relationship
   - Prevents duplication of identity data
   - Maintains clear separation of concerns
   - Supports proper data isolation in multi-tenant system
   - Makes it clear where different types of data should live

4. Profile Types and Scoping:
   a) AdminUser (Global)
      - Not tenant-specific
      - Manages multiple restaurants
      - System administration capabilities

   b) StaffMember (Tenant-Specific)
      - Belongs to specific restaurant (RestaurantId required)
      - Restaurant-scoped operations
      - Employee-specific data

   c) Customer (Global)
      - Not tenant-specific
      - Can interact with multiple restaurants
      - Consumer-focused features

5. Key Strategy:
   - ApplicationUser: string GUID primary key (Id)
   - Profile Entities: integer primary key (Id)
   - Profile → ApplicationUser: string foreign key (ApplicationUserId)
   - Consistent across all profile types
       
       // Permission-specific properties
       public string Name { get; set; }
       public string Description { get; set; }
   }
6. Benefits:
   - Clean separation of authentication and business logic
   - Efficient indexing for business operations
   - Consistent pattern across all profile types
   - Clear tenant boundaries where needed
   - Type-safe profile discrimination
   ```
   This maintains the same pattern of linking to ApplicationUser while keeping permission-specific data separate.

## 2. Permission Management

### UserPermission Entity Relationships
1. Core Relationships:
   - Many-to-One → ApplicationUser (Required)
   - Many-to-One → Restaurant (Required)
   
2. Core Entity Structure:
   - Inherits from EntityBase for audit properties
   - Contains permission metadata (Name, Description, Area)
   - Includes expiration tracking (ExpiresAt)
   - Manages access levels (None, Read, Write, Admin)

3. Key Relationships:
   - **ApplicationUser to UserPermission (One-to-Many)**:
     * Foreign Key: ApplicationUserId (required, string)
     * Navigation Property: ApplicationUser
     * Collection in ApplicationUser: PermissionsAssignment
     * Enables multiple permissions per user across different restaurants
   
   - **Restaurant to UserPermission (One-to-Many)**:
     * Foreign Key: RestaurantId (required, int)
     * Navigation Property: Restaurant
     * Collection in Restaurant: UserPermissions
     * Scopes permissions to specific restaurants

4. Permission Implementation Pattern:

      ```csharp
   public class UserPermission : EntityBase { 
   [Required]
   public string ApplicationUserId { get; set; }
   [Required]
   public int RestaurantId { get; set; }
   
   [Required]
   public string Name { get; set; }
   
   [Required]
   public AccessLevel AccessLevel { get; set; }
   
   // Navigation properties
   public virtual ApplicationUser? ApplicationUser { get; set; }
   public virtual Restaurant? Restaurant { get; set; }
   }
      ```


5. Relationship Diagram:

ApplicationUser 1 --- * UserPermission * --- 1 Restaurant


6. Multi-Tenant Authorization:
   - Permissions are always scoped to a specific restaurant (tenant)
   - Users can have different permission levels across restaurants
   - AdminUsers automatically receive full permissions in their owned restaurants
   - Permissions support temporary access through ExpiresAt property

### Permission Validation and Access Control
1. Permission Checking:

// Example permission check in ApplicationUser

    public bool HasPermission(string permissionName, AccessLevel requiredLevel, int tenantId)
    { 
    if (ProfileType == ProfileType.Admin) return true;
       return PermissionsAssignment.Any(p =>
       p.Name == permissionName &&
       p.RestaurantId == tenantId &&
       p.IsValid() &&
       p.HasAccessLevel(requiredLevel));
    }

2. Permission Inheritance Rules:
   - Admin profile users have implicit full access
   - Higher access levels include lower level permissions
   - Restaurant owners get full access to their restaurants
   - Expired permissions are automatically invalid

## 3. Restaurant Management

### Restaurant (Tenant Root)
1. Core Relationships:
   - Many-to-One → Owner (AdminUser) (Required)
   - One-to-Many → StaffMembers
   - One-to-Many → Tables
   - One-to-Many → Categories
   - One-to-Many → UserPermissions

2. Menu-Related Collections:
   - One-to-Many → Menus
   - One-to-Many → MenuTypes
   - One-to-Many → Dishes

3. Order-Related Collections:
   - One-to-Many → Orders
   - One-to-Many → OrderStatuses
   - One-to-Many → SaleRecords

4. Customer-Related Collections:
   - One-to-Many → Reviews
   - One-to-Many → CustomerLoyalties
   - One-to-Many → Promotions

### Table Management
1. Table Entity:
   - Many-to-One → Restaurant
   - One-to-Many → Reservations

## 4. Menu Management

### Menu-Dish Relationship
1. Rich Many-to-Many Pattern (MenuDish):
   - Properties:
     * DisplayOrder: Presentation order
     * SpecialPrice: Menu-specific pricing
     * Notes: Preparation instructions
     * IsActive: Availability control
   - Tenant scoping via RestaurantId

2. Menu Entity:
   - Many-to-One → Restaurant (Required)
   - Many-to-One → MenuType (Required)
   - Many-to-Many → Dishes (via MenuDish)

3. Dish Entity:
   - Many-to-One → Restaurant (Required)
   - Many-to-One → Category (Required)
   - One-to-Many → OrderItems
   - One-to-Many → Reviews
   - One-to-Many → SaleRecords
   - Many-to-Many → Menus (via MenuDish)

## 5. Order Management

### Order Structure
1. Core Relationships:
   - Many-to-One → Restaurant (Required)
   - Many-to-One → Customer (Optional)
   - Many-to-One → HandledBy (StaffMember) (Optional)
   - Many-to-One → OrderStatus (Required)
   - One-to-Many → OrderItems

2. OrderItem Entity:
   - Many-to-One → Order
   - Many-to-One → Dish

### OrderStatus Management
1. OrderStatus Entity:
   - Many-to-One → Restaurant
   - One-to-Many → Orders

## 6. Loyalty and Promotions Management

### Customer Loyalty Program
1. CustomerLoyalty Entity:
   - Many-to-One → Restaurant (Required)
   - Many-to-One → Customer (Required)
   - One-to-Many → LoyaltyTransactions

2. Promotion Entity:
   - Many-to-One → Restaurant
   - Manages discounts and special offers

## 7. Multi-Tenancy Implementation

### Tenant Hierarchy
1. Entity Scoping:
   - Global Entities: ApplicationUser, AdminUser
   - Tenant-Specific: Most other entities
   - Cross-Tenant: CustomerLoyalty, Review

2. Inheritance Pattern:
   - TenantEntityBase provides RestaurantId
   - Used by most domain entities

### Data Isolation
1. Cascade Delete Behaviors:
   - Tenant-Specific: DeleteBehavior.Cascade
   - Cross-Tenant: DeleteBehavior.Restrict/SetNull
   - Profiles: DeleteBehavior.Cascade

2. Indexing Strategy:
   - Centralized in OnModelCreating
   - Composite indexes for tenant queries
   - Business rule constraints
   - Performance optimization patterns

This organization reflects the logical business domains while maintaining the relationships between entities and enforcing proper multi-tenant boundaries.