using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using System;
using System.Threading.Tasks;

namespace SmartMenuOptim.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Clean Architecture compliant Unit of Work implementation coordinating repository operations 
    /// and managing a single database context instance.
    /// Ensures all changes are committed as a single transaction.
    /// </summary>
    public class UnityOfWork : IUnityOfWork
    {
        private readonly AppDbContext _context;

        public IRepository<SaleRecord> SaleRecords { get; }
        public IRepository<Review> Reviews { get; }
        public IRepository<Dish> Dishes { get; }
        public IRepository<Category> Categories { get; }
        public IRepository<Customer> Customers { get; }
        public IRepository<AdminUser> AdminUsers { get; }
        public IRepository<Restaurant> Restaurants { get; }
        public IRepository<ApplicationUser> ApplicationUsers { get; }
        public IRepository<StaffMember> StaffMembers { get; }
        public IRepository<BusinessRule> BusinessRules { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityOfWork"/> class with the provided database context.
        /// </summary>
        /// <param name="context">The database context to use for all repositories.</param>
        /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
        public UnityOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            
            SaleRecords = new Repository<SaleRecord>(_context);
            Reviews = new Repository<Review>(_context);
            Dishes = new Repository<Dish>(_context);
            Categories = new Repository<Category>(_context);
            Customers = new Repository<Customer>(_context);
            AdminUsers = new Repository<AdminUser>(_context);
            Restaurants = new Repository<Restaurant>(_context);
            ApplicationUsers = new Repository<ApplicationUser>(_context);
            StaffMembers = new Repository<StaffMember>(_context);
            BusinessRules = new Repository<BusinessRule>(_context);
        }


        /// <summary>
        /// Commits all changes made in the context to the database as a single transaction.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// Call this method after performing create, update, or delete operations to persist changes.
        /// </remarks>
        public async Task<int> SaveChangesAsync(){

            // Here we can implement transaction management if needed.
            // Transaction ensures all operations either succeed or fail together. It is useful when multiple related changes must be atomic and maintain data integrity.
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var result = await _context.SaveChangesAsync();
                    await transaction.CommitAsync(); // CommitAsync ensures all changes are saved to the database.
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(); // RollbackAsync reverts all changes if an error occurs, maintaining data integrity.
                    throw;
                }
            }
        }

        /// <summary>
        /// Disposes the database context and releases all resources.
        /// </summary>
        /// <remarks>
        /// Always dispose the unit of work when done to avoid memory leaks and ensure proper resource cleanup.
        /// </remarks>
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
