using SmartMenuOptim.Domain.Specifications;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartMenuOptim.Domain.Repositories
{
    /// <summary>
    /// Clean Architecture compliant repository interface for CRUD operations and specification-based querying.
    /// This interface is infrastructure-agnostic and resides in the Domain layer.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IRepository<T> where T : class
    {   
        /// <summary>
        /// Returns a queryable collection of entities of type T for further composition and execution.
        /// </summary>
        /// <remarks>
        /// The returned <see cref="IQueryable{T}"/> supports deferred execution. Query operators
        /// are not evaluated until the query is enumerated. The actual results depend on the current state of the data
        /// source at the time of execution.
        /// <para>
        /// ⚠️ Use with caution: Direct IQueryable exposure can leak infrastructure concerns. 
        /// Prefer using specifications for complex queries when possible.
        /// </para>
        /// </remarks>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that can be used to compose and execute queries against the underlying data source.
        /// </returns>
        IQueryable<T> Query();

        /// <summary>
        /// Asynchronously retrieves an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to retrieve. Must be greater than zero.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the entity of type T if found;
        /// otherwise, null.
        /// </returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Asynchronously retrieves all entities of type <typeparamref name="T"/> from the data source.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an enumerable collection of all
        /// entities of type <typeparamref name="T"/>. If no entities are found, the collection will be empty.
        /// </returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Asynchronously retrieves entities matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering, ordering, includes, and pagination.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a collection of entities
        /// matching the specification criteria.
        /// </returns>
        /// <remarks>
        /// ✅ CLEAN ARCHITECTURE: This method allows domain-centric query logic without coupling to infrastructure.
        /// Use specifications to encapsulate complex query logic in a testable, reusable way.
        /// </remarks>
        Task<IEnumerable<T>> FindAsync(ISpecification<T> spec);

        /// <summary>
        /// Asynchronously retrieves a single entity matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering and includes.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first entity
        /// matching the specification, or null if no match is found.
        /// </returns>
        Task<T?> FirstOrDefaultAsync(ISpecification<T> spec);

        /// <summary>
        /// Asynchronously counts entities matching the given specification.
        /// </summary>
        /// <param name="spec">The specification defining filtering criteria.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the count of entities
        /// matching the specification.
        /// </returns>
        Task<int> CountAsync(ISpecification<T> spec);

        /// <summary>
        /// Asynchronously adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AddAsync(T entity);

        /// <summary>
        /// Updates the specified entity in the data store.
        /// </summary>
        /// <param name="entity">The entity to update. Cannot be null.</param>
        void Update(T entity);

        /// <summary>
        /// Deletes the specified entity from the data store.
        /// </summary>
        /// <param name="entity">The entity to be deleted. Cannot be null.</param>
        void Delete(T entity);

        /// <summary>
        /// Asynchronously determines whether an entity with the specified identifier exists.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to check for existence.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if the entity
        /// exists; otherwise, <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsAsync(int id);
    }
}
