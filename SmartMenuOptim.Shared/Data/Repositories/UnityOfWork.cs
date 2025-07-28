using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Repositories
{
    public class UnityOfWork : IUnityOfWork
    {
        private readonly AppDbContext _context;

        public UnityOfWork(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            SaleRecords = new Repository<SaleRecord>(_context);
            Reviews = new Repository<Review>(_context);
            Dishes = new Repository<Dish>(_context);
            Categories = new Repository<Category>(_context); // Add Categories repository
            Customers = new Repository<Customer>(_context); // Add Customers repository
            AdminUsers = new Repository<AdminUser>(_context); // Add AdminUsers repository
        }

        public IRepository<SaleRecord> SaleRecords { get; }
        public IRepository<Review> Reviews { get; }
        public IRepository<Dish> Dishes { get; }
        public IRepository<Category> Categories { get; } // Implement Categories repository
        public IRepository<Customer> Customers { get; } // Implement Customers repository
        public IRepository<AdminUser> AdminUsers { get; } // Implement AdminUsers repository

        public async Task<int> SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public void Dispose()
        {
            _context?.Dispose();
        }

    }
}
