using SmartMenuOptim.Shared.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Interfaces
{
    public interface IUnityOfWork
    {
        IRepository<SaleRecord> SaleRecords { get; }
        IRepository<Review> Reviews { get; }
        IRepository<Dish> Dishes { get; } // Add Dishes repository for lookup
        IRepository<Category> Categories { get; } // Add Categories repository for lookup
        IRepository<Customer> Customers { get; } // Add Customers repository for authentication/profile
        IRepository<AdminUser> AdminUsers { get; } // Add AdminUsers repository for admin logic
        Task<int> SaveChangesAsync();
    }
}
