using System.Linq.Expressions;

namespace SmartMenuOptim.Shared.Data.Interfaces
{
    /// <summary>
    /// Extended repository interface supporting type-safe navigation property includes for advanced querying. This implement interface extension pattern allows for more complex queries while maintaining type safety.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IRepositoryWithIncludes<T> : IRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves all entities of type T from the database, with optional navigation property includes.
        /// </summary>
        /// <param name="includes">Navigation property expressions to include (optional).</param>
        /// <returns>A list of all entities of type T.</returns>
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Retrieves an entity of type T by its primary key, with optional navigation property includes.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        /// <param name="includes">Navigation property expressions to include (optional).</param>
        /// <returns>The entity if found, otherwise null.</returns>
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
    }
}
