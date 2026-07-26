using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Components;

/// <summary>
/// Code-behind for the MenuList page component.
/// Handles menu listing, status toggling, and delete operations.
/// </summary>
public partial class MenuList : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<MenuList> Logger { get; set; } = default!;

    [Parameter] public int RestaurantId { get; set; }

    private List<MenuDTO>? _menus;
    private bool _loading = true;
    private string? _error;

    // Delete state
    private bool _showDeleteModal;
    private MenuDTO? _menuToDelete;
    private bool _deleting;

    protected override async Task OnInitializedAsync()
    {
        await LoadMenusAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadMenusAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            _menus = await client.GetFromJsonAsync<List<MenuDTO>>($"api/v1/restaurants/{RestaurantId}/menus");
            Logger.LogInformation("Loaded {Count} menus for restaurant {RestaurantId}",
                _menus?.Count ?? 0, RestaurantId);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load menus for restaurant {RestaurantId}", RestaurantId);
            _error = "Unable to load menus. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading menus");
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STATUS TOGGLE
    // ═══════════════════════════════════════════════════════════════════════

    private async Task ToggleStatusAsync(MenuDTO menu)
    {
        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var newStatus = !menu.IsActive;

            var endpoint = newStatus
                ? $"api/v1/menus/{menu.Id}/activate"
                : $"api/v1/menus/{menu.Id}/deactivate";

            var response = await client.PostAsync(endpoint, null);

            if (response.IsSuccessStatusCode)
            {
                menu.IsActive = newStatus;
                Logger.LogInformation("Menu {MenuId} status toggled to {Status}", menu.Id, newStatus);
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to update menu status.");
                Logger.LogWarning("Failed to toggle menu {MenuId} status. Response: {StatusCode}",
                    menu.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling status for menu {MenuId}", menu.Id);
            _error = "An error occurred while updating status.";
        }
        finally
        {
            StateHasChanged();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ═══════════════════════════════════════════════════════════════════════

    private void CreateNew() => Navigation.NavigateTo($"/restaurants/{RestaurantId}/menus/new");

    private void Edit(int menuId) => Navigation.NavigateTo($"/restaurants/{RestaurantId}/menus/{menuId}/edit");

    private void ManageDishes(int menuId) => Navigation.NavigateTo($"/restaurants/{RestaurantId}/menus/{menuId}/dishes");

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ConfirmDelete(MenuDTO menu)
    {
        _menuToDelete = menu;
        _showDeleteModal = true;
    }

    private void CancelDelete()
    {
        _menuToDelete = null;
        _showDeleteModal = false;
    }

    private async Task DeleteMenuAsync()
    {
        if (_menuToDelete is null) return;

        _deleting = true;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var response = await client.DeleteAsync($"api/v1/menus/{_menuToDelete.Id}");

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Menu {MenuId} deleted", _menuToDelete.Id);
                _menus?.Remove(_menuToDelete);
                CancelDelete();
            }
            else
            {
                _error = await ApiErrorHelper.GetErrorMessageAsync(response, "Failed to delete menu.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting menu {MenuId}", _menuToDelete.Id);
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
}
