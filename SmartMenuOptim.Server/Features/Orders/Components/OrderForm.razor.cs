using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Orders.Services;
using SmartMenuOptim.Server.Features.Restaurants.Services;

namespace SmartMenuOptim.Server.Features.Orders.Components;

/// <summary>
/// Code-behind for the OrderForm page component.
/// Handles order creation with item management.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This component uses the IOrderClientService directly for the create operation,
/// as form components typically don't need a full state container.</para>
/// </remarks>
public partial class OrderForm : ComponentBase
{
    [Inject] private IOrderClientService OrderService { get; set; } = default!;
    [Inject] private IRestaurantClientService RestaurantClientService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<OrderForm> Logger { get; set; } = default!;

    private OrderCreateDTO _model = new()
    {
        Items = [new OrderItemCreateDTO { Quantity = 1 }]
    };

    private string? _error;
    private bool _submitting;

    // Restaurant dropdown state
    private IReadOnlyList<RestaurantDTO>? _restaurants;
    private bool _restaurantsLoading = true;

    protected override async Task OnInitializedAsync()
    {
        var result = await RestaurantClientService.GetAllAsync();
        _restaurants = result.IsSuccess ? result.Value : [];
        _restaurantsLoading = false;

        // Auto-select first restaurant
        if (_restaurants is not null && _restaurants.Any())
        {
            _model.RestaurantId = _restaurants.First().Id;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ITEM MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    private void AddItem()
    {
        _model.Items.Add(new OrderItemCreateDTO { Quantity = 1 });
    }

    private void RemoveItem(int index)
    {
        if (index >= 0 && index < _model.Items.Count)
        {
            _model.Items.RemoveAt(index);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORM SUBMISSION
    // ═══════════════════════════════════════════════════════════════════════

    private async Task HandleSubmitAsync()
    {
        _error = null;
        _submitting = true;

        try
        {
            var result = await OrderService.CreateAsync(_model);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Order created successfully with ID {OrderId}", result.Value.Id);
                Navigation.NavigateTo($"/orders/{result.Value.Id}");
            }
            else
            {
                _error = result.Error ?? "Failed to create order.";
                Logger.LogWarning("Failed to create order: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error creating order");
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _submitting = false;
        }
    }
}
