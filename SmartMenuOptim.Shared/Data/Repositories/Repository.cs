using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {

        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        /// <summary>
        /// This method adds a new entity of type T to the database asynchronously.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task AddAsync(T entity) =>
                await _dbSet.AddAsync(entity);

        /// <summary>
        /// This method deletes an entity of type T from the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Delete(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _dbSet.Remove(entity);
        }

        /// <summary>
        /// This method checks if an entity of type T exists in the database by its ID asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<bool> ExistsAsync(int id) =>
            _dbSet.AnyAsync(e => EF.Property<int>(e, "Id") == id);

        /// <summary>
        /// This method retrieves all entities of type T from the database asynchronously.
        /// </summary>
        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.ToListAsync();

        /// <summary>
        /// This method retrieves an entity of type T by its ID asynchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T?> GetByIdAsync(int id) =>
           await _dbSet.FindAsync(id);

        /// <summary>
        /// This method allows LINQ queries on the repository, enabling filtering, sorting, and other LINQ operations directly on the repository.
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// This method updates an existing entity of type T in the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();

        }


    }
}
