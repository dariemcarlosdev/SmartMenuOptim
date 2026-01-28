using System.Collections.Generic;

namespace SmartMenuOptim.Application.Common
{
    /// <summary>
    /// DTO for transferring AdminUser data between layers and for CRUD operations in Blazor user interfaces.
    /// To make your DTOs ready for CRUD operations in user interfaces (Blazor), the best approach is to ensure:
    /// • All properties are mutable (get/set).
    /// • Use nullable types for optional fields.
    /// • Use default values for collections.
    /// • Avoid navigation properties, but allow for IDs and display names where needed.
    /// • DTOs should be simple POCOs, suitable for model binding and form editing.
    /// </summary>
    public class AdminUserDTO : UserBaseDTO
    {
        public string Role { get; set; } = "Admin";
        public int SalesThreshold { get; set; } = 35;
        public double SentimentThreshold { get; set; } = 0.6;
        public int ReviewCountThreshold { get; set; } = 5;
        public int WellSoldThreshold { get; set; } = 20;
        public int RegularCustomerReviewCountThreshold { get; set; } = 3;
        public int PremiumCustomerReviewCountThreshold { get; set; } = 10;
        public List<string> Permissions { get; set; } = new();
    }
}
