using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Components.Pages.Restaurant;

/// <summary>
/// Code-behind for the DishForm page component.
/// Handles dish creation and editing with category selection and dietary info.
/// </summary>
public partial class DishForm : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<DishForm> Logger { get; set; } = default!;

    [Parameter] public int RestaurantId { get; set; }
    [Parameter] public int? DishId { get; set; }

    private DishFormModel _model = new();
    private List<CategoryDTO>? _categories;
    private bool _isEdit => DishId.HasValue;
    private bool _saving;
    private bool _loadingDish;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategoriesAsync();

        if (_isEdit)
        {
            await LoadDishAsync();
        }
        else
        {
            _model = new DishFormModel { IsActive = true };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            _categories = await client.GetFromJsonAsync<List<CategoryDTO>>(
                $"api/v1/restaurants/{RestaurantId}/categories");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load categories");
            _error = "Failed to load categories.";
        }
    }

    private async Task LoadDishAsync()
    {
        _loadingDish = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var dish = await client.GetFromJsonAsync<DishDetailDTO>($"api/v1/dishes/{DishId}");

            if (dish is not null)
            {
                _model = new DishFormModel
                {
                    Name = dish.Name,
                    Description = dish.Description,
                    CategoryId = dish.CategoryId,
                    Price = dish.Price,
                    Calories = dish.Calories,
                    IsVegetarian = dish.IsVegetarian,
                    IsSpicy = dish.IsSpicy,
                    Ingredients = dish.Ingredients,
                    IsActive = dish.IsActive
                };
                Logger.LogInformation("Loaded dish {DishId} for editing", DishId);
            }
            else
            {
                _error = "Dish not found.";
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load dish {DishId}", DishId);
            _error = "Unable to load dish. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading dish {DishId}", DishId);
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loadingDish = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORM SUBMIT
    // ═══════════════════════════════════════════════════════════════════════

    private async Task HandleSubmitAsync()
    {
        if (_model.CategoryId <= 0)
        {
            _error = "Please select a category.";
            return;
        }

        _saving = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            HttpResponseMessage response;

            if (_isEdit)
            {
                var updateDto = new
                {
                    Id = DishId!.Value,
                    RestaurantId = RestaurantId,
                    Name = _model.Name,
                    Description = _model.Description,
                    CategoryId = _model.CategoryId,
                    DishPrice = _model.Price,
                    Calories = _model.Calories,
                    IsVegetarian = _model.IsVegetarian,
                    IsSpicy = _model.IsSpicy,
                    Ingredients = _model.Ingredients,
                    IsActive = _model.IsActive
                };
                response = await client.PutAsJsonAsync($"api/v1/dishes/{DishId}", updateDto);
                Logger.LogInformation("Updating dish {DishId}", DishId);
            }
            else
            {
                var createDto = new
                {
                    RestaurantId = RestaurantId,
                    Name = _model.Name,
                    Description = _model.Description,
                    CategoryId = _model.CategoryId,
                    DishPrice = _model.Price,
                    Calories = _model.Calories,
                    IsVegetarian = _model.IsVegetarian,
                    IsSpicy = _model.IsSpicy,
                    Ingredients = _model.Ingredients,
                    IsActive = _model.IsActive
                };
                response = await client.PostAsJsonAsync($"api/v1/restaurants/{RestaurantId}/dishes", createDto);
                Logger.LogInformation("Creating new dish for restaurant {RestaurantId}", RestaurantId);
            }

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Dish {Action} successfully", _isEdit ? "updated" : "created");
                Navigation.NavigateTo($"/restaurants/{RestaurantId}/dishes");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Failed to {Action} dish. Status: {Status}, Error: {Error}",
                    _isEdit ? "update" : "create", response.StatusCode, errorContent);
                _error = await ApiErrorHelper.GetErrorMessageAsync(response,
                    $"Failed to {(_isEdit ? "update" : "create")} dish. Please check the form and try again.");
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error {Action} dish", _isEdit ? "updating" : "creating");
            _error = "Unable to connect to the server. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error {Action} dish", _isEdit ? "updating" : "creating");
            _error = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            _saving = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    private void Cancel() => Navigation.NavigateTo($"/restaurants/{RestaurantId}/dishes");

    // ═══════════════════════════════════════════════════════════════════════
    // NESTED TYPES
    // ═══════════════════════════════════════════════════════════════════════

    private class DishFormModel
    {
        [Required(ErrorMessage = "Dish name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be 2-200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        [Range(0.01, 10000, ErrorMessage = "Price must be between $0.01 and $10,000")]
        public decimal Price { get; set; }

        [Range(0, 10000)]
        public int? Calories { get; set; }

        public bool IsVegetarian { get; set; }
        public bool IsSpicy { get; set; }
        public string? Ingredients { get; set; }
        public bool IsActive { get; set; } = true;
    }

    private class DishDetailDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public int? Calories { get; set; }
        public bool IsVegetarian { get; set; }
        public bool IsSpicy { get; set; }
        public string? Ingredients { get; set; }
        public bool IsActive { get; set; }
    }
}
