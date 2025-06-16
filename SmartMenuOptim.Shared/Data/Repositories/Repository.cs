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

        public async Task AddAsync(T entity) =>
                await _dbSet.AddAsync(entity);


        public void Delete(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            _dbSet.Remove(entity);
        }


        public Task<bool> ExistsAsync(int id) =>
            _dbSet.AnyAsync(e => EF.Property<int>(e, "Id") == id);


        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.ToListAsync();


        public async Task<T?> GetByIdAsync(int id) =>
           await _dbSet.FindAsync(id).AsTask();


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
