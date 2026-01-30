using Microsoft.AspNetCore.Identity;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


/*
 Using ApplicationUser inheriting from IdentityUser is the standard and recommended approach when working with ASP.NET Core Identity. Here's why it's preferable to a custom UserBase class for your primary authentication entity:

1.	Framework Integration: IdentityUser is the class the ASP.NET Core Identity framework is built to work with. It contains all the essential properties required for authentication and user management, such as Id, UserName, PasswordHash, Email, SecurityStamp, and more. 
    By inheriting from it, ApplicationUser automatically gains all this functionality without you having to redefine it.
2.	Removes Redundancy: Your UserBase class likely duplicates properties already present in IdentityUser. This creates redundant code that is harder to maintain. 
    Consolidating into a single ApplicationUser that extends IdentityUser simplifies your model.
3.	Security and Features: IdentityUser is tightly integrated with Identity's security mechanisms, including password hashing, token generation for password resets, email confirmation, and two-factor authentication. 
    Replacing it with a custom base class means you would have to implement and maintain all these complex security features yourself, which is not recommended.

In short, ApplicationUser becomes your single, central entity for authentication, while other classes like AdminUser or a new TenantUser will hold profile and business-specific data, linking back to ApplicationUser.

Tailoring for Multitenancy:

For a multitenant architecture, the key is to isolate data and users. Your current ApplicationUser is a global entity, which is perfect for users who manage the entire system (super admins) or for users who might access multiple tenants.
To handle tenant-specific users, we can introduce a TenantId to link a user to their respective tenant. A common approach is to add a nullable TenantId to the ApplicationUser.


ApplicationUser not inherits from EntityBase to avoid duplicating properties already defined in IdentityUser:

We manually add the tracking properties here to maintain consistency across our entities.

Why not inherit from EntityBase?

1. IdentityUser already defines key properties like Id, CreatedAt, UpdatedAt, IsDeleted, and IsActive.
2. Inheriting from both IdentityUser and EntityBase would create conflicts and redundancy.
3. By defining the tracking properties directly in ApplicationUser, we ensure they align with our existing conventions without duplication.
4. Framework Compatibility: ASP.NET Core Identity is designed to work with IdentityUser. Inheriting from EntityBase could introduce unexpected behavior or conflicts with the Identity framework's expectations.
5. Simplified Model: Keeping ApplicationUser focused on authentication while linking to profile entities keeps the model clean and easier to maintain.
6. Flexibility: This approach allows us to customize ApplicationUser specifically for authentication needs while still adhering to our tracking conventions.
7. Consistency: By manually adding the tracking properties, we ensure they are consistent with our other entities without the complications of multiple inheritance.
8. Separation of Concerns: ApplicationUser focuses on authentication, while profile entities handle business-specific data. This separation keeps the design modular and easier to manage.

*/

namespace SmartMenuOptim.Domain.Entities.GlobalEntities
{
    /// <summary>
    /// Defines the available profile types for user accounts.
    /// Used to determine which profile entity type is associated with an ApplicationUser.
    /// </summary>
    /// <remarks>
    /// This enum provides type-safe discrimination between different user profiles.
    /// It replaces the string-based UserType for better compile-time checking and maintainability.
    /// The ProfileType directly corresponds to the valid navigation properties on ApplicationUser.
    /// </remarks>
    public enum ProfileType
    {
        /// <summary>
        /// User has no profile assigned yet
        /// </summary>
        None = 0,

        /// <summary>
        /// User has an AdminUser profile (SystemAdmin, Owner, Manager). It is asociated with Roles like SuperAdmin, RestaurantOwner, RestaurantManager.
        /// </summary>
        Admin = 1,

        /// <summary>
        /// User has a Customer profile
        /// </summary>
        Customer = 2,

        /// <summary>
        /// User has a StaffMember profile
        /// </summary>
        Staff = 3
    }

    /// <summary>
    /// Represents the core user for authentication, inheriting from ASP.NET Core IdentityUser.
    /// This entity is the central authentication entity stored in the AspNetUsers table.
    /// Each ApplicationUser can be linked to exactly one profile entity (AdminUser, Customer, or StaffMember).
    /// </summary>
    /// <remarks>
    /// Profile Association Pattern:
    /// - Each ApplicationUser has a ProfileType that determines which profile navigation property is valid
    /// - ProfileId is required when ProfileType is not None
    /// - Only one profile navigation property should be non-null based on ProfileType
    /// - TenantId is optional and links the user to a specific restaurant context
    /// 
    /// Profile Type Discrimination:
    /// Instead of using a string-based UserType, we now use the ProfileType enum which provides:
    /// 1. Type safety and compile-time checking
    /// 2. Clear enumeration of valid profile types
    /// 3. Prevention of invalid profile assignments
    /// 4. Better IDE support with IntelliSense
    /// 5. Cleaner validation and authorization logic
    /// </remarks>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// A descriptive name for the user.
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Foreign key to the tenant this user belongs to.
        /// Links a user to a specific tenant for multi-tenant scenarios.
        /// A null value indicates a global user (e.g., a super admin) who is not associated with a specific tenant.
        /// </summary>
        /// <remarks>
        /// Key Functions:
        /// - Tenant Context: Links an application user to a specific restaurant (tenant)
        /// - Nullable Design: Being nullable (int?) allows for both tenant-scoped and global users
        /// - Access Control: Used to determine which restaurant's data a user can access
        /// 
        /// Usage Patterns:
        /// 1. Global Users (TenantId = null):
        ///    - System Administrators
        ///    - Restaurant owners who manage multiple restaurants
        ///    - Global customer accounts that can interact with multiple restaurants
        /// 
        /// 2. Tenant-Scoped Users (TenantId has value):
        ///    - Restaurant staff members assigned to a specific restaurant
        ///    - Users who should only see data from their assigned restaurant
        /// 
        /// Implementation Features:
        /// - Links directly to the Restaurant entity
        /// - Enables navigation to restaurant context
        /// - Used for tenant-specific data filtering
        /// 
        /// Security Implications:
        /// - Used in tenant filtering at repository/service layer
        /// - Helps enforce data isolation between restaurants
        /// - Part of the authorization context for user operations
        /// 
        /// This design allows for flexible user management where:
        /// - Some users (like staff) are strictly scoped to one restaurant
        /// - Others (like admins) can operate across multiple restaurants
        /// - Customers can interact with multiple restaurants while maintaining proper data isolation
        /// 
        /// The property is a key part of implementing the multi-tenant security model in the application.
        /// </remarks>
        
        public int? RestaurantTenantId { get; set; } // nullable Design to allows for both global and tenant-specific users

        /// <summary>
        /// Type of profile associated with this user.( e.g ., Admin, Customer, Staff).
        /// Determines which profile navigation property is valid.
        /// </summary>
        [Required]
        public ProfileType ProfileType { get; set; } = ProfileType.None;

        /// <summary>
        /// Foreign key to a profile-specific entity (e.g., AdminUser, Customer).
        /// This allows separating authentication data from profile/business data.
        /// Type of profile associated with this user.
        /// Determines which profile navigation property is valid.
        /// </summary>
        public int? ProfileId { get; set; }

        /// <summary>
        /// Computed property that returns the normalized email for consistent lookups.
        /// </summary>
        [NotMapped]
        public override string NormalizedEmail => Email.ToUpperInvariant(); // Override to match IdentityUser property name

        /// <summary>
        /// Computed property that returns the normalized username for consistent lookups.
        /// </summary>
        [NotMapped]
        public override string NormalizedUserName => UserName.ToUpperInvariant(); // Override to match IdentityUser property name

        // ===  Foreign Navigation Properties ===

        /// <summary>
        /// Gets or sets the restaurant entity associated with this tenant.Declared as virtual for lazy loading support. Virtual properties enable EF Core to create proxy classes that support lazy loading of related entities.
        /// Should all foreign key navigation properties be virtual in Entity Framework Core? Yes, declaring foreign key navigation properties as virtual is a common practice in Entity Framework Core to enable lazy loading.
        /// Navigation property to the associated Restaurant (tenant).
        /// Only populated for tenant-scoped users.
        /// </summary>
        /// <remarks>This property represents a navigation to the related <see cref="Restaurant"/> entity
        /// via the foreign key. The value may be <see langword="null"/> if no restaurant is associated.</remarks>
        [ForeignKey(nameof(RestaurantTenantId))]
        public virtual Restaurant? RestaurantTenant { get; set; }

        // === Navigation Properties to profile entities ===
        /// <summary>
        /// Gets or sets the administrator profile associated with this entity.
        /// Navigation property to the AdminUser profile.
        /// Only valid when ProfileType is Admin.
        /// </summary>
        /// <remarks>This property represents a navigation link to the related administrator user profile.
        /// The value may be null if no administrator profile is assigned.</remarks>
        [ForeignKey(nameof(ProfileId))]
        public virtual AdminUser? AdminProfile { get; set; }

        /// <summary>
        /// Gets or sets the customer profile associated with this entity.
        /// Navigation property to the Customer profile.
        /// Only valid when ProfileType is Customer.
        /// </summary>
        /// <remarks>The customer profile provides access to additional information about the customer
        /// linked to this record. This property may be null if no customer profile is associated.</remarks>
        [ForeignKey(nameof(ProfileId))]
        public virtual Customer? CustomerProfile { get; set; }

        /// <summary>
        /// Gets or sets the staff member profile associated with this entity.
        /// Navigation property to the StaffMember profile.
        /// Only valid when ProfileType is Staff.
        /// </summary>
        /// <remarks>This property represents a navigation to the related staff member profile. The value
        /// may be null if no profile is linked.</remarks>
        [ForeignKey(nameof(ProfileId))]
        public virtual StaffMember? StaffProfile { get; set; }

        // === Permission Related Properties for relationships ===

        // permission relationships can be added here in the future as needed

        /// <summary>
        /// Gets or sets the collection of permission group assignments associated with the user.
        /// </summary>
        /// <remarks>Each item in the collection represents a specific permission group to which the user
        /// is assigned. Modifying this collection updates the user's group-based permissions.</remarks>
        public virtual ICollection<UserPermission> PermissionsAssigment { get; set; } = [];

        // === Audit Properties ===

        /// <summary>
        /// Date and time when the user was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the user was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the user is soft-deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current instance is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Concurrency token for optimistic concurrency control, mapped to the PostgreSQL 'xmin' system column.
        /// Concurrency token for optimistic concurrency control, mapped to PostgreSQL's 'xmin' system column.
        /// </summary>
        /// <remarks>
        /// This property maps to PostgreSQL's built-in 'xmin' system column, which is automatically managed by PostgreSQL.
        /// - It does not create a new column in the database
        /// - The 'xmin' system column exists by default for all PostgreSQL tables
        /// - It's used for transaction control and versioning
        /// - The [Timestamp] attribute tells EF Core to use this property for concurrency control
        /// 
        /// PostgreSQL Transaction System Values:
        /// - xmin: Creating Transaction ID (e.g., 5970). Indicates which transaction created/last modified the row.
        /// - xmax: Deleting Transaction ID (e.g., 0). A value of 0 means the row is current/valid.
        ///         When a row is updated/deleted, xmax gets set to the transaction ID that made the change.
        /// 
        /// To verify it in PostgreSQL:
        /// SELECT *, xmin, xmax FROM "YourTableName";
        /// 
        /// Reference: https://www.postgresql.org/docs/current/ddl-system-columns.html
        /// </remarks>
        [Timestamp]
        public uint xmin { get; set; }

        /// <summary>
        /// Checks if the user has a specific permission within a given tenant context.
        /// This method incorporates profile-based rules, cascading permissions, and tenant validation.
        /// </summary>
        /// <param name="permissionName">The name of the permission to check (e.g., "ManageMenus").</param>
        /// <param name="requiredLevel">The minimum access level required.</param> (e.g., Read, Write, Admin)</param>
        /// <param name="tenantId">The ID of the tenant (restaurant) where the permission is required.</param> ( e.g., 42)</param>
        /// <returns>True if the user has the required permission; otherwise, false.</returns>
        public bool HasPermissionTo( string permissionName, AccessLevel requiredLevel, int tenantId)
        {
            if (!IsActive || IsDeleted)
            {
                return false;
            }

            // 1. Handle Inheritance/Cascading: Admin profile has all permissions.
            // This rule grants any permission check if the user is an Admin.
            if (ProfileType == ProfileType.Admin)
            {
                return true;
            }

            // 2. Consider Tenant Context: Ensure the user is associated with the correct tenant.
            // A user can only have permissions for their assigned tenant.
            if (RestaurantTenantId != tenantId)
            {
                return false;
            }

            // 3. Check Profile Type and Direct Permissions:
            // Find a specific, valid permission that matches the criteria.
            var hasDirectPermission = PermissionsAssigment.Any(p =>
                p.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase) &&
                p.RestaurantId == tenantId && // Ensure permission is for the correct tenant
                p.IsValid() &&
                p.HasAccessLevel(requiredLevel));

            return hasDirectPermission;
        }

        /// <summary>
        /// Synchronizes profile relationships based on the current ProfileType.
        /// Similar to BusinessRule.SynchronizeWithAdminUser pattern, this method ensures data consistency
        /// and proper relationship management.
        /// </summary>
        /// <remarks>
        /// Key improvements in the synchronization pattern:
        /// 
        /// 1. Entity-Level Responsibility:
        ///    - Profile synchronization logic moved to entity level
        ///    - Entity is responsible for its own data consistency
        ///    - Follows same pattern as BusinessRule.SynchronizeWithAdminUser()
        /// 
        /// 2. Better Separation of Concerns:
        ///    - Entity manages its own relationships
        ///    - DbContext delegates profile management to entity
        ///    - Profile validation rules closer to the data they validate
        /// 
        /// 3. Improved Data Consistency:
        ///    - Automatically syncs ProfileId with correct profile entity
        ///    - Cleans up other profile relationships when type changes
        ///    - Maintains bidirectional relationships
        /// 
        /// 4. Enhanced Error Handling:
        ///    - Clear validation with descriptive error messages
        ///    - Proper cleanup of invalid states
        ///    - Prevents invalid profile combinations
        /// 
        /// 5. State Management:
        ///    - Tracks modifications to profile relationships
        ///    - Updates timestamps when relationships change
        ///    - Ensures consistency between ProfileType and ProfileId
        /// 
        /// Usage Example:
        /// ```csharp
        /// var appUser = new ApplicationUser { ProfileType = ProfileType.Admin };
        /// var adminProfile = new AdminUser { /* ... */ };
        /// appUser.AdminProfile = adminProfile;
        /// appUser.SynchronizeProfiles(); // Validates and syncs relationships
        /// ```
        /// </remarks>
        /// <returns>True if profiles were synchronized successfully, false if validation failed</returns>
        /// <exception cref="InvalidOperationException">Thrown when profile relationships are invalid</exception>
        public bool SynchronizeProfiles()
        {
            switch (ProfileType)
            {
                case ProfileType.None:
                    if (ProfileId.HasValue)
                        throw new InvalidOperationException("ProfileId must be null when ProfileType is None");
                    
                    // Clean up any existing profile relationships
                    AdminProfile = null;
                    CustomerProfile = null;
                    StaffProfile = null;
                    ProfileId = null;
                    break;

                case ProfileType.Admin:
                    if (AdminProfile == null && !ProfileId.HasValue)
                        throw new InvalidOperationException("AdminProfile must be set when ProfileType is Admin");
                    
                    // Ensure other profiles are cleared
                    CustomerProfile = null;
                    StaffProfile = null;
                    
                    // Sync ProfileId with AdminProfile if present
                    if (AdminProfile != null)
                        ProfileId = AdminProfile.Id;
                    break;

                case ProfileType.Customer:
                    if (CustomerProfile == null && !ProfileId.HasValue)
                        throw new InvalidOperationException("CustomerProfile must be set when ProfileType is Customer");
                    
                    // Ensure other profiles are cleared
                    AdminProfile = null;
                    StaffProfile = null;
                    
                    // Sync ProfileId with CustomerProfile if present
                    if (CustomerProfile != null)
                        ProfileId = CustomerProfile.Id;
                    break;

                case ProfileType.Staff:
                    if (StaffProfile == null && !ProfileId.HasValue)
                        throw new InvalidOperationException("StaffProfile must be set when ProfileType is Staff");
                    
                    // Ensure other profiles are cleared
                    AdminProfile = null;
                    CustomerProfile = null;
                    
                    // Sync ProfileId with StaffProfile if present
                    if (StaffProfile != null)
                        ProfileId = StaffProfile.Id;
                    break;
            }

            // Update timestamp
            UpdatedAt = DateTime.UtcNow;
            return true;
        }
    }
}