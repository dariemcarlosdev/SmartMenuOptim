using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.TenantSpecificEntities;
using System.Threading.Tasks;

namespace SmartMenuOptim.Application.Interfaces
{
    public interface IUnityOfWork
    {
        IRepositoryWithIncludes<SaleRecord> SaleRecords { get; }
        IRepositoryWithIncludes<Review> Reviews { get; }
        IRepositoryWithIncludes<Dish> Dishes { get; } // Add Dishes repository for lookup
        IRepositoryWithIncludes<Category> Categories { get; } // Add Categories repository for lookup
        IRepositoryWithIncludes<Customer> Customers { get; } // Add Customers repository for authentication/profile
        IRepositoryWithIncludes<AdminUser> AdminUsers { get; } // Add AdminUsers repository for admin logic
        IRepositoryWithIncludes<Restaurant> Restaurants { get; } // Add Restaurants repository for multi-tenancy
        IRepositoryWithIncludes<ApplicationUser> ApplicationUsers { get; } // Add ApplicationUsers repository for shared user data
        IRepositoryWithIncludes<StaffMember> UserProfiles { get; } // Add UserProfiles repository for user profile data
        IRepositoryWithIncludes<BusinessRule> BussinessRules { get; } // Add BusinessRules repository for business logic
        IRepositoryWithIncludes<Customer> Customer { get; } // Add Customer repository for customer data
        Task<int> SaveChangesAsync();
    }
}
