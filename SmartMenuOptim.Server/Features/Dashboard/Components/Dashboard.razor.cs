using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Orders.Services;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.Features.Sales.Services;

namespace SmartMenuOptim.Server.Features.Dashboard.Components;

/// <summary>
/// Dashboard page code-behind. Aggregates restaurant, order, and sales data,
/// then wires the shared zero-dependency motion module (reveals + KPI count-up)
/// once every async source has loaded.
/// </summary>
public sealed partial class Dashboard : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISaleRecordClientService SaleRecordService { get; set; } = default!;
    [Inject] private IRestaurantClientService RestaurantClientService { get; set; } = default!;
    [Inject] private IOrderClientService OrderClientService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<SaleRecordDTO>? saleRecords;
    private IReadOnlyList<RestaurantDTO>? restaurants;
    private List<OrderDTO>? allOrders;
    private bool restaurantsLoading = true;
    private bool ordersLoading = true;
    private bool isloading = true;
    private string? expandedRestaurant;
    private bool _showGuide;

    private ElementReference Root;
    private IJSObjectReference? _module;
    private bool _motionWired;

    /// <summary>True once all three async data sources have resolved.</summary>
    private bool _ready => !restaurantsLoading && !ordersLoading && !isloading;

    protected override async Task OnInitializedAsync()
    {
        var restaurantsTask = RestaurantClientService.GetAllAsync();
        var salesTask = SaleRecordService.GetSaleRecordsAsync();

        await Task.WhenAll(restaurantsTask, salesTask);

        var restaurantResult = restaurantsTask.Result;
        restaurants = restaurantResult.IsSuccess ? restaurantResult.Value : [];
        restaurantsLoading = false;

        saleRecords = salesTask.Result;
        isloading = false;

        // Load orders for each restaurant (after restaurants are loaded)
        if (restaurants is not null && restaurants.Any())
        {
            var orderTasks = restaurants.Select(r => OrderClientService.GetByRestaurantAsync(r.Id));
            var orderResults = await Task.WhenAll(orderTasks);

            allOrders = orderResults
                .Where(r => r is { IsSuccess: true, Value: not null })
                .SelectMany(r => r.Value)
                .ToList();
        }
        else
        {
            allOrders = [];
        }

        ordersLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Wire motion only after data is present so count-up animates real values,
        // not the loading placeholders. Fires on the first render where _ready is true.
        if (!_ready || _motionWired)
        {
            return;
        }

        _motionWired = true;
        _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/home-motion.js");
        await _module.InvokeVoidAsync("init", Root);
    }

    private void ToggleRestaurant(string restaurantName)
    {
        expandedRestaurant = expandedRestaurant == restaurantName ? null : restaurantName;
    }

    private void ToggleGuide()
    {
        _showGuide = !_showGuide;
    }

    private void OnDishClicked(string? dishName)
    {
        if (string.IsNullOrWhiteSpace(dishName))
        {
            return;
        }

        NavigationManager.NavigateTo($"/reviews?dishname={Uri.EscapeDataString(dishName)}");
    }

    private void NavigateToRestaurant(int id) => NavigationManager.NavigateTo($"/restaurants/{id}");

    private void NavigateToInsights() => NavigationManager.NavigateTo("/insights");

    // ── Summary computed properties ─────────────────────────────────────
    private int SummarySaleRecords => expandedRestaurant is null
        ? saleRecords?.Count ?? 0
        : saleRecords?.Count(r => r.RestaurantName == expandedRestaurant) ?? 0;

    private decimal OrderTotalRevenue => allOrders?.Sum(o => o.TotalAmount) ?? 0;
    private decimal OrderAverageValue => allOrders is not null && allOrders.Any() ? allOrders.Average(o => o.TotalAmount) : 0;
    private int ActiveOrderCount => allOrders?.Count(o => !o.IsTerminal) ?? 0;

    private int SummaryDishesSold => expandedRestaurant is null
        ? saleRecords?.Sum(r => r.QuantitySold) ?? 0
        : saleRecords?.Where(r => r.RestaurantName == expandedRestaurant).Sum(r => r.QuantitySold) ?? 0;

    private decimal SummaryTotalSales => expandedRestaurant is null
        ? saleRecords?.Sum(r => r.DishPrice * r.QuantitySold) ?? 0
        : saleRecords?.Where(r => r.RestaurantName == expandedRestaurant).Sum(r => r.DishPrice * r.QuantitySold) ?? 0;

    /// <summary>Culture-invariant number string for JS <c>data-count</c> attributes.</summary>
    private static string Inv(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static RenderFragment RenderStars(double rating) => builder =>
    {
        int filled = (int)Math.Round(rating);
        for (int i = 0; i < filled; i++)
        {
            builder.OpenElement(0, "span");
            builder.AddContent(1, "★");
            builder.CloseElement();
        }
        for (int i = filled; i < 5; i++)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "smo-star-empty");
            builder.AddContent(2, "☆");
            builder.CloseElement();
        }
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("dispose");
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone — nothing to clean up.
        }
        catch (OperationCanceledException)
        {
            // Render/dispose race — safe to ignore.
        }
    }
}
