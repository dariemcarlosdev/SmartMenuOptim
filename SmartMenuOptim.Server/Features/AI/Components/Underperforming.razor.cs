using Microsoft.AspNetCore.Components;
using MudBlazor;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Server.Features.AI.Services;
using SmartMenuOptim.Server.Features.Reviews.Services;

namespace SmartMenuOptim.Server.Features.AI.Components;

/// <summary>
/// Code-behind for the Underperforming page.
/// Lists dishes with low sales/sentiment and requests AI-generated improvement strategies on demand.
/// </summary>
public partial class Underperforming : ComponentBase
{
    [Inject] private IAIClientService aIService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IReviewClientService ReviewService { get; set; } = default!;

    private List<UnderperformingDishDTO>? underperformingDishes;
    private bool isloading = true;
    private bool _showGuide;

    private void ToggleGuide() => _showGuide = !_showGuide;

    // Modal State. It is used to show the AI suggestion modal.
    private bool showModal = false;
    private UnderperformingDishDTO? selectedDish;
    private string? aiSuggestion;
    private bool loadingSuggestion = false;

    protected override async Task OnInitializedAsync()
    {
        underperformingDishes = await aIService.GetUnderperformingDishesAsync();
        isloading = false;
    }

    private async void ShowImproveModal(UnderperformingDishDTO dish)
    {
        // Reset modal state
        selectedDish = dish;
        showModal = true;
        loadingSuggestion = true;
        aiSuggestion = null;
        StateHasChanged(); // Force UI update to show loading state

        // Call the AI service to get improvement suggestions
        try
        {
            var response = await aIService.GetImprovementStrategyAsync(dish);
            if (!string.IsNullOrEmpty(response))
            {
                aiSuggestion = response;
            }
            else
            {
                aiSuggestion = "Failed to fetch suggestion. Please try again later.";
            }
        }
        catch (Exception ex)
        {
            aiSuggestion = $"Error fetching suggestion: {ex.Message}";
        }
        loadingSuggestion = false;
        StateHasChanged(); // Update UI with the fetched suggestion
    }

    private void CloseModal()
    {
        showModal = false;
        selectedDish = null;
        aiSuggestion = null;
    }

    private void OnDishClicked(string dishName, double sentiment)
    {
        // Optionally update the URL:
        NavigationManager.NavigateTo($"/reviews?dishname={Uri.EscapeDataString(dishName)}&sentiment={sentiment}");
    }
}
