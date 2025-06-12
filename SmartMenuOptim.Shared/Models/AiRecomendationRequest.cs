using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Shared.Models
{
    public class AiRecomendationRequest
    {
        public List<Review> reviews { get; set; } = [];
        public List<SaleRecord> saleRecords { get; set; } = [];
    }
}
