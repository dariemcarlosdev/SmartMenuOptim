using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Specifications;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMenuOptim.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Clean Architecture compliant generic repository implementation using Entity Framework Core.
    /// Implements specification pattern for domain-centric querying without infrastructure coupling.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class Repository<T> : IRepository<T> where T : class
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
        /// Asynchronously retrieves all entities of type T from the database.
        /// </summary>
        /// <returns>A list of all entities of type T.</returns>
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <returns>The entity if found, otherwise null.</returns>
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Asynchronously retrieves entities matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering, ordering, includes, and pagination.</param>
        /// <returns>A collection of entities matching the specification criteria.</returns>
        /// <remarks>
        /// This method translates domain specifications into EF Core queries.
        /// </remarks>
        public async Task<IEnumerable<T>> FindAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves a single entity matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering and includes.</param>
        /// <returns>The first entity matching the specification, or null if no match is found.</returns>
        public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Asynchronously counts entities matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering criteria.</param>
        /// <returns>The count of entities matching the specification.</returns>
        public async Task<int> CountAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.CountAsync();
        }

        /// <summary>
        /// Applies a specification to the queryable, translating domain logic into EF Core queries.
        /// </summary>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>A queryable with the specification applied.</returns>
        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            IQueryable<T> query = _dbSet;

            // Apply filtering criteria
            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            // Apply includes (EF Core specific - this is where domain abstraction is translated)
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
            query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            // Apply ordering
            if (spec.OrderBy != null)
            {
                query = query.OrderBy(spec.OrderBy);
            }
            else if (spec.OrderByDescending != null)
            {
                query = query.OrderByDescending(spec.OrderByDescending);
            }

            // Apply paging
            if (spec.IsPagingEnabled)
            {
                if (spec.Skip.HasValue)
                {
                    query = query.Skip(spec.Skip.Value);
                }
                if (spec.Take.HasValue)
                {
                    query = query.Take(spec.Take.Value);
                }
            }

            // Apply no-tracking for read-only queries (optimization)
            query = query.AsNoTracking();

            return query;
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
