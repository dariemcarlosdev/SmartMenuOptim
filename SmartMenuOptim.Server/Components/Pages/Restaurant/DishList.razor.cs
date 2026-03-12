using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Components.Pages.Restaurant;

/// <summary>
/// Code-behind for the DishList page component.
/// Handles dish listing, category filtering, menu dish management, and delete operations.
/// </summary>
public partial class DishList : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<DishList> Logger { get; set; } = default!;

    [Parameter] public int RestaurantId { get; set; }
    [Parameter] public int? MenuId { get; set; }

    private List<DishListItemDTO>? _dishes;
    private List<DishListItemDTO>? _filteredDishes;
    private List<CategoryDTO>? _categories;
    private List<DishListItemDTO>? _availableDishes;
    private int? _selectedCategoryId;
    private bool _loading = true;
    private string? _error;

    // Delete state
    private bool _showDeleteModal;
    private DishListItemDTO? _dishToDelete;
    private bool _deleting;

    // Add to menu state
    private bool _showAddToMenuModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadDataAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");

            _categories = await client.GetFromJsonAsync<List<CategoryDTO>>(
                $"api/v1/restaurants/{RestaurantId}/categories");

            if (MenuId.HasValue)
            {
                _dishes = await client.GetFromJsonAsync<List<DishListItemDTO>>(
                    $"api/v1/menus/{MenuId}/dishes");
            }
            else
            {
                _dishes = await client.GetFromJsonAsync<List<DishListItemDTO>>(
                    $"api/v1/restaurants/{RestaurantId}/dishes");
            }

            _filteredDishes = _dishes;
            Logger.LogInformation("Loaded {Count} dishes", _dishes?.Count ?? 0);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load dishes");
            _error = "Unable to load dishes. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading dishes");
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FILTERING
    // ═══════════════════════════════════════════════════════════════════════

    private void FilterByCategory(int? categoryId)
    {
        _selectedCategoryId = categoryId;

        if (categoryId is null)
        {
            _filteredDishes = _dishes;
        }
        else
        {
            _filteredDishes = _dishes?.Where(d => d.CategoryId == categoryId).ToList();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    private void CreateNew() => Navigation.NavigateTo($"/restaurants/{RestaurantId}/dishes/new");

    private void Edit(int dishId) => Navigation.NavigateTo($"/restaurants/{RestaurantId}/dishes/{dishId}/edit");

    // ═══════════════════════════════════════════════════════════════════════
    // MENU DISH MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    private async void ShowAddDishModal()
    {
        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");

            var allDishes = await client.GetFromJsonAsync<List<DishListItemDTO>>(
                $"api/v1/restaurants/{RestaurantId}/dishes");

            var menuDishIds = _dishes?.Select(d => d.Id).ToHashSet() ?? new HashSet<int>();
            _availableDishes = allDishes?.Where(d => !menuDishIds.Contains(d.Id)).ToList();

            _showAddToMenuModal = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading available dishes");
            _error = "Failed to load available dishes.";
        }
    }

    private async Task AddToMenuAsync(DishListItemDTO dish)
    {
        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.PostAsync($"api/v1/menus/{MenuId}/dishes/{dish.Id}", null);

            if (response.IsSuccessStatusCode)
            {
                _dishes?.Add(dish);
                _filteredDishes = _dishes;
                _availableDishes?.Remove(dish);
                Logger.LogInformation("Dish {DishId} added to menu {MenuId}", dish.Id, MenuId);
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to add dish to menu.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding dish {DishId} to menu", dish.Id);
            _error = "An error occurred while adding the dish.";
        }
    }

    private async Task RemoveFromMenu(DishListItemDTO dish)
    {
        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/menus/{MenuId}/dishes/{dish.Id}");

            if (response.IsSuccessStatusCode)
            {
                _dishes?.Remove(dish);
                _filteredDishes = _dishes;
                Logger.LogInformation("Dish {DishId} removed from menu {MenuId}", dish.Id, MenuId);
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to remove dish from menu.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing dish {DishId} from menu", dish.Id);
            _error = "An error occurred while removing the dish.";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmDelete(DishListItemDTO dish)
    {
        _dishToDelete = dish;
        _showDeleteModal = true;
    }

    private void CancelDelete()
    {
        _dishToDelete = null;
        _showDeleteModal = false;
    }

    private async Task DeleteDishAsync()
    {
        if (_dishToDelete is null) return;

        _deleting = true;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/dishes/{_dishToDelete.Id}");

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Dish {DishId} deleted", _dishToDelete.Id);
                _dishes?.Remove(_dishToDelete);
                _filteredDishes = _dishes;
                CancelDelete();
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete dish.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting dish {DishId}", _dishToDelete.Id);
            _error = "An error occurred while deleting.";
        }
        finally
        {
            _deleting = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NESTED TYPES
    // ═══════════════════════════════════════════════════════════════════════

    private class DishListItemDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public bool IsVegetarian { get; set; }
        public bool IsSpicy { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
