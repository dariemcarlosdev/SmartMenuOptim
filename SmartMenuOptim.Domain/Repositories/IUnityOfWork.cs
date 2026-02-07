using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using System.Threading.Tasks;

namespace SmartMenuOptim.Domain.Repositories
{
    /// <summary>
    /// Unit of Work pattern interface coordinating repository operations within a single transaction boundary.
    /// Clean Architecture compliant - uses infrastructure-agnostic IRepository interface.
    /// </summary>
    public interface IUnityOfWork
    {
        /// <summary>
        /// Repository for sales transaction records.
        /// </summary>
        IRepository<SaleRecord> SaleRecords { get; }

        /// <summary>
        /// Repository for customer reviews.
        /// </summary>
        IRepository<Review> Reviews { get; }

        /// <summary>
        /// Repository for menu dishes (aggregate root).
        /// </summary>
        IRepository<Dish> Dishes { get; }

        /// <summary>
        /// Repository for dish categories.
        /// </summary>
        IRepository<DishCategory> Categories { get; }

        /// <summary>
        /// Repository for customer profiles.
        /// </summary>
        IRepository<Customer> Customers { get; }

        /// <summary>
        /// Repository for administrative users.
        /// </summary>
        IRepository<AdminUser> AdminUsers { get; }

        /// <summary>
        /// Repository for restaurants (aggregate root, multi-tenancy).
        /// </summary>
        IRepository<Restaurant> Restaurants { get; }

        /// <summary>
        /// Repository for shared application user data.
        /// </summary>
        IRepository<ApplicationUser> ApplicationUsers { get; }

        /// <summary>
        /// Repository for staff member profiles.
        /// </summary>
        IRepository<StaffMember> StaffMembers { get; }

        /// <summary>
        /// Repository for business rules and configurations.
        /// </summary>
        IRepository<BusinessRule> BusinessRules { get; }

        /// <summary>
        /// Asynchronously commits all pending changes to the database as a single transaction.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous save operation. 
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        Task<int> SaveChangesAsync();
    }
}
