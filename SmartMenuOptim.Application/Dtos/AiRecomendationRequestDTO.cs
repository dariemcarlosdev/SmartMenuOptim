using System.Collections.Generic;

namespace SmartMenuOptim.Application.Common
{
    /// <summary>
    /// DTO for transferring AI recommendation request data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class AiRecomendationRequestDTO
    {
        public List<ReviewDTO> Reviews { get; set; } = new();
        public List<SaleRecordDTO> SaleRecords { get; set; } = new();
    }
}
