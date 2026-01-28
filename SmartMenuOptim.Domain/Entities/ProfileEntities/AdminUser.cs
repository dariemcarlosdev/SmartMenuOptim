using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartMenuOptim.Domain.Entities.GlobalEntities;

namespace SmartMenuOptim.Domain.Entities.ProfileEntities
{
    /// <summary>
    /// Defines the available role types for admin users
    /// </summary>
    public enum AdminRoleType
    {
        /// <summary>
        /// System administrator with full access
        /// </summary>
        SystemAdmin = 0,

        /// <summary>
        /// Restaurant owner with full access to their restaurants
        /// </summary>
        RestaurantOwner = 1,

        /// <summary>
        /// Restaurant manager with limited administrative access
        /// </summary>
        RestaurantManager = 2,

        /// <summary>
        /// Supervisor with oversight capabilities
        /// </summary>
        RestaurantSupervisor = 3
    }

    /// <summary>
    /// Represents the different permissions that can be assigned to an admin user.
    /// Uses [Flags] to allow combining multiple permissions.
    /// </summary>
    [Flags]
    public enum AdminPermission
    {   
        /// <summary>
        /// Represents the absence of any value or option.
        /// </summary>
        None = 0,
        /// <summary>
        /// Represents the permission to manage restaurant entities within the system.
        /// </summary>
        ManageRestaurants = 1 << 0,      // Create, edit, delete restaurants
        /// <summary>
        /// Represents permission to view reports within the system.
        /// </summary>
        /// <remarks>Use this value to grant or check access for users who need to view generated reports.
        /// This flag can be combined with other permissions in a bitwise manner if the enumeration supports flag
        /// operations.</remarks>
        ViewReports = 1 << 1,            // Access to analytics and reports
        /// <summary>
        /// Represents permission to edit menu items within the application.
        /// </summary>
        EditMenus = 1 << 2,              // Modify menus and dishes
        /// <summary>
        /// Represents the permission to manage staff members within the system and their schedules.
        /// </summary>
        /// <remarks>This value can be combined with other permission flags to grant multiple access
        /// rights. Assign this flag to enable actions such as adding, editing, or removing staff records.</remarks>
        ManageStaff = 1 << 3,            // Manage staff members and schedules
        /// <summary>
        /// Represents permission to create, modify, or delete promotional offers within the system.
        /// </summary>
        /// <remarks>Assign this value to grant users the ability to manage all aspects of promotions,
        /// including configuring discounts and activating or deactivating promotional campaigns. This flag can be
        /// combined with other permission flags using bitwise operations.</remarks>
        ManagePromotions = 1 << 4,       // Create and manage promotions
        /// <summary>
        /// Represents permission to view customer data within the system.
        /// </summary>
        ViewCustomerData = 1 << 5,       // Access customer information and loyalty
        /// <summary>
        /// Represents the permission to manage orders within the system.
        /// </summary>
        ManageOrders = 1 << 6,           // Manage and process orders
        /// <summary>
        /// Specifies the permission to configure application or system settings.
        /// </summary>
        /// <remarks>Use this value to grant or check access for operations that modify configuration
        /// parameters. This permission is typically required for administrative tasks that affect the application's
        /// behavior or environment.</remarks>
        ConfigureSettings = 1 << 7,      // Modify restaurant settings and thresholds
        /// <summary>
        /// Specifies permission to manage user accounts, including creating, updating, or deleting users.
        /// </summary>
        /// <remarks>Assign this flag to grant access to user management operations. This permission is
        /// typically required for administrative roles responsible for maintaining user information.</remarks>
        ManageUsers = 1 << 8,            // Manage other admin users
        /// <summary>
        /// Represents a value that includes all available permissions.
        /// </summary>
        /// <remarks>This value can be used to grant or check for every permission defined in the set. It
        /// is typically used when full access is required.</remarks>
        All = ~None                       // All permissions
    }

    /* === AdminPermission Usage Examples ===
    
    1. Assigning a single permission:
    ```csharp
    var admin = new AdminUser();
    admin.Permissions = AdminPermission.ManageRestaurants;
    ```

    2. Assigning multiple permissions using bitwise OR (|):
    ```csharp
    var admin = new AdminUser();
    admin.Permissions = AdminPermission.ManageRestaurants | AdminPermission.EditMenus | AdminPermission.ViewReports;
    ```

    3. Assigning all permissions:
    ```csharp
    var admin = new AdminUser();
    admin.Permissions = AdminPermission.All;
    ```

    4. Checking if a user has a specific permission:
    ```csharp
    if (admin.Permissions.HasFlag(AdminPermission.ManageRestaurants))
    {
        // User can manage restaurants
    }
    ```

    5. Checking multiple permissions (must have all):
    ```csharp
    var requiredPermissions = AdminPermission.ManageRestaurants | AdminPermission.EditMenus;
    if ((admin.Permissions & requiredPermissions) == requiredPermissions)
    {
        // User has both ManageRestaurants AND EditMenus permissions
    }
    ```

    6. Adding a permission to existing ones:
    ```csharp
    admin.Permissions |= AdminPermission.ManagePromotions;
    ```

    7. Removing a specific permission:
    ```csharp
    admin.Permissions &= ~AdminPermission.ManageUsers;
    ```

    8. Checking if user has ANY of multiple permissions:
    ```csharp
    var somePermissions = AdminPermission.ViewReports | AdminPermission.ViewCustomerData;
    if ((admin.Permissions & somePermissions) != AdminPermission.None)
    {
        // User has at least one of the specified permissions
    }
    ```

    9. Extension Methods for Cleaner Permission Checks:
    ```csharp
    public static class AdminPermissionExtensions
    {
        public static bool HasPermission(this AdminUser admin, AdminPermission permission)
        {
            return admin.Permissions.HasFlag(permission);
        }

        public static bool HasAllPermissions(this AdminUser admin, AdminPermission permissions)
        {
            return (admin.Permissions & permissions) == permissions;
        }

        public static bool HasAnyPermission(this AdminUser admin, AdminPermission permissions)
        {
            return (admin.Permissions & permissions) != AdminPermission.None;
        }
    }

    // Usage with extension methods:
    if (admin.HasPermission(AdminPermission.ManageRestaurants)) { }
    if (admin.HasAllPermissions(AdminPermission.ManageRestaurants | AdminPermission.EditMenus)) { }
    if (admin.HasAnyPermission(AdminPermission.ViewReports | AdminPermission.ViewCustomerData)) { }
    ```

    // These examples demonstrate how to effectively use the Admin

    ------------------------------------------------------------------------------------------------------

    permission system I'd like to explore further For example:

    •	Adding more permission types
    •	Creating helper methods for common permission combinations
    •	Setting up default permission sets for different roles
    •	Adding validation or authorization middleware   
    •	Creating UI components for permission management

    ------------------------------------------------------------------------------------------------------

    */

    /// <summary>
    /// Represents the profile of a Global admin user (system admin, owner, manager, supervisor) for business/admin logic and sensitive features.
    /// </summary>
    /// <remarks>
    /// Relationship with ApplicationUser:
    /// - One-to-One relationship where AdminUser acts as a profile entity for ApplicationUser
    /// - AdminUser.Id is an auto-incrementing integer primary key
    /// - AdminUser.ApplicationUserId is a string foreign key that matches ApplicationUser.Id (GUID)
    /// - This creates a separation between authentication (ApplicationUser) and business logic (AdminUser)
    /// 
    /// Key Design Points:
    /// 1. Identity Separation:
    ///    - ApplicationUser handles authentication/identity (inherits from IdentityUser)
    ///    - AdminUser handles business logic and admin-specific properties
    /// 
    /// 2. Key Strategy:
    ///    - AdminUser uses integer Id for internal references (e.g., from Restaurant.OwnerId)
    ///    - ApplicationUserId links to the ASP.NET Identity user (string GUID)
    ///    - This allows efficient indexing while maintaining Identity integration
    /// 
    /// 3. Property Delegation:
    ///    - Email and UserName properties delegate to ApplicationUser
    ///    - Maintains single source of truth for identity information
    ///    - Avoids duplication while providing convenient access
    /// 
    /// Multi-Tenancy Notes:
    /// - AdminUser is not tenant-specific (global entity)
    /// - Acts as owner/manager of one or more tenants (restaurants)
    /// - Each Restaurant references AdminUser.Id as OwnerId
    /// - Enables proper multi-tenant management hierarchy
    /// </remarks>
    [Table("AdminUsers")]
    public class AdminUser : EntityBase
    {


        /// <summary>
        /// Foreign key to ApplicationUser. This is a string GUID that matches
        /// ApplicationUser's Id property, creating the one-to-one relationship
        /// between AdminUser profile and ApplicationUser identity.
        /// </summary>
        [Required]
        [MaxLength(450)] // Matches Identity's key length
        public string ApplicationUserId { get; set; } = null!;

        // === Delegated Identity Properties ===
        /*
        DESIGN NOTE: Delegated Properties vs Alternative Approaches
        This class uses delegated properties (Email, UserName) that forward to ApplicationUser 
        instead of alternative approaches like interfaces or base classes. Here's why:
        
        1. Delegated Properties (Current Approach):
           + Maintains single source of truth in ApplicationUser
           + Clear and explicit dependency on ApplicationUser
           + No inheritance complexity or interface overhead
           + Easy to modify or extend individual properties
           + Works well with Entity Framework's change tracking
        */

        [NotMapped]
        public string Email 
        { 
            get => ApplicationUser?.Email ?? string.Empty;
            set 
            {
                if (ApplicationUser != null)
                    ApplicationUser.Email = value;
            }
        }

        [NotMapped]
        public string UserName
        {
            get => ApplicationUser?.UserName ?? string.Empty;
            set 
            {
                if (ApplicationUser != null)
                    ApplicationUser.UserName = value;
            }
        }

        // === Business Rule Properties ===

        /// <summary>
        /// Role of the admin user (SystemAdmin, Owner, Manager, or Supervisor).
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        [EnumDataType(typeof(AdminRoleType))]
        public AdminRoleType Role { get; set; }

        /// <summary>
        /// Bitwise flags representing the permissions assigned to this admin user.
        /// Permissions control access to different features or areas of the application.
        /// </summary>
        public AdminPermission Permissions { get; set; } = AdminPermission.None;

        /// <summary>
        /// Minimum number of sales for a dish to be considered popular.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Sales threshold must be between 1 and 1000")]
        public int SalesThreshold { get; set; } = 100;

        /// <summary>
        /// Minimum sentiment score for a review to be considered positive.
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "Sentiment threshold must be between 0.0 and 1.0")]
        public double SentimentThreshold { get; set; } = 0.6;

        /// <summary>
        /// Minimum number of reviews required for a dish to be considered well-reviewed.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Review count threshold must be between 1 and 1000")]
        public int ReviewCountThreshold { get; set; } = 5;

        /// <summary>
        /// Minimum number of sales required for a dish to be considered well-sold.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Well-sold threshold must be between 1 and 1000")]
        public int WellSoldThreshold { get; set; } = 20;

        /// <summary>
        /// Minimum number of reviews left by a customer for them to be considered a regular customer.
        /// </summary>
        [Range(1, 100, ErrorMessage = "Regular customer review count threshold must be between 1 and 100")]
        public int RegularCustomerReviewCountThreshold { get; set; } = 3;

        /// <summary>
        /// Minimum number of reviews left by a customer for them to be considered as a premium customer.
        /// </summary>
        [Range(1, 100, ErrorMessage = "Premium customer review count threshold must be between 1 and 100")]
        public int PremiumCustomerReviewCountThreshold { get; set; } = 10;

        // === Contact Information ===

        /// <summary>
        /// Last login timestamp for the admin user.
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Phone number of the admin user.
        /// </summary>
        [Phone]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Gets or sets the associated application user for this entity.
        /// </summary>
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        /// <summary>
        /// Navigation property for all restaurants owned by this admin user.
        /// Represents the one-to-many relationship between AdminUser and Restaurant.
        /// One AdminUser can own/manage multiple Restaurants (tenants).
        /// </summary>
        [InverseProperty(nameof(Restaurant.Owner))]
        public virtual ICollection<Restaurant> OwnedRestaurants { get; set; } = new List<Restaurant>();

        /// <summary>
        /// Navigation property for historical business rules created/managed by this admin user.
        /// While most business rules are now properties on AdminUser, this collection maintains
        /// a history of rule changes for audit purposes.
        /// </summary>
        [InverseProperty(nameof(BusinessRule.AdminUser))]
        public virtual ICollection<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();

        // === Business Logic Methods ===

        /// <summary>
        /// Returns true if this admin user is allowed to manage staff schedules for the specified restaurant.
        /// </summary>
        /// <param name="restaurantId">The ID of the restaurant to check.</param>
        /// <returns>True if the admin user can manage staff schedules for the specified restaurant; otherwise, false.</returns>
        public bool CanManageStaffSchedules(int restaurantId)
        {
            if (Role == AdminRoleType.SystemAdmin)
                return true;

            if (Role != AdminRoleType.RestaurantOwner)
                return false;

            if (OwnedRestaurants != null && OwnedRestaurants.Any())
                return OwnedRestaurants.Any(r => r.Id == restaurantId);

            return true;
        }
    }
}
