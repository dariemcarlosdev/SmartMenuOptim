using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Dtos.Dish;

/// <summary>
/// Data Transfer Object for creating a new Dish.
/// </summary>
public class DishCreateDTO
{
    [Required(ErrorMessage = "Restaurant ID is required")]
    public int RestaurantId { get; set; }

    [Required(ErrorMessage = "Dish name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
    public decimal DishPrice { get; set; }

    [Range(0, 10000)]
    public int? Calories { get; set; }

    public bool IsVegetarian { get; set; }

    public bool IsSpicy { get; set; }

    [StringLength(1000)]
    public string? Ingredients { get; set; }

    public bool IsActive { get; set; } = true;
}
