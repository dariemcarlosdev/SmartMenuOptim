using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Components;

/// <summary>
/// Code-behind for the CategoryList page component.
/// Handles category CRUD, drag-and-drop reordering, and delete operations.
/// </summary>
public partial class CategoryList : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<CategoryList> Logger { get; set; } = default!;

    [Parameter] public int RestaurantId { get; set; }

    private List<CategoryDTO>? _categories;
    private bool _loading = true;
    private string? _error;
    private string? _successMessage;

    // Form state
    private bool _showForm;
    private CategoryDTO? _editingCategory;
    private CategoryFormModel _formModel = new();
    private bool _saving;

    // Delete state
    private bool _showDeleteModal;
    private CategoryDTO? _categoryToDelete;
    private bool _deleting;

    // Drag and drop state
    private CategoryDTO? _draggedCategory;
    private bool _reordering;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategoriesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadCategoriesAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            _categories = await client.GetFromJsonAsync<List<CategoryDTO>>($"api/v1/restaurants/{RestaurantId}/categories");
            Logger.LogInformation("Loaded {Count} categories for restaurant {RestaurantId}",
                _categories?.Count ?? 0, RestaurantId);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load categories for restaurant {RestaurantId}", RestaurantId);
            _error = "Unable to load categories. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading categories");
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DRAG AND DROP HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void HandleDragStart(CategoryDTO category)
    {
        _draggedCategory = category;
        Logger.LogDebug("Started dragging category: {CategoryName}", category.Name);
    }

    private void HandleDragOver(DragEventArgs e)
    {
        // Allow drop
    }

    private async Task HandleDropAsync(CategoryDTO targetCategory)
    {
        if (_draggedCategory is null || _categories is null || _draggedCategory.Id == targetCategory.Id)
        {
            return;
        }

        Logger.LogInformation("Dropping category {DraggedName} onto {TargetName}",
            _draggedCategory.Name, targetCategory.Name);

        var orderedCategories = _categories.OrderBy(c => c.DisplayOrder).ToList();
        var draggedIndex = orderedCategories.FindIndex(c => c.Id == _draggedCategory.Id);
        var targetIndex = orderedCategories.FindIndex(c => c.Id == targetCategory.Id);

        if (draggedIndex == -1 || targetIndex == -1)
        {
            return;
        }

        var draggedItem = orderedCategories[draggedIndex];
        orderedCategories.RemoveAt(draggedIndex);
        orderedCategories.Insert(targetIndex, draggedItem);

        var categoryOrders = new Dictionary<int, int>();
        for (int i = 0; i < orderedCategories.Count; i++)
        {
            var newOrder = i + 1;
            categoryOrders[orderedCategories[i].Id] = newOrder;
            orderedCategories[i].DisplayOrder = newOrder;
        }

        _categories = orderedCategories;
        StateHasChanged();

        await ReorderCategoriesAsync(categoryOrders);

        _draggedCategory = null;
    }

    private void HandleDragEnd()
    {
        _draggedCategory = null;
    }

    private async Task ReorderCategoriesAsync(Dictionary<int, int> categoryOrders)
    {
        _reordering = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.PutAsJsonAsync(
                $"api/v1/restaurants/{RestaurantId}/categories/reorder",
                categoryOrders);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Categories reordered successfully for restaurant {RestaurantId}", RestaurantId);
                _successMessage = "Category order saved successfully!";

                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    _successMessage = null;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to save category order.");
                await LoadCategoriesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error reordering categories");
            _error = "An error occurred while saving the category order.";
            await LoadCategoriesAsync();
        }
        finally
        {
            _reordering = false;
        }
    }

    private string GetRowClass(CategoryDTO category)
    {
        var classes = new List<string>();

        if (!category.IsActive)
        {
            classes.Add("table-secondary");
        }

        if (_draggedCategory?.Id == category.Id)
        {
            classes.Add("opacity-50");
        }

        return string.Join(" ", classes);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORM HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ShowAddForm()
    {
        _editingCategory = null;
        _formModel = new CategoryFormModel { IsActive = true, DisplayOrder = (_categories?.Count ?? 0) + 1 };
        _showForm = true;
    }

    private void EditCategory(CategoryDTO category)
    {
        _editingCategory = category;
        _formModel = new CategoryFormModel
        {
            Name = category.Name,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };
        _showForm = true;
    }

    private void CancelForm()
    {
        _showForm = false;
        _editingCategory = null;
        _formModel = new();
    }

    private async Task SaveCategoryAsync()
    {
        _saving = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            HttpResponseMessage response;

            if (_editingCategory is not null)
            {
                var updateDto = new
                {
                    Id = _editingCategory.Id,
                    Name = _formModel.Name,
                    Description = _formModel.Description,
                    DisplayOrder = _formModel.DisplayOrder,
                    IsActive = _formModel.IsActive
                };
                response = await client.PutAsJsonAsync($"api/v1/categories/{_editingCategory.Id}", updateDto);
            }
            else
            {
                var createDto = new
                {
                    Name = _formModel.Name,
                    Description = _formModel.Description,
                    DisplayOrder = _formModel.DisplayOrder,
                    IsActive = _formModel.IsActive,
                    RestaurantId = RestaurantId
                };
                response = await client.PostAsJsonAsync($"api/v1/restaurants/{RestaurantId}/categories", createDto);
            }

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Category {Action} successfully", _editingCategory is null ? "created" : "updated");
                _successMessage = _editingCategory is null ? "Category created!" : "Category updated!";
                CancelForm();
                await LoadCategoriesAsync();

                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    _successMessage = null;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to save category. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving category");
            _error = "An error occurred while saving.";
        }
        finally
        {
            _saving = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmDelete(CategoryDTO category)
    {
        _categoryToDelete = category;
        _showDeleteModal = true;
    }

    private void CancelDelete()
    {
        _categoryToDelete = null;
        _showDeleteModal = false;
    }

    private async Task DeleteCategoryAsync()
    {
        if (_categoryToDelete is null) return;

        _deleting = true;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/categories/{_categoryToDelete.Id}");

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Category {Id} deleted", _categoryToDelete.Id);
                _categories?.Remove(_categoryToDelete);
                _successMessage = "Category deleted!";
                CancelDelete();

                _ = Task.Delay(3000).ContinueWith(_ =>
                {
                    _successMessage = null;
                    InvokeAsync(StateHasChanged);
                });
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete category.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting category");
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
    // FORM MODEL
    // ═══════════════════════════════════════════════════════════════════════

    private class CategoryFormModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
