namespace SmartMenuOptim.Application.Dtos.Dish;

/// <summary>
/// DTO representing an underperforming dish identified by menu analytics.
/// </summary>
public class UnderperformingDishDTO
{
    public int DishId { get; init; }
    public string DishName { get; init; } = string.Empty;
    public int TotalSales { get; init; }
    public double AverageSentiment { get; init; }
    public int TotalReviews { get; init; }
    public decimal AverageRating { get; init; }
    public List<string> Comments { get; set; } = new();
}
