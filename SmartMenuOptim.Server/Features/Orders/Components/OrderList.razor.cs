using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Orders.State;
using SmartMenuOptim.Server.Features.Restaurants.Services;

namespace SmartMenuOptim.Server.Features.Orders.Components;

/// <summary>
/// Code-behind for the OrderList page component.
/// Uses state container pattern for clean separation of concerns.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This component delegates all data operations to the
/// OrderListState container, keeping the component focused on UI orchestration.</para>
///
/// <para><strong>State Management:</strong></para>
/// <para>Uses the State Container Pattern for predictable state management.
/// The component subscribes to state changes and re-renders automatically.</para>
/// </remarks>
public partial class OrderList : ComponentBase, IDisposable
{
    [Inject] private OrderListState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IRestaurantClientService RestaurantClientService { get; set; } = default!;

    // Expose state properties for the view
    private IReadOnlyList<OrderDTO>? _orders => State.Orders;
    private IReadOnlyList<OrderDTO>? _filteredOrders => State.FilteredOrders;
    private IReadOnlyList<OrderStatusDTO>? _statuses => State.Statuses;
    private int? _selectedStatusFilter => State.SelectedStatusFilter;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;
    private bool _showDeleteModal => State.ShowDeleteModal;
    private OrderDTO? _orderToDelete => State.OrderToDelete;
    private bool _deleting => State.IsDeleting;
    private bool _updatingStatus => State.IsUpdatingStatus;
    private bool _cancelling => State.IsCancelling;
    private bool _showCancelModal => State.ShowCancelModal;
    private OrderDTO? _orderToCancel => State.OrderToCancel;

    // Local UI state
    private string _cancelReasonInput = string.Empty;
    private bool _cancelReasonInvalid;
    private bool _showGuide;

    private void ToggleGuide() => _showGuide = !_showGuide;

    // Restaurant filter state
    private IReadOnlyList<RestaurantDTO>? _restaurants;
    private int _selectedRestaurantId;
    private bool _restaurantsLoading = true;

    /// <summary>
    /// Optional query parameter for deep-linking (e.g., from Dashboard: /orders?restaurantId=2).
    /// </summary>
    [SupplyParameterFromQuery(Name = "restaurantId")]
    public int? RestaurantIdFromQuery { get; set; }

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;

        // Load restaurants for the filter dropdown
        var result = await RestaurantClientService.GetAllAsync();
        _restaurants = result.IsSuccess ? result.Value : [];
        _restaurantsLoading = false;

        // Use query param if provided; otherwise default to the first restaurant
        if (_restaurants is not null && _restaurants.Any())
        {
            _selectedRestaurantId = RestaurantIdFromQuery is > 0 && _restaurants.Any(r => r.Id == RestaurantIdFromQuery)
                ? RestaurantIdFromQuery.Value
                : _restaurants.First().Id;

            await State.LoadAsync(_selectedRestaurantId);
        }
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    // ═══════════════════════════════════════════════════════════════════════
    // RESTAURANT FILTER
    // ═══════════════════════════════════════════════════════════════════════

    private async Task OnRestaurantChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var restaurantId) && restaurantId > 0)
        {
            _selectedRestaurantId = restaurantId;
            await State.LoadAsync(_selectedRestaurantId);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadOrdersAsync() => await State.LoadAsync(_selectedRestaurantId);

    // ═══════════════════════════════════════════════════════════════════════
    // FILTERING
    // ═══════════════════════════════════════════════════════════════════════

    private void SetStatusFilter(int? statusId) => State.SelectedStatusFilter = statusId;

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    private async Task UpdateStatusAsync(int orderId, int newStatusId)
        => await State.UpdateStatusAsync(orderId, newStatusId);

    // ═══════════════════════════════════════════════════════════════════════
    // CANCEL ORDER
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmCancel(OrderDTO order) => State.ConfirmCancel(order);

    private void DismissCancel()
    {
        _cancelReasonInput = string.Empty;
        _cancelReasonInvalid = false;
        State.DismissCancel();
    }

    private async Task CancelOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(_cancelReasonInput) || _cancelReasonInput.Length < 3)
        {
            _cancelReasonInvalid = true;
            return;
        }

        _cancelReasonInvalid = false;
        State.CancelReason = _cancelReasonInput;
        await State.CancelOrderAsync();
        _cancelReasonInput = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    private void CreateNew() => Navigation.NavigateTo("/orders/new");

    private void ViewDetails(int id) => Navigation.NavigateTo($"/orders/{id}");

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmDelete(OrderDTO order) => State.ConfirmDelete(order);

    private void CancelDelete() => State.CancelDelete();

    private async Task DeleteOrderAsync() => await State.DeleteAsync();

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
