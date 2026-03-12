using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// HTTP client-based implementation for Dish operations.
/// </summary>
public class DishClientService : IDishClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DishClientService> _logger;

    public DishClientService(IHttpClientFactory httpClientFactory, ILogger<DishClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<DishDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"api/v1/dishes/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dish = await response.Content.ReadFromJsonAsync<DishDTO>(cancellationToken);
                return dish is not null
                    ? Result.Success(dish)
                    : Result.Failure<DishDTO>("Dish not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load dish.");
            return Result.Failure<DishDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading dish {Id}", id);
            return Result.Failure<DishDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading dish {Id}", id);
            return Result.Failure<DishDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DishDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var dishes = await client.GetFromJsonAsync<List<DishDTO>>(
                $"api/v1/restaurants/{restaurantId}/dishes", cancellationToken);
            return Result.Success<IReadOnlyList<DishDTO>>(dishes ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading dishes for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<DishDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading dishes");
            return Result.Failure<IReadOnlyList<DishDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DishDTO>>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var dishes = await client.GetFromJsonAsync<List<DishDTO>>(
                $"api/v1/menus/{menuId}/dishes", cancellationToken);
            return Result.Success<IReadOnlyList<DishDTO>>(dishes ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading dishes for menu {MenuId}", menuId);
            return Result.Failure<IReadOnlyList<DishDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading menu dishes");
            return Result.Failure<IReadOnlyList<DishDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<DishDTO>> CreateAsync(int restaurantId, DishCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync($"api/v1/restaurants/{restaurantId}/dishes", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dish = await response.Content.ReadFromJsonAsync<DishDTO>(cancellationToken);
                return dish is not null
                    ? Result.Success(dish)
                    : Result.Failure<DishDTO>("Failed to create dish.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response,
                "Failed to create dish. Please check the form and try again.");
            return Result.Failure<DishDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating dish");
            return Result.Failure<DishDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating dish");
            return Result.Failure<DishDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<DishDTO>> UpdateAsync(DishUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync($"api/v1/dishes/{dto.Id}", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var dish = await response.Content.ReadFromJsonAsync<DishDTO>(cancellationToken);
                return dish is not null
                    ? Result.Success(dish)
                    : Result.Failure<DishDTO>("Failed to update dish.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response,
                "Failed to update dish. Please check the form and try again.");
            return Result.Failure<DishDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating dish {Id}", dto.Id);
            return Result.Failure<DishDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating dish {Id}", dto.Id);
            return Result.Failure<DishDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/dishes/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Dish {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete dish.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting dish {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting dish {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddToMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsync($"api/v1/menus/{menuId}/dishes/{dishId}", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Dish {DishId} added to menu {MenuId}", dishId, menuId);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to add dish to menu.");
            return Result.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding dish {DishId} to menu {MenuId}", dishId, menuId);
            return Result.Failure("An error occurred while adding the dish.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveFromMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/menus/{menuId}/dishes/{dishId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Dish {DishId} removed from menu {MenuId}", dishId, menuId);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to remove dish from menu.");
            return Result.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing dish {DishId} from menu {MenuId}", dishId, menuId);
            return Result.Failure("An error occurred while removing the dish.");
        }
    }
}
