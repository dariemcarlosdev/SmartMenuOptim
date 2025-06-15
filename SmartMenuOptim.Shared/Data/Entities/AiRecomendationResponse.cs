using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Data.Entities
{
    public class AiRecomendationResponse
    {
        public List<string> RecomendedDishes { get; set; } = new List<string>();
        public string StrategyNote { get; set; } = string.Empty;
    }
}
