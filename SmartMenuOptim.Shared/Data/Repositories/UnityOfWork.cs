using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;
using SmartMenuOptim.Shared.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Repositories
{
    /// <summary>
    /// Implements the Unit of Work pattern to coordinate repository operations and manage a single database context instance.
    /// Provides access to repositories for all major entities and ensures all changes are committed as a single transaction.
    /// </summary>
    /// <remarks>
    /// This class centralizes repository access and transaction management, promoting modularity and testability.
    /// All repositories are exposed as <see cref="IRepositoryWithIncludes{T}"/> for advanced querying.
    /// </remarks>
    public class UnityOfWork : IUnityOfWork
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Repository for <see cref="SaleRecord"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all sales record data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<SaleRecord> SaleRecords { get; }

        /// <summary>
        /// Repository for <see cref="Review"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all review data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<Review> Reviews { get; }

        /// <summary>
        /// Repository for <see cref="Dish"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all dish data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<Dish> Dishes { get; }

        /// <summary>
        /// Repository for <see cref="Category"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all category data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<Category> Categories { get; }

        /// <summary>
        /// Repository for <see cref="Customer"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all customer data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<Customer> Customers { get; }

        /// <summary>
        /// Repository for <see cref="AdminUser"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all admin user data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<AdminUser> AdminUsers { get; }

        /// <summary>
        /// Repository for <see cref="Restaurant"/> entities, supporting advanced querying and includes.
        /// </summary>
        /// <remarks>
        /// Use this repository for all restaurant data access and queries.
        /// </remarks>
        public IRepositoryWithIncludes<Restaurant> Restaurants { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityOfWork"/> class with the provided database context.
        /// </summary>
        /// <param name="context">The database context to use for all repositories.</param>
        /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
        public UnityOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            SaleRecords = new Repository<SaleRecord>(_context); // Add SaleRecords repository
            Reviews = new Repository<Review>(_context); // Add Review repository
            Dishes = new Repository<Dish>(_context); // Add Diwsh repository
            Categories = new Repository<Category>(_context); // Add Categories repository
            Customers = new Repository<Customer>(_context); // Add Customers repository
            AdminUsers = new Repository<AdminUser>(_context); // Add AdminUsers repository
            Restaurants = new Repository<Restaurant>(_context); // Add Restaurants repository
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
