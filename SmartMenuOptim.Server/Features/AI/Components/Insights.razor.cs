using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Server.Features.AI.Services;
using SmartMenuOptim.Server.Features.Reviews.Services;
using SmartMenuOptim.Server.Features.Sales.Services;

namespace SmartMenuOptim.Server.Features.AI.Components;

/// <summary>
/// Insights page code-behind. Loads sales + reviews, then asks the AI client for
/// top dish recommendations. A null result renders the "No recommendations" notice.
/// </summary>
public sealed partial class Insights : ComponentBase
{
    [Inject] private IAIClientService AIService { get; set; } = default!;
    [Inject] private IReviewClientService ReviewService { get; set; } = default!;
    [Inject] private ISaleRecordClientService SaleRecordService { get; set; } = default!;

    private List<AiRecommendationResponseDTO>? recommendations;
    private bool isloading = true;
    private bool _showGuide;

    private void ToggleGuide() => _showGuide = !_showGuide;

    protected override async Task OnInitializedAsync()
    {
        var sales = await SaleRecordService.GetSaleRecordsAsync();
        var reviews = await ReviewService.GetReviewsAsync();

        recommendations = await AIService.GetRecommendationsAsync(sales, reviews);
        isloading = false;
    }
}
