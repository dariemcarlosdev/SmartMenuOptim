namespace SmartMenuOptim.Shared.Data.Dtos
{
    /// <summary>
    /// DTO for transferring dish data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class DishDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; } // For UI display
    }
}
