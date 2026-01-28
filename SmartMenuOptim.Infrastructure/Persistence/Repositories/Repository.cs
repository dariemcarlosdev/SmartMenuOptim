using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Application.Interfaces;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SmartMenuOptim.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Generic repository implementation for CRUD operations and flexible querying using Entity Framework Core.
    /// Supports type-safe navigation property includes and dynamic primary key resolution.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class Repository<T> : IRepositoryWithIncludes<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{T}"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// Asynchronously adds a new entity of type T to the database.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
        /// <remarks>
        /// The entity will be tracked by the context and inserted on SaveChanges.
        /// </remarks>
        public async Task AddAsync(T entity) =>
                await _dbSet.AddAsync(entity);

        /// <summary>
        /// Removes an entity of type T from the database.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
        /// <remarks>
        /// The entity will be deleted on SaveChanges. If the entity is not tracked, it will be attached and then removed.
        /// </remarks>
        public void Delete(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _dbSet.Remove(entity);
        }

        /// <summary>
        /// Checks if an entity of type T exists in the database by its primary key value.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>True if the entity exists, otherwise false.</returns>
        /// <remarks>
        /// This method dynamically resolves the primary key property name using EF Core metadata.
        /// It is robust to changes in the primary key property name.
        /// </remarks>
        public Task<bool> ExistsAsync(int id)
        {
            var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties
                .Select(x => x.Name).Single();
            return _dbSet.AnyAsync(e => EF.Property<int>(e, keyName!) == id);
        }

        /// <summary>
        /// Retrieves all entities of type T from the database, with optional navigation property includes.
        /// </summary>
        /// <param name="includes">Navigation property expressions to include (optional).</param>
        /// <returns>A list of all entities of type T.</returns>
        /// <remarks>
        /// Use this overload to eagerly load related data via navigation properties in a type-safe manner.
        /// </remarks>
        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            if (includes != null && includes.Length > 0)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
                // Apply AsNoTracking once after all includes for read-only scenarios. This saves memory and improves performance.
                query = query.AsNoTracking();
            }
            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves an entity of type T by its primary key, with optional navigation property includes.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="includes">Navigation property expressions to include (optional).</param>
        /// <returns>The entity if found, otherwise null.</returns>
        /// <remarks>
        /// <para>This method optimizes the query based on whether includes are provided:</para>
        /// <list type="bullet">
        /// <item><description>If no includes: Uses <see cref="DbSet{T}.FindAsync"/> for fast primary key lookup with change tracking.</description></item>
        /// <item><description>If includes provided: Uses eager loading with <see cref="EntityFrameworkQueryableExtensions.AsNoTracking"/> for read-only scenarios.</description></item>
        /// </list>
        /// <para>The primary key property name is dynamically resolved using EF Core metadata, making this method robust to schema changes.</para>
        /// </remarks>
        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            // Optimization: If no includes, use FindAsync for better performance
            if (includes == null || includes.Length == 0)
            {
                return await _dbSet.FindAsync(id);
            }

            // Build query with includes
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            
            // Apply AsNoTracking once after all includes for read-only scenarios. This saves memory and improves performance.
            query = query.AsNoTracking();

            // Resolve primary key name dynamically and execute query
            var keyName = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties
                .Select(x => x.Name).Single();
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, keyName!) == id);
        }

        /// <summary>
        /// Retrieves all entities of type T from the database asynchronously (interface method).
        /// </summary>
        /// <returns>A list of all entities of type T.</returns>
        /// <remarks>
        /// This method does not include navigation properties. Use the overload with includes for eager loading.
        /// </remarks>
        async Task<IEnumerable<T>> IRepository<T>.GetAllAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// Retrieves an entity of type T by its primary key asynchronously (explicit interface implementation).
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        /// <remarks>
        /// <para><strong>Note:</strong> This is an explicit interface implementation for <see cref="IRepository{T}.GetByIdAsync(int)"/>.</para>
        /// <para>When using <see cref="IRepositoryWithIncludes{T}"/>, call the public <see cref="GetByIdAsync(int, Expression{Func{T, object}}[])"/> method instead, 
        /// which supports optional includes and automatically uses <see cref="DbSet{T}.FindAsync"/> when no includes are provided.</para>
        /// <para>This method uses <see cref="DbSet{T}.FindAsync"/> for efficient primary key lookup with change tracking.</para>
        /// </remarks>
        async Task<T?> IRepository<T>.GetByIdAsync(int id)
        {
            // Delegate to the public method without includes for consistency
            return await GetByIdAsync(id);
        }

        /// <summary>
        /// Returns an IQueryable for the entity type, enabling LINQ queries, filtering, and sorting and including navigation properties as needed.
        /// </summary>
        /// <returns>An IQueryable for the entity type.</returns>
        /// <remarks>
        /// This method does not include navigation properties by default. Use .Include() in your LINQ queries as needed.
        /// </remarks>
        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// Updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
        /// <remarks>
        /// This method only updates the main entity. If you need to update related (navigation) entities,
        /// you must explicitly set their state to Modified in your service or business logic layer, e.g.:
        /// <code>
        /// _context.Entry(entity.RelatedEntity).State = EntityState.Modified;
        /// </code>
        /// The generic repository does not automatically update related entities to avoid unintended data changes.
        /// </remarks>
        public void Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _context.Entry(entity).State = EntityState.Modified;
            /*_context.SaveChanges();*/ // ❌ This breaks the Unit of Work pattern. This bypasses any transaction management in the service layer and commit all changes immediately, breaking atomicity transactions and UoW pattern.
        }
    }
}
