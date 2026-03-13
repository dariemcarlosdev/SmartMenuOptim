namespace SmartMenuOptim.Application.Features.AI.DTOs;

/// <summary>
/// DTO for AI recommendation request data.
/// </summary>
/// <remarks>
/// Contains the data needed for AI-powered menu optimization recommendations.
/// </remarks>
public class AiRecommendationRequestDTO
{
    public List<Reviews.DTOs.ReviewDTO> Reviews { get; set; } = new();
    public List<Sales.DTOs.SaleRecordDTO> SaleRecords { get; set; } = new();
}
