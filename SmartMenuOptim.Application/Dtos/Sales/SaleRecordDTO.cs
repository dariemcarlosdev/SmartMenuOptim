namespace SmartMenuOptim.Application.Dtos.Sales;

/// <summary>
/// DTO for transferring sale record data between layers and for CRUD operations in Blazor user interfaces.
/// </summary>
public class SaleRecordDTO
{
    public int Id { get; set; }
    public int DishId { get; set; }
    public string? DishName { get; set; }
    public decimal DishPrice { get; set; }
    public int QuantitySold { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
}
