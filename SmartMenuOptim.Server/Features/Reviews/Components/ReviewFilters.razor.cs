using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Server.Features.Reviews.Models;

namespace SmartMenuOptim.Server.Features.Reviews.Components;

/// <summary>
/// Code-behind for the ReviewFilters component.
/// Renders sort/rating/search controls over the shared <see cref="ReviewFilterState"/>
/// and notifies the parent when filters change.
/// </summary>
public partial class ReviewFilters : ComponentBase
{
    // Cascading parameter to receive the shared filter state. Cascading parameters allow
    // passing data down the component hierarchy without explicitly passing through each level.
    [CascadingParameter] public ReviewFilterState FilterState { get; set; } = null!;

    // Event callback to notify parent component of filter changes. Useful for child-to-parent communication.
    [Parameter] public EventCallback OnFiltersChanged { get; set; }

    // Method to handle search input changes
    private async Task OnSearch()
    {
        await OnFiltersChanged.InvokeAsync();
    }

    // Method to handle rating filter changes
    private void OnRatingChanged(ChangeEventArgs e)
    {
        // Try to parse the selected value to an integer and update MinRating in the shared filter state
        if (int.TryParse(e.Value?.ToString(), out int minRating))
        {
            FilterState.MinRating = minRating; // Update the MinRating property in the shared filter state
            OnFiltersChanged.InvokeAsync(); // Notify parent component of filter changes
        }
    }

    // Method to handle sort option changes
    private void OnSortChanged(ChangeEventArgs e)
    {
        FilterState.SortBy = e.Value?.ToString() ?? "date"; // Update the SortBy property in the shared filter state
        OnFiltersChanged.InvokeAsync(); // Notify parent component of filter changes
    }
}
