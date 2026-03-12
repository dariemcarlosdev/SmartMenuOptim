using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Dtos.Restaurant;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Components.Pages.Restaurant;

/// <summary>
/// Code-behind for the MenuEditor page component.
/// Handles menu creation and editing with availability hours configuration.
/// </summary>
public partial class MenuEditor : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<MenuEditor> Logger { get; set; } = default!;

    [Parameter] public int RestaurantId { get; set; }
    [Parameter] public int? MenuId { get; set; }

    private MenuFormModel _model = new();
    private bool _isEdit => MenuId.HasValue;
    private bool _saving;
    private bool _loadingMenu;
    private string? _error;

    // Availability state
    private bool _isAllDay = true;
    private string _availableFromStr = "09:00";
    private string _availableToStr = "21:00";

    protected override async Task OnInitializedAsync()
    {
        if (_isEdit)
        {
            await LoadMenuAsync();
        }
        else
        {
            _model = new MenuFormModel { IsActive = true };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadMenuAsync()
    {
        _loadingMenu = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var menu = await client.GetFromJsonAsync<MenuDTO>($"api/v1/menus/{MenuId}");

            if (menu is not null)
            {
                _model = new MenuFormModel
                {
                    Name = menu.Name,
                    Description = menu.Description,
                    MenuTypeId = menu.MenuTypeId,
                    IsActive = menu.IsActive
                };

                if (menu.AvailableFrom.HasValue && menu.AvailableTo.HasValue)
                {
                    _isAllDay = false;
                    _availableFromStr = menu.AvailableFrom.Value.ToString(@"hh\:mm");
                    _availableToStr = menu.AvailableTo.Value.ToString(@"hh\:mm");
                }

                Logger.LogInformation("Loaded menu {MenuId} for editing", MenuId);
            }
            else
            {
                _error = "Menu not found.";
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load menu {MenuId}", MenuId);
            _error = "Unable to load menu. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading menu {MenuId}", MenuId);
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loadingMenu = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // AVAILABILITY HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void ToggleAllDay(ChangeEventArgs e)
    {
        _isAllDay = (bool)(e.Value ?? true);
    }

    private void SetAvailability(string from, string to)
    {
        _isAllDay = false;
        _availableFromStr = from;
        _availableToStr = to;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORM SUBMIT
    // ═══════════════════════════════════════════════════════════════════════

    private async Task HandleSubmitAsync()
    {
        _saving = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");

            TimeSpan? availableFrom = null;
            TimeSpan? availableTo = null;

            if (!_isAllDay)
            {
                if (TimeSpan.TryParse(_availableFromStr, out var from))
                    availableFrom = from;
                if (TimeSpan.TryParse(_availableToStr, out var to))
                    availableTo = to;
            }

            HttpResponseMessage response;

            if (_isEdit)
            {
                var updateDto = new MenuUpdateDTO
                {
                    Id = MenuId!.Value,
                    Name = _model.Name,
                    Description = _model.Description,
                    MenuTypeId = _model.MenuTypeId,
                    AvailableFrom = availableFrom,
                    AvailableTo = availableTo,
                    IsActive = _model.IsActive
                };
                response = await client.PutAsJsonAsync($"api/v1/menus/{MenuId}", updateDto);
                Logger.LogInformation("Updating menu {MenuId}, IsActive: {IsActive}", MenuId, _model.IsActive);
            }
            else
            {
                var createDto = new MenuCreateDTO
                {
                    Name = _model.Name,
                    Description = _model.Description,
                    MenuTypeId = _model.MenuTypeId,
                    AvailableFrom = availableFrom,
                    AvailableTo = availableTo,
                    IsActive = _model.IsActive
                };
                response = await client.PostAsJsonAsync($"api/v1/restaurants/{RestaurantId}/menus", createDto);
                Logger.LogInformation("Creating new menu for restaurant {RestaurantId}, IsActive: {IsActive}",
                    RestaurantId, _model.IsActive);
            }

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Menu {Action} successfully", _isEdit ? "updated" : "created");
                Navigation.NavigateTo($"/restaurants/{RestaurantId}/menus");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Failed to {Action} menu. Status: {Status}, Error: {Error}",
                    _isEdit ? "update" : "create", response.StatusCode, errorContent);
                _error = await ApiErrorHelper.GetErrorMessageAsync(response,
                    $"Failed to {(_isEdit ? "update" : "create")} menu. Please check the form and try again.");
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error {Action} menu", _isEdit ? "updating" : "creating");
            _error = "Unable to connect to the server. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error {Action} menu", _isEdit ? "updating" : "creating");
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

    private void Cancel() => Navigation.NavigateTo($"/restaurants/{RestaurantId}/menus");

    // ═══════════════════════════════════════════════════════════════════════
    // FORM MODEL
    // ═══════════════════════════════════════════════════════════════════════

    private class MenuFormModel
    {
        [Required(ErrorMessage = "Menu name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? MenuTypeId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
