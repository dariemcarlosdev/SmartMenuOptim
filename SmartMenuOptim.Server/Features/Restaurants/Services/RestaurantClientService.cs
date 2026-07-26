/*
 * File: RestaurantClientService.cs
 * HTTP client-based implementation for Restaurant operations
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Implements Restaurant operations by communicating with the backend API.
 * Follows the Adapter Pattern to adapt HTTP responses to the Result pattern.
 * 
 * Design Patterns:
 * - Adapter Pattern: Adapts HTTP API to service interface
 * - Result Pattern: Encapsulates success/failure without exceptions
 */

using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Helpers;
using SmartMenuOptim.Server.Features.Restaurants.Services;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// HTTP client-based implementation for Restaurant operations.
/// </summary>
/// <remarks>
/// <para><strong>Implementation Details:</strong></para>
/// <para>This service communicates with the backend API using IHttpClientFactory
/// and translates HTTP responses into Result objects for consistent error handling.</para>
/// </remarks>
public class RestaurantClientService : IRestaurantClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RestaurantClientService> _logger;
    private const string ApiBasePath = "api/v1/restaurants";

    /// <summary>
    /// Initializes a new instance of RestaurantClientService.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public RestaurantClientService(
        IHttpClientFactory httpClientFactory,
        ILogger<RestaurantClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"{ApiBasePath}/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDTO>(cancellationToken);
                return restaurant is not null
                    ? Result.Success(restaurant)
                    : Result.Failure<RestaurantDTO>("Restaurant not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load restaurant.");
            return Result.Failure<RestaurantDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading restaurant {Id}", id);
            return Result.Failure<RestaurantDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading restaurant {Id}", id);
            return Result.Failure<RestaurantDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var restaurants = await client.GetFromJsonAsync<List<RestaurantDTO>>(ApiBasePath, cancellationToken);
            
            return Result.Success<IReadOnlyList<RestaurantDTO>>(restaurants ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading restaurants");
            return Result.Failure<IReadOnlyList<RestaurantDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading restaurants");
            return Result.Failure<IReadOnlyList<RestaurantDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ToggleAcceptingOrdersAsync(int id, bool isAccepting, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PatchAsync(
                $"{ApiBasePath}/{id}/status?isAccepting={isAccepting}",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Restaurant {Id} status toggled to {Status}", id, isAccepting);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update status.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error toggling status for restaurant {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error toggling status for restaurant {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync(ApiBasePath, dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDTO>(cancellationToken);
                return restaurant is not null
                    ? Result.Success(restaurant)
                    : Result.Failure<RestaurantDTO>("Failed to create restaurant.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to create restaurant.");
            return Result.Failure<RestaurantDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating restaurant");
            return Result.Failure<RestaurantDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating restaurant");
            return Result.Failure<RestaurantDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RestaurantDTO>> UpdateAsync(RestaurantUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync($"{ApiBasePath}/{dto.Id}", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDTO>(cancellationToken);
                return restaurant is not null
                    ? Result.Success(restaurant)
                    : Result.Failure<RestaurantDTO>("Failed to update restaurant.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update restaurant.");
            return Result.Failure<RestaurantDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating restaurant {Id}", dto.Id);
            return Result.Failure<RestaurantDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating restaurant {Id}", dto.Id);
            return Result.Failure<RestaurantDTO>("An unexpected error occurred.");
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
                _logger.LogInformation("Restaurant {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete restaurant.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting restaurant {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting restaurant {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }
}
