
using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Server.Services
{
    internal class AiImprovementRequest
    {
        [Required]
        public required string DishName { get; set; }
        [Required]
        public int TotalSales { get; set; }
        [Required]
        public double AverageSentiment { get; set; }
        public List<string>? Comments { get; set; }
    }
}