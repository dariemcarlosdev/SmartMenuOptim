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

        Task<int> SaveChangesAsync();
    }
}
