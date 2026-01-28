using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Application.Interfaces
{
    /// <summary>
    /// Basic repository interface for CRUD operations and LINQ querying.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IRepository<T> where T : class
    {   
        /// <summary>
        /// Returns a queryable collection of entities of type T for further composition and execution.
        /// </summary>
        /// <remarks>The returned <see cref="IQueryable{T}"/> supports deferred execution. Query operators
        /// are not evaluated until the query is enumerated. The actual results depend on the current state of the data
        /// source at the time of execution.</remarks>
        /// <returns>An <see cref="IQueryable{T}"/> that can be used to compose and execute queries against the underlying data
        /// source.</returns>
        IQueryable<T> Query();
        /// <summary>
        /// Asynchronously retrieves an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to retrieve. Must be greater than zero.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the entity of type T if found;
        /// otherwise, null.</returns>
        Task<T?> GetByIdAsync(int id);
        /// <summary>
        /// Asynchronously retrieves all entities of type <typeparamref name="T"/> from the data source.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of all
        /// entities of type <typeparamref name="T"/>. If no entities are found, the collection will be empty.</returns>
        Task<IEnumerable<T>> GetAllAsync();
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
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the entity
        /// exists; otherwise, <see langword="false"/>.</returns>
        Task<bool> ExistsAsync(int id);
    }
}
