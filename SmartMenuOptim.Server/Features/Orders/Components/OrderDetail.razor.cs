using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Server.Features.Orders.State;

namespace SmartMenuOptim.Server.Features.Orders.Components;

/// <summary>
/// Code-behind for OrderDetail component.
/// Uses state container pattern for clean separation of concerns.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This component delegates all business logic and data operations to the
/// OrderDetailState container, keeping the component focused on UI orchestration.</para>
/// 
/// <para><strong>State Management:</strong></para>
/// <para>Uses the State Container Pattern for predictable state management.
/// The component subscribes to state changes and re-renders automatically.</para>
/// </remarks>
public partial class OrderDetail : ComponentBase, IDisposable
{
    [Inject] private OrderDetailState State { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    // Expose state properties for the view
    private OrderDetailDTO? _order => State.Order;
    private bool _loading => State.IsLoading;
    private string? _error => State.Error;
    private bool _updatingStatus => State.IsUpdatingStatus;
    private bool _cancelling => State.IsCancelling;
    private bool _showCancelModal => State.ShowCancelModal;
    private IReadOnlyList<OrderStatusDTO>? _statuses => State.Statuses;

    // Local UI state
    private int _selectedStatusId;
    private string _cancelReason = string.Empty;
    private bool _cancelReasonInvalid;

    protected override async Task OnInitializedAsync()
    {
        State.OnStateChanged += HandleStateChanged;
        await State.LoadAsync(Id);
    }

    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS UPDATE
    // ═══════════════════════════════════════════════════════════════════════

    private async Task UpdateStatusAsync()
    {
        if (_selectedStatusId <= 0) return;
        await State.UpdateStatusAsync(Id, _selectedStatusId);
        _selectedStatusId = 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CANCEL ORDER
    // ═══════════════════════════════════════════════════════════════════════

    private void ShowCancelModal() => State.ShowCancelConfirmation();

    private void HideCancelModal()
    {
        _cancelReason = string.Empty;
        _cancelReasonInvalid = false;
        State.HideCancelConfirmation();
    }

    private async Task CancelOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(_cancelReason) || _cancelReason.Length < 3)
        {
            _cancelReasonInvalid = true;
            return;
        }

        _cancelReasonInvalid = false;
        State.CancelReason = _cancelReason;
        await State.CancelOrderAsync(Id);
        _cancelReason = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    private void DismissError() => State.ClearError();

    public void Dispose()
    {
        State.OnStateChanged -= HandleStateChanged;
        GC.SuppressFinalize(this);
    }
}
