using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Models
{
    public class InsightResponse
    {
        public double ConfidenceScore { get; set; }
        public required string Recomendation { get; set; }
    }
}


