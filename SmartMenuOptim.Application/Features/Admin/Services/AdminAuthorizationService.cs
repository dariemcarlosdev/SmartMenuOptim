using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services.Abstractions;

namespace SmartMenuOptim.Application.Features.Admin.Services;

/// <summary>
/// Implementation of admin authorization business rules.
/// </summary>
/// <remarks>
/// This service implements authorization logic for administrative operations.
/// It has been moved to the Application layer following Clean Architecture principles:
/// - Uses IRepository pattern instead of direct DbContext access
/// - Implements domain interface (IAdminAuthorizationService)
/// - Contains application-level authorization orchestration logic
/// </remarks>
public class AdminAuthorizationService : IAdminAuthorizationService
{
    private readonly IRepository<AdminUser> _adminUserRepository;
    
    public AdminAuthorizationService(IRepository<AdminUser> adminUserRepository)
    {
        _adminUserRepository = adminUserRepository;
    }
    
    /// <inheritdoc/>
    public async Task<bool> CanManageScheduleAsync(int adminUserId, int restaurantId)
    {
        var admin = await _adminUserRepository
            .Query()
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
    private bool CanManageInternal(AdminUser? admin, int restaurantId)
    {
        if (admin == null) return false;
        
        // Prefer entity logic on AdminUser if available:
        if (admin.CanManageStaffSchedules(restaurantId))
            return true;
        
        // fallback: check flags/role explicitly if needed
        return false;
    }
}
