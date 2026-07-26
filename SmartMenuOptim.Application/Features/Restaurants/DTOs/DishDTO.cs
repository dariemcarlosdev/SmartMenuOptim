namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// DTO for transferring dish data between layers and for CRUD operations in Blazor user interfaces.
/// </summary>
public class DishDTO
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal Price { get; set; }
    public int? Calories { get; set; }
    public bool IsVegetarian { get; set; }
    public bool IsSpicy { get; set; }
    public string? Ingredients { get; set; }
    public bool IsActive { get; set; }
}
