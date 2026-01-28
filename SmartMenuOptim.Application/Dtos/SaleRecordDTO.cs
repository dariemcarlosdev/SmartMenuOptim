using System;

namespace SmartMenuOptim.Application.Common
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
        // Add dish price if needed
         public decimal DishPrice { get; set; } // Optional: Price of the dish, if applicable
        public int QuantitySold { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = string.Empty; // Optional: Category of the dish, if applicable
        public int Rating { get; set; } = 0; // Optional: Rating of the dish based on reviews, if applicable

        public string RestaurantName { get; set; } = string.Empty; // Optional: Name of the restaurant, if applicable.

        // This property is used to link the sale record to a specific restaurant, if applicable.
        //public int RstaurantId { get; set; } = 0; // Optional: Restaurant ID of the dish, if applicable.
    }
}
