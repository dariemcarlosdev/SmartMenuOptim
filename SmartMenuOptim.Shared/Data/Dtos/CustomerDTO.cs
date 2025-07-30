using System;

namespace SmartMenuOptim.Shared.Data.DTOs
{
    /// <summary>
    /// DTO for transferring Customer data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class CustomerDTO : UserBaseDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
        public string? Role { get; set; }
    }
}
