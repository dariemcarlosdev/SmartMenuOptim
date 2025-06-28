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
        }

        public IRepository<SaleRecord> SaleRecords { get; }

        public IRepository<Review> Reviews { get; }

        public async Task<int> SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public void Dispose()
        {
            _context?.Dispose();
        }

    }
}
