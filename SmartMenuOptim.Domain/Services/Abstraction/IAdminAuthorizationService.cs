using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;

namespace SmartMenuOptim.Domain.Services.Abstraction;

/// <summary>
/// Domain service abstraction for admin authorization business rules.
/// Defines authorization policies based on domain business logic.
/// Based on Hexagonal Architecture principles, this is a PORT interface.( an interface that defines a contract for domain services)
/// </summary>
/// <remarks>
/// This interface represents domain-level authorization decisions,
/// not infrastructure concerns like JWT tokens or claims.
/// 
/// Business rules it enforces:
/// - Admins can only manage schedules for restaurants they're assigned to
/// - Multi-restaurant admins have specific permission scopes
/// - System admins have global permissions
/// 
/// Implementation can check:
/// - AdminUser.AssignedRestaurants
/// - AdminUser.IsSuperAdmin
/// - Domain permission rules
/// </remarks>
public interface IAdminAuthorizationService
{
    /// <summary>
    /// Checks if an admin user can manage schedules for a specific restaurant.
    /// Business rule: Admin must be assigned to the restaurant or be a super admin.
    /// </summary>
    /// <param name="adminUserId">The ID of the admin user</param>
    /// <param name="restaurantId">The ID of the restaurant</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> CanManageScheduleAsync(int adminUserId, int restaurantId);
    
    /// <summary>
    /// Checks if an admin user entity can manage schedules for a specific restaurant.
    /// Business rule: Admin must be assigned to the restaurant or be a super admin.
    /// </summary>
    /// <param name="admin">The admin user entity (can be null)</param>
    /// <param name="restaurantId">The ID of the restaurant</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> CanManageScheduleAsync(AdminUser? admin, int restaurantId);
}
