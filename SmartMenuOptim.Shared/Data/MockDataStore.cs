using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartMenuOptim.Shared.Models;
using SmartMenuOptim.Shared.Models.Dtos;

namespace SmartMenuOptim.Shared.Data
{
    public class MockDataStore
    {
        
        public static List<SaleRecord> GetSaleRecords() => new List<SaleRecord>
            {
                new() { DishName = "Ribeye Steak", QuantitySold = 10, SaleDate = DateTime.Now.AddDays(-1) },
                new() { DishName = "Grilled Chicken", QuantitySold = 5, SaleDate = DateTime.Now.AddDays(-2) },
                new() { DishName = "Empanadas", QuantitySold = 8, SaleDate = DateTime.Now.AddDays(-3) }
            };
    }
}
