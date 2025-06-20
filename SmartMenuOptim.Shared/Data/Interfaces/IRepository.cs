using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// This method allows LINQ queries on the repository, enabling filtering, sorting, and other LINQ operations directly on the repository.
        /// It is useful for scenarios where you want to perform complex queries without loading all entities into memory first.
        /// </summary>
        /// <returns>returns an IQueryable<T></returns>
        IQueryable<T> Query(); // Add this line to allow LINQ queries on the repository.
        /// <summary>
        /// This method retrieves an entity of type T by its ID asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<T?> GetByIdAsync(int id);
        /// <summary>
        /// This method retrieves all entities of type T from the database asynchronously.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAllAsync();
        /// <summary>
        /// This method adds a new entity of type T to the database asynchronously.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task AddAsync(T entity);
        /// <summary>
        /// This method updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="entity"></param>
        void Update(T entity);
        /// <summary>
        /// This method deletes an entity of type T from the database.
        /// </summary>
        /// <param name="entity"></param>
        void Delete(T entity);
        /// <summary>
        /// This method checks if an entity of type T exists in the database by its ID asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(int id);
    }
}
