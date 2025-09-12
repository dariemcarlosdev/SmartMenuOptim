using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Shared.Data
{
    public class MockDataStore
    {
        public static List<SaleRecord> GetSaleRecords() => new List<SaleRecord>
        {
            new() { DishId = 1, QuantitySold = 10, SaleDate = DateTime.Now.AddDays(-1) },
            new() { DishId = 2, QuantitySold = 5, SaleDate = DateTime.Now.AddDays(-2) },
            new() { DishId = 3, QuantitySold = 8, SaleDate = DateTime.Now.AddDays(-3) }
        };
    }
}
