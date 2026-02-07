using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Services.Abstraction;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using SmartMenuOptim.Domain.Entities.ProfileEntities;

namespace SmartMenuOptim.API.Services;

/// <summary>
/// Implementation of admin authorization business rules.
/// </summary>
/// <remarks>
/// ⚠️ NOTE: This service is currently in the API layer but should be moved to Domain.Services
/// as it contains business logic for authorization rules.
/// 
/// Current location violates Clean Architecture:
/// - Business rules should be in Domain layer
/// - This service directly depends on AppDbContext (Infrastructure)
/// 
/// Recommended refactoring:
/// 1. Move to Domain.Services as a proper domain service
/// 2. Use IRepository<AdminUser> instead of AppDbContext
/// 3. Keep this class in API only if it needs HttpContext for claims
/// 
/// For now, this implements the domain interface to maintain DIP.
/// </remarks>
public class AdminAuthorizationService : IAdminAuthorizationService
{
    readonly AppDbContext _db;
    public AdminAuthorizationService(AppDbContext db) => _db = db;
    
    /// <inheritdoc/>
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