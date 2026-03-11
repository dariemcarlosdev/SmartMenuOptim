using SmartMenuOptim.Domain.Entities.ProfileEntities;

namespace SmartMenuOptim.Domain.Services.Abstractions;

/// <summary>
/// Domain service abstraction for admin authorization business rules.
/// This is a PORT in Hexagonal Architecture.
/// </summary>
/// <remarks>
/// <para><strong>Hexagonal Architecture (Ports & Adapters)</strong></para>
/// 
/// This interface represents domain-level authorization decisions,
/// not infrastructure concerns like JWT tokens or claims.
/// 
/// <para><strong>Business Rules Enforced:</strong></para>
/// <list type="bullet">
///   <item><description>Admins can only manage schedules for restaurants they're assigned to</description></item>
///   <item><description>Multi-restaurant admins have specific permission scopes</description></item>
///   <item><description>System admins have global permissions</description></item>
/// </list>
/// 
/// <para><strong>Implementation Checks:</strong></para>
/// <list type="bullet">
///   <item><description>AdminUser.AssignedRestaurants</description></item>
///   <item><description>AdminUser.IsSuperAdmin</description></item>
///   <item><description>Domain permission rules</description></item>
/// </list>
/// </remarks>
public interface IAdminAuthorizationService
{
    /// <summary>
    /// Checks if an admin user can manage schedules for a specific restaurant.
    /// Business rule: Admin must be assigned to the restaurant or be a super admin.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user.</param>
    /// <param name="restaurantId">The ID of the restaurant.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    Task<bool> CanManageScheduleAsync(int adminUserId, int restaurantId);

    /// <summary>
    /// Checks if an admin user entity can manage schedules for a specific restaurant.
    /// Business rule: Admin must be assigned to the restaurant or be a super admin.
    /// </summary>
    /// <param name="admin">The admin user entity (can be null).</param>
    /// <param name="restaurantId">The ID of the restaurant.</param>
    /// <returns>True if authorized, false otherwise.</returns>
    Task<bool> CanManageScheduleAsync(AdminUser? admin, int restaurantId);
}
