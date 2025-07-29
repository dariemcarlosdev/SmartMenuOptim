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
        IRepositoryWithIncludes<SaleRecord> SaleRecords { get; }
        IRepositoryWithIncludes<Review> Reviews { get; }
        IRepositoryWithIncludes<Dish> Dishes { get; } // Add Dishes repository for lookup
        IRepositoryWithIncludes<Category> Categories { get; } // Add Categories repository for lookup
        IRepositoryWithIncludes<Customer> Customers { get; } // Add Customers repository for authentication/profile
        IRepositoryWithIncludes<AdminUser> AdminUsers { get; } // Add AdminUsers repository for admin logic
        Task<int> SaveChangesAsync();
    }
}
