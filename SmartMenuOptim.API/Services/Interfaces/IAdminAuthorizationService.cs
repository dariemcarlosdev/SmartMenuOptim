
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using System.Threading.Tasks;

///<summary>
/// Service interface for admin authorization related to managing staff schedules.
/// Small DI-friendly authorization service (API layer) that loads necessary data and delegates to the entity method:
/// </summary>
/// 
namespace SmartMenuOptim.API.Services;
public interface IAdminAuthorizationService
{
    Task<bool> CanManageScheduleAsync(int adminUserId, int restaurantId);
    Task<bool> CanManageScheduleAsync(AdminUser? admin, int restaurantId);
}