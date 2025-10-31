using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;

namespace SmartMenuOptim.Shared.Extensions;

/*
 Placement rationale / Reference & Next steps

 Why here
 • Shared project holds domain entities (AdminUser, AdminPermission) — placing extensions alongside domain code keeps concerns together.
 • Makes methods available to API, Blazor, background jobs and tests without duplicating code.
 • Avoid putting domain helpers in the API project (that would couple domain logic to the web layer).

 Next steps (optional)
 • If you prefer grouping by feature, you can instead create `SmartMenuOptim.Shared\Data\Extensions\AdminPermissionExtensions.cs`
   and use namespace `SmartMenuOptim.Shared.Data.Extensions`.
*/

/// <summary>
/// Domain-level helper extension methods for checking <see cref="AdminPermission"/> on <see cref="AdminUser"/>.
/// Placed in the Shared project so API, Blazor client and other layers can reuse them.
/// </summary>
public static partial class AdminPermissionExtensions
{
    /// <summary>
    /// Returns true if the admin has the specified permission flag.
    /// Null-safe.
    /// </summary>
    public static bool HasPermission(this AdminUser? admin, AdminPermission permission)
        => admin is not null && admin.Permissions.HasFlag(permission);


    /// <summary>
    /// Returns true if the admin has all specified permissions.
    /// Null-safe.
    /// </summary>
    public static bool HasAllPermissions(this AdminUser? admin, AdminPermission permissions)
        => admin is not null && (admin.Permissions & permissions) == permissions;

    /// <summary>
    /// Returns true if the admin has any of the specified permissions.
    /// Null-safe.
    /// </summary>
    public static bool HasAnyPermission(this AdminUser? admin, AdminPermission permissions)
        => admin is not null && (admin.Permissions & permissions) != AdminPermission.None;

    /// <summary>
    /// This is an extension method on AdminRole enum to get default permissions. Implemented as an extension method type for consistency with other permission-related helpers.
    /// As parameter it takes AdminRole enum value.This keyword 'this' allows calling it as an extension method on AdminRole instances.
    /// Returns default permission flags for a given role.
    /// Pure mapping — suitable as a shared helper used by seeders and role setup code.
    /// </summary>
    /// <param name="role">The admin role.</param>
    /// <returns>The default permissions for the role.</returns>

    /// GetDefaultPermissionsForRole() is pure, stateless, and conceptually a permission mapping.
    /// Putting it in SmartMenuOptim.Shared.Extensions (or a SmartMenuOptim.Shared.Data.Extensions feature folder) keeps permission-related utilities together and re-usable across API, Blazor, tests, seeders.
    public static AdminPermission GetDefaultPermissionsForRole(this AdminRole role)
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
}