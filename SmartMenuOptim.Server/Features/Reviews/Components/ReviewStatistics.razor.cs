using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos;

namespace SmartMenuOptim.Server.Features.Reviews.Components;

/// <summary>
/// Code-behind for the ReviewStatistics component.
/// Displays aggregate totals (count, average rating, average sentiment) for a review set.
/// </summary>
public partial class ReviewStatistics : ComponentBase
{
    // Parameters is a collection of ReviewDTO objects passed to the component
    [Parameter]
    public IEnumerable<ReviewDTO>? Reviews { get; set; }

    // Calculate average rating and sentiment score
    private double AverageRating => Reviews?.Any() == true ? Reviews.Average(r => r.Rating) : 0;
    private double AverageSentiment => Reviews?.Any() == true ? Reviews.Average(r => r.SentimentScore) : 0;
}
