using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Dtos
{
    public class UnderperformingDishDTO
    {
        public string DishName { get; init; } = string.Empty;
        public int TotalSales { get; init; } = 0;
        public double AverageSentiment { get; init; } = 0.0;
        public string? SuggestedAction { get; init; } = null;
    }
}
