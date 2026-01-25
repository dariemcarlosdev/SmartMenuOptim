using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Infrastructure.Persistence.Context;
namespace SmartMenuOptim.API.Services;

/// <summary>
/// Provides authorization services for admin users, enabling permission checks for managing schedules and internal
/// operations of restaurants.
/// </summary>
/// <remarks>This service offers methods to verify whether an admin user has the necessary permissions to perform
/// management tasks for a specific restaurant. It is typically used to enforce access control in administrative
/// workflows.</remarks>
public class AdminAuthorizationService : IAdminAuthorizationService
{
    readonly AppDbContext _db;
    public AdminAuthorizationService(AppDbContext db) => _db = db;
    
    /// <summary>
    /// Determines asynchronously whether the specified admin user has permission to manage the schedule for a given
    /// restaurant.
    /// </summary>
    /// <param name="adminUserId">The unique identifier of the admin user whose permissions are being checked. Must correspond to a valid admin
    /// user.</param>
    /// <param name="restaurantId">The unique identifier of the restaurant for which schedule management permissions are being verified.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the admin user
    /// can manage the schedule for the specified restaurant; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> CanManageScheduleAsync(int adminUserId, int restaurantId)
    {
        var admin = await _db.AdminUsers
            .Include(u => u.OwnedRestaurants)
            .FirstOrDefaultAsync(u => u.Id == adminUserId);
        return CanManageInternal(admin, restaurantId);
    }

    public Task<bool> CanManageScheduleAsync(AdminUser? admin, int restaurantId)
        => Task.FromResult(CanManageInternal(admin, restaurantId));

    /// <summary>
    /// Determines whether the specified admin user has permission to manage internal operations for the given
    /// restaurant.
    /// </summary>
    /// <remarks>This method checks whether the admin user is authorized to manage staff schedules or other
    /// internal functions for the specified restaurant. If the admin user is null, the method returns false.</remarks>
    /// <param name="admin">The admin user whose permissions are to be evaluated. Can be null.</param>
    /// <param name="restaurantId">The unique identifier of the restaurant for which permissions are being checked.</param>
    /// <returns>true if the admin user can manage internal operations for the specified restaurant; otherwise, false.</returns>
    bool CanManageInternal(AdminUser? admin, int restaurantId)
    {
        if (admin == null) return false;
        // Prefer entity logic on AdminUser if available:
        if (admin.CanManageStaffSchedules(restaurantId))
            return true;
        // fallback: check flags/role explicitly if needed
        return false;
    }
}