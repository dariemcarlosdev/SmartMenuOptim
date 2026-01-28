using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMenuOptim.Application.Common
{
    // Create CategoryGroupDTOa DTO for category grouping
    public class CategoryGroupDTO
    {
        public required string CategoryName { get; set; }
        public required List<SaleRecordDTO> Records { get; set; }
    }
}
