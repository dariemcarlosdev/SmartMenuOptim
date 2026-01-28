using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Application.Common
{
    public class UnderperformingDishDTO
    {
        public int DishId { get; init; } // Add DishId for lookup
        public string DishName { get; init; } = string.Empty;
        public int TotalSales { get; init; } = 0;
        public double AverageSentiment { get; init; } = 0.0; // Average sentiment score based on reviews
        public int TotalReviews { get; init; } = 0; // Total number of reviews received
        public decimal AverageRating { get; init; } = 0.0m; // Average rating based on reviews
        public List<string> Comments { get; set; } = new List<string>();
    }
}
