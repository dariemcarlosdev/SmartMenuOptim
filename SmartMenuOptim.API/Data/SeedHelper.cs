using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using System.Transactions;
using SmartMenuOptim.Shared.Constants;

namespace SmartMenuOptim.API.Data
{
    /// <summary>
    /// Provides helper methods for seeding roles, claims, and user accounts in the application's identity system during
    /// initialization or testing.
    /// </summary>
    /// <remarks>This class is intended for internal use to automate the setup of identity-related data, such
    /// as creating roles with associated claims and generating users with specific roles and profiles. All methods are
    /// static and designed to be called during application startup or in test environments. The class is not
    /// thread-safe and should not be used concurrently from multiple threads.</remarks>
    internal static class SeedHelper
    {
        /// <summary>
        /// Creates all predefined roles and assigns their associated claims using the specified role manager. Roles and
        /// claims are added only if they do not already exist.
        /// </summary>
        /// <remarks>This method ensures that all roles defined in the application are present in the
        /// identity store and that each role has its default claims assigned. For administrative roles, additional CRUD
        /// claims are added. The method is safe to call multiple times; existing roles are not recreated.</remarks>
        /// <param name="roleManager">The role manager used to create roles and assign claims. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation of creating roles and assigning claims.</returns>
        public static async Task CreateRolesAndClaims(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in AuthConstants.Roles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);

                    // Add claims for this role if defined
                    if (AuthConstants.DefaultRoleClaims.TryGetValue(roleName, out var claims))
                    {
                        foreach (var claim in claims)
                        {
                            await roleManager.AddClaimAsync(role, new Claim(claim, AuthConstants.ClaimValues.Read));
                            
                            // Add CRUD claims for admin roles
                            if (roleName == AuthConstants.Roles.SystemAdmin || 
                                roleName == AuthConstants.Roles.RestaurantAdmin)
                            {
                                await roleManager.AddClaimAsync(role, new Claim(claim, AuthConstants.ClaimValues.Create));
                                await roleManager.AddClaimAsync(role, new Claim(claim, AuthConstants.ClaimValues.Update));
                                await roleManager.AddClaimAsync(role, new Claim(claim, AuthConstants.ClaimValues.Delete));
                            }
                        }
                    }
                }
            }
        }
        
        
        /// <summary>
        /// Creates a new admin user with the specified credentials, profile information, and administrative role.
        /// </summary>
        /// <remarks>The created admin user will have all available admin permissions and will be marked
        /// as active. The underlying application user is created with the provided credentials and profile details.
        /// Ensure that the username and email are not already in use to avoid conflicts.</remarks>
        /// <param name="userManager">The user manager instance used to create and manage the underlying application user.</param>
        /// <param name="username">The username for the new admin user. Must be unique within the system.</param>
        /// <param name="email">The email address for the new admin user. Must be a valid email format.</param>
        /// <param name="password">The password for the new admin user account. Must meet the password requirements enforced by the user
        /// manager.</param>
        /// <param name="fullName">The full name to associate with the admin user's profile.</param>
        /// <param name="adminRoleType">The administrative role to assign to the new user.</param>
        /// <param name="emailConfirmed">Indicates whether the user's email address should be marked as confirmed. Defaults to <see
        /// langword="true"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see
        /// cref="AdminUser"/> instance with the specified role and profile information.</returns>
        public static async Task<AdminUser> CreateAdminUser(
            UserManager<ApplicationUser> userManager,
            string username,
            string email,
            string password,
            string fullName,
            AdminRoleType adminRoleType,
            bool emailConfirmed = true)
        {
            // 1. Create the ApplicationUser object
            var applicationUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = emailConfirmed,
                FullName = fullName,
                ProfileType = ProfileType.None, // Start with None to avoid premature validation
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            // 2. Create the user in the identity store
            var result = await userManager.CreateAsync(applicationUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user {username}: {errors}");
            }

            // 3. Now create the AdminUser profile and link it to the ApplicationUser
            var adminUser = new AdminUser
            {
                ApplicationUserId = applicationUser.Id,
                Email = email,
                UserName = username,
                Role = adminRoleType,
                ApplicationUser = applicationUser,
                Permissions = GetDefaultPermissionsForRole(adminRoleType)
            };

            // 4. Link the profile to the ApplicationUser
            applicationUser.AdminProfile = adminUser;
            applicationUser.ProfileType = ProfileType.Admin; // Set it after linking the profile
            
            await userManager.AddToRoleAsync(applicationUser, AuthConstants.Roles.SystemAdmin);

            return adminUser;
        }

        /// <summary>
        /// Creates a new customer user account and returns a corresponding Customer entity.
        /// </summary>
        /// <remarks>The created customer user is assigned the 'Customer' role and is marked as active
        /// upon creation. The method does not persist the Customer entity to the database; callers are responsible for
        /// saving it as needed.</remarks>
        /// <param name="userManager">The user manager instance used to create and manage the application user.</param>
        /// <param name="username">The username for the new customer user account. Must be unique within the system.</param>
        /// <param name="email">The email address associated with the new customer user account. Cannot be null or empty.</param>
        /// <param name="password">The password for the new customer user account. Must meet the password requirements defined by the user
        /// manager.</param>
        /// <param name="fullName">The full name of the customer to be associated with the account.</param>
        /// <param name="emailConfirmed">Specifies whether the customer's email address should be marked as confirmed. Defaults to <see
        /// langword="true"/>.</param>
        /// <returns>A Customer entity representing the newly created customer user. The entity includes the associated
        /// application user and registration details.</returns>
        public static async Task<Customer> CreateCustomerUser(
            UserManager<ApplicationUser> userManager,
            string username,
            string email,
            string password,
            string fullName,
            bool emailConfirmed = true)
        {
            // 1. Create the ApplicationUser object but DO NOT save it yet.
            var applicationUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = emailConfirmed,
                FullName = fullName,
                ProfileType = ProfileType.Customer, // Set the profile type
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            // 2. Create the Customer profile object.
            var customer = new Customer
            {
                Name = fullName,
                DateRegistered = DateTime.UtcNow,
                IsActive = true,
                // Link it to the ApplicationUser
                ApplicationUser = applicationUser
            };

            // 3. Associate the profile with the user. This is the crucial step.
            applicationUser.CustomerProfile = customer;

            // 4. Now, create the user. UserManager will call SaveChanges,
            //    and the user will have its CustomerProfile correctly linked.
            var result = await userManager.CreateAsync(applicationUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user {username}: {errors}");
            }

            // 5. Add the user to the appropriate role.
            await userManager.AddToRoleAsync(applicationUser, AuthConstants.Roles.Customer);

            // 6. Return the Customer profile entity, which is now correctly saved.
            return customer;
        }

        
        /// <summary>
        /// Creates a new staff user account and returns a corresponding staff member profile for the specified
        /// restaurant.
        /// </summary>
        /// <remarks>The created staff member will have a temporary employment status and be marked as
        /// active upon creation. The associated user account will be assigned the staff role and linked to the
        /// specified restaurant tenant.</remarks>
        /// <param name="userManager">The user manager instance used to create and manage the application user.</param>
        /// <param name="username">The username for the new staff user account. Must be unique within the system.</param>
        /// <param name="email">The email address associated with the staff user. Cannot be null or empty.</param>
        /// <param name="password">The password for the new staff user account. Must meet the application's password requirements.</param>
        /// <param name="fullName">The full name of the staff member to be associated with the user profile.</param>
        /// <param name="role">The staff role to assign to the new staff member.</param>
        /// <param name="restaurantId">The identifier of the restaurant to associate with the staff member.</param>
        /// <param name="emailConfirmed">Indicates whether the staff user's email address should be marked as confirmed. Defaults to <see
        /// langword="true"/>.</param>
        /// <returns>A <see cref="StaffMember"/> object representing the newly created staff member profile.</returns>
        public static async Task<StaffMember> CreateStaffUser(
            UserManager<ApplicationUser> userManager,
            string username,
            string email,
            string password,
            string fullName,
            StaffRole role,
            int restaurantId,
            bool emailConfirmed = true)
        {
            var applicationUser = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = emailConfirmed,
                FullName = fullName,
                ProfileType = ProfileType.Staff,
                RestaurantTenantId = restaurantId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            };

            var staffMember = new StaffMember
            {
                Name = fullName,
                Role = role,
                RestaurantId = restaurantId,
                HireDate = DateTime.UtcNow,
                IsActive = true,
                EmploymentStatus = EmploymentStatus.Temporary,
                ApplicationUser = applicationUser
            };

            applicationUser.StaffProfile = staffMember;

            var result = await userManager.CreateAsync(applicationUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create staff user '{username}'. Errors: {errors}");
            }

            await userManager.AddToRoleAsync(applicationUser, AuthConstants.Roles.Staff);

            return staffMember;
        }

        /// <summary>
        /// Adds a set of claims to the specified user using the provided user manager.
        /// </summary>
        /// <remarks>Each claim in the collection is added with a default value as defined by the
        /// application. This method does not check for duplicate claims; callers should ensure that claims are not
        /// already present if duplicates are undesirable.</remarks>
        /// <param name="userManager">The user manager instance used to add claims to the user. Cannot be null.</param>
        /// <param name="user">The user to whom the claims will be added. Cannot be null.</param>
        /// <param name="claims">A collection of claim types to add to the user. Each claim will be assigned a default value. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation of adding claims to the user.</returns>
        public static async Task AddUserClaims(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            IEnumerable<string> claims)
        {
            foreach (var claim in claims)
            {
                await userManager.AddClaimAsync(user, new Claim(claim, AuthConstants.ClaimValues.Read));
            }
        }

        /// <summary>
        /// Returns the set of default administrative permissions assigned to the specified role.
        /// </summary>
        /// <remarks>Use this method to determine the baseline permissions granted to a role before any
        /// customizations or overrides are applied.</remarks>
        /// <param name="role">The administrative role for which to retrieve default permissions. Must be a valid value of <see
        /// cref="AdminRoleType"/>.</param>
        /// <returns>An <see cref="AdminPermission"/> value representing the default permissions for the given role. Returns <see
        /// cref="AdminPermission.None"/> if the role is not recognized.</returns>
        private static AdminPermission GetDefaultPermissionsForRole(AdminRoleType role)
        {
            return role switch
            {
                AdminRoleType.SystemAdmin => AdminPermission.All,
                AdminRoleType.RestaurantOwner => AdminPermission.ManageRestaurants | AdminPermission.EditMenus | 
                                 AdminPermission.ManageStaff | AdminPermission.ManagePromotions |
                                 AdminPermission.ViewCustomerData | AdminPermission.ManageOrders |
                                 AdminPermission.ConfigureSettings,
                AdminRoleType.RestaurantManager => AdminPermission.EditMenus | AdminPermission.ManageStaff |
                                   AdminPermission.ManagePromotions | AdminPermission.ViewCustomerData |
                                   AdminPermission.ManageOrders,
                AdminRoleType.RestaurantSupervisor => AdminPermission.ViewReports | AdminPermission.ViewCustomerData |
                                      AdminPermission.ManageOrders,
                _ => AdminPermission.None
            };
        }
    }
}