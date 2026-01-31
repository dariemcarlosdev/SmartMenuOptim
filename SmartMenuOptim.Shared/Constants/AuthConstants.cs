
namespace SmartMenuOptim.Shared.Constants
{
    /// <summary>
    /// <strong>Application-level configuration constants </strong>for authentication and authorization.
    /// Provides a centralized collection of constants related to authentication and authorization, including role
    /// names, claim types, default passwords, claim values, and default role-to-claim mappings for the application.
    /// </summary>
    /// <remarks>Use the constants defined in this class to standardize role and claim identifiers throughout
    /// the authentication and authorization logic. This helps ensure consistency when assigning roles, evaluating
    /// claims, or configuring access control policies. The nested classes group related constants for easier discovery
    /// and maintenance.</remarks>
    public static class AuthConstants
    {
        /// <summary>
        /// Defines the available user roles in the system, representing different levels of access and responsibility.
        /// </summary>
        /// <remarks>
        /// The hierarchy of roles from highest to lowest privileges is:
        /// 1. SystemAdmin - Full system access
        /// 2. RestaurantAdmin - Restaurant-level administration
        /// 3. RestaurantManager - Restaurant operation management
        /// 4. Staff - Basic restaurant operations
        /// 5. Customer - Limited menu and order access
        /// </remarks>
        public static class Roles
        {
            /// <summary>Has complete access to all system features and configurations.</summary>
            public const string SystemAdmin = "SystemAdmin";
            
            /// <summary>Can manage all aspects of their assigned restaurant(s).</summary>
            public const string RestaurantAdmin = "RestaurantAdmin";
            
            /// <summary>Can manage day-to-day restaurant operations and staff.</summary>
            public const string RestaurantManager = "RestaurantManager";
            
            /// <summary>Has access to basic restaurant operation features.</summary>
            public const string Staff = "Staff";
            
            /// <summary>Has access to menu viewing and order placement features.</summary>
            public const string Customer = "Customer";

            /// <summary>
            /// A complete list of all available roles in the system.
            /// Used for role-based access control and user management.
            /// </summary>
            public static readonly IReadOnlyList<string> AllRoles = new[]
            {
                SystemAdmin,
                RestaurantAdmin,
                RestaurantManager,
                Staff,
                Customer
            };
        }

        /// <summary>
        /// Defines the available claims that represent specific permissions in the system.
        /// Claims are grouped by functionality area for better organization.
        /// </summary>
        /// <remarks>
        /// Claims are used in conjunction with roles to provide fine-grained access control.
        /// Each claim represents a specific operation or access permission within the system.
        /// </remarks>
        public static class Claims
        {
            /// <summary>Allows creation, modification, and deletion of restaurant entities.</summary>
            public const string ManageRestaurants = "ManageRestaurants";
            /// <summary>Allows viewing restaurant information.</summary>
            public const string ViewRestaurants = "ViewRestaurants";
            
            /// <summary>Allows creation, modification, and deletion of menu items and categories.</summary>
            public const string ManageMenus = "ManageMenus";
            /// <summary>Allows viewing menu items and categories.</summary>
            public const string ViewMenus = "ViewMenus";
            
            /// <summary>Allows managing order processing, status updates, and order history.</summary>
            public const string ManageOrders = "ManageOrders";
            /// <summary>Allows viewing orders and their details.</summary>
            public const string ViewOrders = "ViewOrders";
            
            /// <summary>Allows managing staff accounts, roles, and permissions.</summary>
            public const string ManageStaff = "ManageStaff";
            /// <summary>Allows viewing staff information.</summary>
            public const string ViewStaff = "ViewStaff";
            
            /// <summary>Allows managing customer accounts and profiles.</summary>
            public const string ManageCustomers = "ManageCustomers";
            /// <summary>Allows viewing customer information.</summary>
            public const string ViewCustomers = "ViewCustomers";

            /// <summary>Allows configuration and management of loyalty program rules and rewards.</summary>
            public const string ManageLoyaltyProgram = "ManageLoyaltyProgram";
            /// <summary>Allows viewing loyalty program information and status.</summary>
            public const string ViewLoyaltyProgram = "ViewLoyaltyProgram";
            /// <summary>
            /// Allows adding reviews and ratings for restaurants and menu items.  
            /// </summary>
            public const string AddReviews = "AddReviews";
            /// <summary>
            /// Gets the identifier or display name used to access the reviews view within the application.
            /// </summary>
            public static string ViewReviews = "ViewReviews";
        }

        /// <summary>
        /// Defines default passwords for different user types during initial setup or testing.
        /// These should be changed in production environments.
        /// </summary>
        /// <remarks>
        /// These passwords are primarily used for development and testing purposes.
        /// In production, secure password policies should be enforced.
        /// </remarks>
        public static class DefaultPasswords
        {
            /// <summary>Default password for administrator accounts.</summary>
            public const string AdminPassword = "Admin@123";
            /// <summary>Default password for staff accounts.</summary>
            public const string StaffPassword = "Staff@123";
            /// <summary>Default password for customer accounts.</summary>
            public const string CustomerPassword = "Customer@123";
        }

        /// <summary>
        /// Defines standard values for claim operations that can be performed on resources.
        /// </summary>
        /// <remarks>
        /// These values represent the standard CRUD operations and are used to define
        /// granular permissions within claims.
        /// </remarks>
        public static class ClaimValues
        {
            /// <summary>Represents permission to create new resources.</summary>
            public const string Create = "Create";
            /// <summary>Represents permission to read or view resources.</summary>
            public const string Read = "Read";
            /// <summary>Represents permission to modify existing resources.</summary>
            public const string Update = "Update";
            /// <summary>Represents permission to remove or delete resources.</summary>
            public const string Delete = "Delete";
        }

        /// <summary>
        /// Provides the default mapping of roles to their associated claims for access control within the system.
        /// Each role is assigned a set of claims that define its permissions.
        /// </summary>
        /// <remarks>
        /// This dictionary defines the initial permission set for each role.
        /// - SystemAdmin has full access to all features
        /// - RestaurantAdmin has restaurant-specific management access
        /// - RestaurantManager has operational access
        /// - Staff has basic operational access
        /// - Customer has limited access to menus and orders
        /// </remarks>
        public static IDictionary<string, string[]> DefaultRoleClaims = new Dictionary<string, string[]>
        {
            {
                Roles.SystemAdmin, new[]
                {
                    Claims.ManageRestaurants,
                    Claims.ViewRestaurants,
                    Claims.ManageMenus,
                    Claims.ViewMenus,
                    Claims.ManageOrders,
                    Claims.ViewOrders,
                    Claims.ManageStaff,
                    Claims.ViewStaff,
                    Claims.ManageCustomers,
                    Claims.ViewCustomers,
                    Claims.ManageLoyaltyProgram,
                    Claims.ViewLoyaltyProgram
                }
            },
            {
                Roles.RestaurantAdmin, new[]
                {
                    Claims.ViewRestaurants,
                    Claims.ManageMenus,
                    Claims.ViewMenus,
                    Claims.ManageOrders,
                    Claims.ViewOrders,
                    Claims.ManageStaff,
                    Claims.ViewStaff,
                    Claims.ViewCustomers,
                    Claims.ManageLoyaltyProgram,
                    Claims.ViewLoyaltyProgram
                }
            },
            {
                Roles.RestaurantManager, new[]
                {
                    Claims.ViewRestaurants,
                    Claims.ViewMenus,
                    Claims.ManageOrders,
                    Claims.ViewOrders,
                    Claims.ViewStaff,
                    Claims.ViewCustomers,
                    Claims.ViewLoyaltyProgram
                }
            },
            {
                Roles.Staff, new[]
                {
                    Claims.ViewMenus,
                    Claims.ViewOrders,
                    Claims.ViewCustomers
                }
            },
            {
                Roles.Customer, new[]
                {
                    Claims.ViewMenus,
                    Claims.ViewOrders,
                    Claims.AddReviews,
                    Claims.ViewOrders,
                    Claims.ViewLoyaltyProgram,
                    Claims.ViewRestaurants,
                    Claims.ViewReviews
                }
            }
        };
    }
}