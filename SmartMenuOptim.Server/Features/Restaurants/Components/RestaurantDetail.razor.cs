using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Restaurants.State;

namespace SmartMenuOptim.Server.Features.Restaurants.Components;

/// <summary>
/// Code-behind for RestaurantDetail component.
/// Uses state container pattern for clean separation of concerns.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This component delegates all business logic and data operations to the
/// RestaurantDetailState container, keeping the component focused on UI orchestration.</para>
/// 
/// <para><strong>State Management:</strong></para>
/// <para>Uses the State Container Pattern for predictable state management.
/// The component subscribes to state changes and re-renders automatically.</para>
/// </remarks>
public partial class RestaurantDetail : ComponentBase, IDisposable
{
    [Inject] private RestaurantDetailState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    // Expose state properties for the view
    private RestaurantDTO? _restaurant => State.Restaurant;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;
    private bool _togglingStatus => State.IsTogglingStatus;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(Id);
    }

    private async Task ToggleStatusAsync()
    {
        await State.ToggleStatusAsync(Id);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    private void DismissError() => State.ClearError();

    private void NavigateToEdit() => Navigation.NavigateTo($"/restaurants/{Id}/edit");

    private void NavigateToMenus() => Navigation.NavigateTo($"/restaurants/{Id}/menus");

    private void NavigateToCategories() => Navigation.NavigateTo($"/restaurants/{Id}/categories");

    private void NavigateToDishes() => Navigation.NavigateTo($"/restaurants/{Id}/dishes");

    private void NavigateToBusinessHours() => Navigation.NavigateTo($"/restaurants/{Id}/edit#business-hours");

    private void NavigateToInsights() => Navigation.NavigateTo("/insights");

    private void NavigateToDashboard() => Navigation.NavigateTo("/dashboard");

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
