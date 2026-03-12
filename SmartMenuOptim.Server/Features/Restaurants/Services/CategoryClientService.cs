using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// HTTP client-based implementation for Category operations.
/// </summary>
public class CategoryClientService : ICategoryClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CategoryClientService> _logger;

    public CategoryClientService(IHttpClientFactory httpClientFactory, ILogger<CategoryClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CategoryDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var categories = await client.GetFromJsonAsync<List<CategoryDTO>>(
                $"api/v1/restaurants/{restaurantId}/categories", cancellationToken);
            return Result.Success<IReadOnlyList<CategoryDTO>>(categories ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading categories for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<CategoryDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading categories");
            return Result.Failure<IReadOnlyList<CategoryDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDTO>> CreateAsync(int restaurantId, CategoryCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync($"api/v1/restaurants/{restaurantId}/categories", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var category = await response.Content.ReadFromJsonAsync<CategoryDTO>(cancellationToken);
                return category is not null
                    ? Result.Success(category)
                    : Result.Failure<CategoryDTO>("Failed to create category.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to create category.");
            return Result.Failure<CategoryDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating category");
            return Result.Failure<CategoryDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating category");
            return Result.Failure<CategoryDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDTO>> UpdateAsync(CategoryUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync($"api/v1/categories/{dto.Id}", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var category = await response.Content.ReadFromJsonAsync<CategoryDTO>(cancellationToken);
                return category is not null
                    ? Result.Success(category)
                    : Result.Failure<CategoryDTO>("Failed to update category.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update category.");
            return Result.Failure<CategoryDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating category {Id}", dto.Id);
            return Result.Failure<CategoryDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating category {Id}", dto.Id);
            return Result.Failure<CategoryDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/categories/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Category {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete category.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting category {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting category {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ReorderAsync(int restaurantId, Dictionary<int, int> categoryOrders, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync(
                $"api/v1/restaurants/{restaurantId}/categories/reorder", categoryOrders, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Categories reordered for restaurant {RestaurantId}", restaurantId);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to save category order.");
            return Result.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories for restaurant {RestaurantId}", restaurantId);
            return Result.Failure("An error occurred while saving the category order.");
        }
    }
}
