using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Restaurants.State;

namespace SmartMenuOptim.Server.Features.Restaurants.Components;

/// <summary>
/// Code-behind for the RestaurantList page component.
/// Uses state container pattern for clean separation of concerns.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This component delegates all data operations to the
/// RestaurantListState container, keeping the component focused on UI orchestration.</para>
///
/// <para><strong>State Management:</strong></para>
/// <para>Uses the State Container Pattern for predictable state management.
/// The component subscribes to state changes and re-renders automatically.</para>
/// </remarks>
public partial class RestaurantList : ComponentBase, IDisposable
{
    [Inject] private RestaurantListState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Expose state properties for the view
    private IReadOnlyList<RestaurantDTO>? _restaurants => State.Restaurants;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;
    private bool _showDeleteModal => State.ShowDeleteModal;
    private RestaurantDTO? _restaurantToDelete => State.RestaurantToDelete;
    private bool _deleting => State.IsDeleting;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync();
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadRestaurantsAsync() => await State.LoadAsync();

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    private void CreateNew() => Navigation.NavigateTo("/restaurants/new");

    private void ViewDetails(int id) => Navigation.NavigateTo($"/restaurants/{id}");

    private void Edit(int id) => Navigation.NavigateTo($"/restaurants/{id}/edit");

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmDelete(RestaurantDTO restaurant) => State.ConfirmDelete(restaurant);

    private void CancelDelete() => State.CancelDelete();

    private async Task DeleteRestaurantAsync() => await State.DeleteAsync();

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
