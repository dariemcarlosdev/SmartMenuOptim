namespace SmartMenuOptim.Application.Dtos.AI;

/// <summary>
/// DTO for AI recommendation request data.
/// </summary>
/// <remarks>
/// Contains the data needed for AI-powered menu optimization recommendations.
/// </remarks>
public class AiRecommendationRequestDTO
{
    public List<Review.ReviewDTO> Reviews { get; set; } = new();
    public List<Sales.SaleRecordDTO> SaleRecords { get; set; } = new();
}
