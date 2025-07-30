using System;

namespace SmartMenuOptim.Shared.Data.DTOs
{
    /// <summary>
    /// DTO for transferring sale record data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class SaleRecordDTO
    {
        public int Id { get; set; }
        public int DishId { get; set; }
        public string? DishName { get; set; } // For UI display
        public int QuantitySold { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
