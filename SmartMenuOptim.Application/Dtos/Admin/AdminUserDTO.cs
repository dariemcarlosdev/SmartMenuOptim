using SmartMenuOptim.Application.Dtos.Common;

namespace SmartMenuOptim.Application.Dtos.Admin;

/// <summary>
/// DTO for transferring AdminUser data between layers and for CRUD operations in Blazor user interfaces.
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
