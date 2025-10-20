using System;

namespace SmartMenuOptim.Shared.Data.Dtos
{
    /// <summary>
    /// DTO for transferring review data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class ReviewDTO
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public double SentimentScore { get; set; }
        public int DishId { get; set; }
        public string? DishName { get; set; } // For UI display
        public int? CustomerId { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public int Rating { get; set; }
    }
}
