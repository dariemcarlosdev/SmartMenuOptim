/*
 * File: OrderClientService.cs
 * HTTP client-based implementation for Order operations
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Implements Order operations by communicating with the backend API.
 * Follows the Adapter Pattern to adapt HTTP responses to the Result pattern.
 * 
 * Design Patterns:
 * - Adapter Pattern: Adapts HTTP API to service interface
 * - Result Pattern: Encapsulates success/failure without exceptions
 * 
 * Reference: RestaurantClientService.cs (canonical pattern)
 */

using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Customers.DTOs;
using SmartMenuOptim.Application.Features.Orders.DTOs;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Orders.Services;

/// <summary>
/// HTTP client-based implementation for Order operations.
/// </summary>
/// <remarks>
/// <para><strong>Implementation Details:</strong></para>
/// <para>This service communicates with the backend API using IHttpClientFactory
/// and translates HTTP responses into Result objects for consistent error handling.</para>
/// </remarks>
public class OrderClientService : IOrderClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderClientService> _logger;
    private const string ApiBasePath = "api/v1/orders";

    /// <summary>
    /// Initializes a new instance of OrderClientService.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public OrderClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<OrderClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrderDTO>>> GetByRestaurantAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var orders = await client.GetFromJsonAsync<List<OrderDTO>>(
                $"{ApiBasePath}?restaurantId={restaurantId}", cancellationToken);

            return Result.Success<IReadOnlyList<OrderDTO>>(orders ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading orders for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<OrderDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading orders for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<OrderDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<OrderDetailDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"{ApiBasePath}/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderDetailDTO>(cancellationToken);
                return order is not null
                    ? Result.Success(order)
                    : Result.Failure<OrderDetailDTO>("Order not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load order.");
            return Result.Failure<OrderDetailDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading order {Id}", id);
            return Result.Failure<OrderDetailDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading order {Id}", id);
            return Result.Failure<OrderDetailDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OrderStatusDTO>>> GetStatusesAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var statuses = await client.GetFromJsonAsync<List<OrderStatusDTO>>(
                $"{ApiBasePath}/statuses?restaurantId={restaurantId}", cancellationToken);

            return Result.Success<IReadOnlyList<OrderStatusDTO>>(statuses ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading order statuses for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<OrderStatusDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading order statuses for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<OrderStatusDTO>>("An unexpected error occurred.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<Result<OrderDTO>> CreateAsync(OrderCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync(ApiBasePath, dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderDTO>(cancellationToken);
                return order is not null
                    ? Result.Success(order)
                    : Result.Failure<OrderDTO>("Failed to create order.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to create order.");
            return Result.Failure<OrderDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating order");
            return Result.Failure<OrderDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating order");
            return Result.Failure<OrderDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<OrderDTO>> UpdateStatusAsync(int id, int newStatusId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PatchAsJsonAsync(
                $"{ApiBasePath}/{id}/status",
                new { NewStatusId = newStatusId },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<OrderDTO>(cancellationToken);
                return order is not null
                    ? Result.Success(order)
                    : Result.Failure<OrderDTO>("Failed to update order status.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update order status.");
            return Result.Failure<OrderDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating status for order {Id}", id);
            return Result.Failure<OrderDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating status for order {Id}", id);
            return Result.Failure<OrderDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> CancelAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync(
                $"{ApiBasePath}/{id}/cancel",
                new { Reason = reason },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Order {Id} cancelled", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to cancel order.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error cancelling order {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error cancelling order {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"{ApiBasePath}/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Order {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete order.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting order {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting order {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CustomerLookupDTO>>> GetCustomerLookupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var customers = await client.GetFromJsonAsync<List<CustomerLookupDTO>>(
                $"{ApiBasePath}/customers/lookup", cancellationToken);

            return Result.Success<IReadOnlyList<CustomerLookupDTO>>(customers ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading customer lookups");
            return Result.Failure<IReadOnlyList<CustomerLookupDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading customer lookups");
            return Result.Failure<IReadOnlyList<CustomerLookupDTO>>("An unexpected error occurred.");
        }
    }
}
