using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using System.Linq;

namespace SmartMenuOptim.Shared.Data.Entities.GlobalEntities
{
    /// <summary>
    /// Defines the available role types for admin users
    /// </summary>
    public enum AdminRole
    {
        /// <summary>
        /// System administrator with full access
        /// </summary>
        SystemAdmin = 0,

        /// <summary>
        /// Restaurant owner with full access to their restaurants
        /// </summary>
        Owner = 1,

        /// <summary>
        /// Restaurant manager with limited administrative access
        /// </summary>
        Manager = 2,

        /// <summary>
        /// Supervisor with oversight capabilities
        /// </summary>
        Supervisor = 3
    }

    /// <summary>
    /// Represents the different permissions that can be assigned to an admin user.
    /// Uses [Flags] to allow combining multiple permissions.
    /// </summary>
    [Flags]
    public enum AdminPermission
    {
        None = 0,
        ManageRestaurants = 1 << 0,      // Create, edit, delete restaurants
        ViewReports = 1 << 1,            // Access to analytics and reports
        EditMenus = 1 << 2,              // Modify menus and dishes
        ManageStaff = 1 << 3,            // Manage staff members and schedules
        ManagePromotions = 1 << 4,       // Create and manage promotions
        ViewCustomerData = 1 << 5,       // Access customer information and loyalty
        ManageOrders = 1 << 6,           // Manage and process orders
        ConfigureSettings = 1 << 7,      // Modify restaurant settings and thresholds
        ManageUsers = 1 << 8,            // Manage other admin users
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
    /// Represents an admin user for business/admin logic and sensitive features.
    /// </summary>
    /// <remarks>
    /// Note: AdminUser is not tenant-specific. It acts as the owner or manager of one or more tenants (restaurants). It is a global entity that can manage multiple restaurants.
    /// Each AdminUser can own/manage multiple restaurants (tenants), and each Restaurant references a single AdminUser as its owner.
    /// AdminUser is a global entity and enables multi-tenancy by linking to tenant entities, but is not itself scoped to a single tenant.
    /// </remarks>
    [Table("AdminUsers")]
    public class AdminUser : UserBase
    {
        // === Standalone Properties ===

        /// <summary>
        /// Role of the admin user (SystemAdmin, Owner, Manager, or Supervisor).
        /// </summary>
        [Required(ErrorMessage = "Role is required")]
        [EnumDataType(typeof(AdminRole))]
        public AdminRole Role { get; set; }

        /// <summary>
        /// Minimum number of sales for a dish to be considered popular.
        /// </summary>
        [Range(1, 1000, ErrorMessage = "Sales threshold must be between 1 and 1000")]
        public int SalesThreshold { get; set; } = 30;

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

        /// <summary>
        /// Bitwise flags representing the permissions assigned to this admin user.
        /// Permissions control access to different features or areas of the application.
        /// </summary>
        public AdminPermission Permissions { get; set; } = AdminPermission.None;

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
        /// Navigation property for all restaurants owned by this admin user.
        /// Represents the one-to-many relationship between AdminUser and Restaurant.
        /// One AdminUser can own/manage multiple Restaurants (tenants).
        /// </summary>
        [InverseProperty(nameof(Restaurant.Owner))]
        public ICollection<Restaurant> OwnedRestaurants { get; set; } = new List<Restaurant>();

        /// <summary>
        /// Navigation property for historical business rules created/managed by this admin user.
        /// While most business rules are now properties on AdminUser, this collection maintains
        /// a history of rule changes for audit purposes.
        /// </summary>
        [InverseProperty(nameof(BusinessRule.AdminUser))]
        public ICollection<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();

        /// <summary>
        /// Helper method to get default permissions for a role
        /// </summary>
        public static AdminPermission GetDefaultPermissionsForRole(AdminRole role)
        {
            return role switch
            {
                AdminRole.SystemAdmin => AdminPermission.All,
                AdminRole.Owner => AdminPermission.ManageRestaurants | 
                                 AdminPermission.EditMenus | 
                                 AdminPermission.ViewReports | 
                                 AdminPermission.ManageStaff |
                                 AdminPermission.ManagePromotions |
                                 AdminPermission.ManageOrders |
                                 AdminPermission.ConfigureSettings |
                                 AdminPermission.ViewCustomerData,
                AdminRole.Manager => AdminPermission.EditMenus | 
                                   AdminPermission.ViewReports | 
                                   AdminPermission.ManageStaff |
                                   AdminPermission.ManageOrders |
                                   AdminPermission.ViewCustomerData,
                AdminRole.Supervisor => AdminPermission.ViewReports | 
                                      AdminPermission.ViewCustomerData |
                                      AdminPermission.ManageOrders,
                _ => AdminPermission.None
            };
        }

        /// <summary>
        /// Returns true if this admin user has the specified permission flag.
        /// </summary>
        public bool HasPermission(AdminPermission permission)
            => (Permissions & permission) == permission;

        /// <summary>
        /// Returns true if this admin user is allowed to manage staff schedules for the specified restaurant.
        /// Logic (current policy):
        /// - SystemAdmin: allowed for all restaurants
        /// - Owner: allowed for restaurants they own (if OwnedRestaurants is loaded this is enforced), otherwise Owner role is allowed
        /// - Managers and other roles are NOT allowed under the strict policy. To change that, update this method.
        /// 
        /// To allow an additional role to manage schedules:
        /// 1. Add the new value to the AdminRole enum.
        /// 2. Update `GetDefaultPermissionsForRole` if you want default permissions assigned.
        /// 3. Modify this method to include the new role and (optionally) require a specific permission flag.
        /// 4. Update any seeding or tests to include the new role.
        /// </summary>
        public bool CanManageStaffSchedules(int restaurantId)
        {
            // SystemAdmin has global rights
            if (Role == AdminRole.SystemAdmin)
                return true;

            // Only Owners are allowed under the strict policy
            if (Role != AdminRole.Owner)
                return false;

            // If OwnedRestaurants navigation is populated, prefer strict ownership check
            if (OwnedRestaurants != null && OwnedRestaurants.Any())
                return OwnedRestaurants.Any(r => r.Id == restaurantId);

            // Fallback: allow based on Owner role only (caller/service should enforce tenant scoping when necessary)
            return true;
        }
    }
}
