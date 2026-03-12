using System.Net.Http.Json;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Restaurant;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// HTTP client-based implementation for Menu operations.
/// </summary>
public class MenuClientService : IMenuClientService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MenuClientService> _logger;

    public MenuClientService(IHttpClientFactory httpClientFactory, ILogger<MenuClientService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<MenuDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.GetAsync($"api/v1/menus/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var menu = await response.Content.ReadFromJsonAsync<MenuDTO>(cancellationToken);
                return menu is not null
                    ? Result.Success(menu)
                    : Result.Failure<MenuDTO>("Menu not found.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to load menu.");
            return Result.Failure<MenuDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading menu {Id}", id);
            return Result.Failure<MenuDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading menu {Id}", id);
            return Result.Failure<MenuDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MenuDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var menus = await client.GetFromJsonAsync<List<MenuDTO>>(
                $"api/v1/restaurants/{restaurantId}/menus", cancellationToken);
            return Result.Success<IReadOnlyList<MenuDTO>>(menus ?? []);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading menus for restaurant {RestaurantId}", restaurantId);
            return Result.Failure<IReadOnlyList<MenuDTO>>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading menus");
            return Result.Failure<IReadOnlyList<MenuDTO>>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<MenuDTO>> CreateAsync(int restaurantId, MenuCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsJsonAsync($"api/v1/restaurants/{restaurantId}/menus", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var menu = await response.Content.ReadFromJsonAsync<MenuDTO>(cancellationToken);
                return menu is not null
                    ? Result.Success(menu)
                    : Result.Failure<MenuDTO>("Failed to create menu.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to create menu.");
            return Result.Failure<MenuDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating menu");
            return Result.Failure<MenuDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating menu");
            return Result.Failure<MenuDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<MenuDTO>> UpdateAsync(MenuUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync($"api/v1/menus/{dto.Id}", dto, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var menu = await response.Content.ReadFromJsonAsync<MenuDTO>(cancellationToken);
                return menu is not null
                    ? Result.Success(menu)
                    : Result.Failure<MenuDTO>("Failed to update menu.");
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update menu.");
            return Result.Failure<MenuDTO>(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating menu {Id}", dto.Id);
            return Result.Failure<MenuDTO>("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating menu {Id}", dto.Id);
            return Result.Failure<MenuDTO>("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/menus/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Menu {Id} deleted", id);
                return Result.Success();
            }

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete menu.");
            return Result.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error deleting menu {Id}", id);
            return Result.Failure("Unable to connect to the server. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting menu {Id}", id);
            return Result.Failure("An unexpected error occurred.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsync($"api/v1/menus/{id}/activate", null, cancellationToken);

            if (response.IsSuccessStatusCode) return Result.Success();

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to activate menu.");
            return Result.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating menu {Id}", id);
            return Result.Failure("An error occurred while updating status.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsync($"api/v1/menus/{id}/deactivate", null, cancellationToken);

            if (response.IsSuccessStatusCode) return Result.Success();

            var error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to deactivate menu.");
            return Result.Failure(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating menu {Id}", id);
            return Result.Failure("An error occurred while updating status.");
        }
    }
}
