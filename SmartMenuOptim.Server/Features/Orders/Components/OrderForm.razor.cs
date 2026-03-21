using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Customers.DTOs;
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
    [Inject] private IDishClientService DishClientService { get; set; } = default!;
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

    // Customer dropdown state
    private IReadOnlyList<CustomerLookupDTO>? _customers;
    private bool _customersLoading = true;

    // Dish dropdown state (filtered by selected restaurant)
    private IReadOnlyList<DishDTO>? _dishes;
    private bool _dishesLoading;

    protected override async Task OnInitializedAsync()
    {
        // Load restaurants and customers in parallel
        var restaurantTask = RestaurantClientService.GetAllAsync();
        var customerTask = OrderService.GetCustomerLookupsAsync();

        await Task.WhenAll(restaurantTask, customerTask);

        var restaurantResult = restaurantTask.Result;
        _restaurants = restaurantResult.IsSuccess ? restaurantResult.Value : [];
        _restaurantsLoading = false;

        var customerResult = customerTask.Result;
        _customers = customerResult.IsSuccess ? customerResult.Value : [];
        _customersLoading = false;

        // Auto-select first restaurant and load its dishes
        if (_restaurants is not null && _restaurants.Any())
        {
            _model.RestaurantId = _restaurants.First().Id;
            await LoadDishesAsync(_model.RestaurantId);
        }

        // Auto-select first customer
        if (_customers is not null && _customers.Any())
        {
            _model.CustomerId = _customers.First().Id;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RESTAURANT CHANGE
    // ═══════════════════════════════════════════════════════════════════════

    private async Task OnRestaurantChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var restaurantId) && restaurantId > 0)
        {
            _model.RestaurantId = restaurantId;

            // Clear existing items since dishes belong to the previous restaurant
            _model.Items.Clear();

            await LoadDishesAsync(restaurantId);
        }
    }

    private async Task LoadDishesAsync(int restaurantId)
    {
        _dishesLoading = true;
        _dishes = null;

        var result = await DishClientService.GetByRestaurantIdAsync(restaurantId);
        _dishes = result.IsSuccess ? result.Value : [];
        _dishesLoading = false;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ITEM MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    private void AddItem()
    {
        var newItem = new OrderItemCreateDTO { Quantity = 1 };

        // Auto-select first available dish
        if (_dishes is not null && _dishes.Any())
        {
            newItem.DishId = _dishes.First().Id;
        }

        _model.Items.Add(newItem);
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
