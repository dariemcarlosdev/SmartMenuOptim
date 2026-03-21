using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Aggregates.ReviewAggregate;
using SmartMenuOptim.Domain.Aggregates.SaleRecordAggregate;
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
        public IRepository<Menu> Menus { get; }
        public IRepository<DishCategory> Categories { get; }
        public IRepository<Customer> Customers { get; }
        public IRepository<AdminUser> AdminUsers { get; }
        public IRepository<Restaurant> Restaurants { get; }
        public IRepository<ApplicationUser> ApplicationUsers { get; }
        public IRepository<StaffMember> StaffMembers { get; }
        public IRepository<BusinessRule> BusinessRules { get; }
        public IRepository<Order> Orders { get; }
        public IRepository<OrderStatus> OrderStatuses { get; }

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
            Menus = new Repository<Menu>(_context);
            Categories = new Repository<DishCategory>(_context);
            Customers = new Repository<Customer>(_context);
            AdminUsers = new Repository<AdminUser>(_context);
            Restaurants = new Repository<Restaurant>(_context);
            ApplicationUsers = new Repository<ApplicationUser>(_context);
            StaffMembers = new Repository<StaffMember>(_context);
            BusinessRules = new Repository<BusinessRule>(_context);
            Orders = new Repository<Order>(_context);
            OrderStatuses = new Repository<OrderStatus>(_context);
        }


        /// <summary>
        /// Commits all changes made in the context to the database as a single transaction.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// <para>Call this method after performing create, update, or delete operations to persist changes.</para>
        /// 
        /// <para><strong>Transaction Handling:</strong></para>
        /// <para>If a transaction is already active on the connection (e.g., when called from a domain event
        /// handler dispatched during <c>AppDbContext.SaveChangesAsync</c>), this method participates in the
        /// existing transaction instead of starting a new one. This prevents
        /// <see cref="InvalidOperationException"/> ("The connection is already in a transaction")
        /// while ensuring all changes remain atomic within the outer transaction boundary.</para>
        /// </remarks>
        public async Task<int> SaveChangesAsync()
        {
            // If a transaction is already active (e.g., called from within a domain event handler
            // dispatched by AppDbContext.SaveChangesAsync), participate in the existing transaction
            // instead of starting a new one to avoid nested transaction conflicts.
            if (_context.Database.CurrentTransaction != null)
            {
                return await _context.SaveChangesAsync();
            }

            // No active transaction — wrap in an explicit transaction for atomicity.
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var result = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
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
