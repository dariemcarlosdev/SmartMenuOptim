using Microsoft.AspNetCore.Components;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Helpers;

namespace SmartMenuOptim.Server.Features.Restaurants.Components;

/// <summary>
/// Code-behind for the RestaurantForm page component.
/// Handles restaurant creation and editing with address, contact, and business hours.
/// </summary>
public partial class RestaurantForm : ComponentBase
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<RestaurantForm> Logger { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    private RestaurantCreateDTO _model = new()
    {
        OwnerId = 1, // MVP: Default owner
        Address = new AddressDTO { CountryCode = "US" },
        MaxSimultaneousOrders = 50,
        TimeZoneId = "UTC"
    };

    private bool _isEdit => Id.HasValue;
    private bool _saving;
    private bool _loadingRestaurant;
    private string? _error;
    private bool _currentIsAcceptingOrders;
    private List<BusinessHoursEntry> _businessHoursEntries = [];

    protected override async Task OnInitializedAsync()
    {
        InitializeBusinessHoursEntries();

        if (_isEdit)
        {
            await LoadRestaurantAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    private void InitializeBusinessHoursEntries()
    {
        _businessHoursEntries = Enum.GetValues<DayOfWeek>()
            .Select(day => new BusinessHoursEntry
            {
                DayOfWeek = day,
                IsClosed = true,
                OpenTimeString = "09:00",
                CloseTimeString = "22:00"
            })
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DATA LOADING
    // ═══════════════════════════════════════════════════════════════════════

    private async Task LoadRestaurantAsync()
    {
        _loadingRestaurant = true;
        _error = null;

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            var restaurant = await client.GetFromJsonAsync<RestaurantDTO>($"api/v1/restaurants/{Id}");

            if (restaurant is not null)
            {
                _model = new RestaurantCreateDTO
                {
                    Name = restaurant.Name,
                    Description = restaurant.Description,
                    Email = restaurant.Email,
                    PhoneNumber = restaurant.PhoneNumber,
                    Address = restaurant.Address ?? new AddressDTO { CountryCode = "US" },
                    OwnerId = restaurant.OwnerId,
                    TimeZoneId = restaurant.TimeZoneId ?? "UTC",
                    MaxSimultaneousOrders = restaurant.MaxSimultaneousOrders
                };
                _currentIsAcceptingOrders = restaurant.IsAcceptingOrders;

                if (restaurant.BusinessHours?.Count > 0)
                {
                    foreach (var bh in restaurant.BusinessHours)
                    {
                        var entry = _businessHoursEntries.FirstOrDefault(e => e.DayOfWeek == bh.DayOfWeek);
                        if (entry is not null)
                        {
                            entry.IsClosed = bh.IsClosed;
                            entry.OpenTimeString = bh.OpenTime.ToString(@"hh\:mm");
                            entry.CloseTimeString = bh.CloseTime.ToString(@"hh\:mm");
                        }
                    }
                }

                Logger.LogInformation("Loaded restaurant {Id} for editing", Id);
            }
            else
            {
                _error = "Restaurant not found.";
                Logger.LogWarning("Restaurant {Id} not found", Id);
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Failed to load restaurant {Id}", Id);
            _error = "Unable to load restaurant. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error loading restaurant {Id}", Id);
            _error = "An unexpected error occurred.";
        }
        finally
        {
            _loadingRestaurant = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FORM SUBMIT
    // ═══════════════════════════════════════════════════════════════════════

    private async Task HandleSubmitAsync()
    {
        _saving = true;
        _error = null;
        SyncBusinessHoursToModel();

        try
        {
            var client = HttpClientFactory.CreateClient("BackendAPI");
            HttpResponseMessage response;

            if (_isEdit)
            {
                var updateDto = new RestaurantUpdateDTO
                {
                    Id = Id!.Value,
                    Name = _model.Name,
                    Description = _model.Description,
                    Email = _model.Email,
                    PhoneNumber = _model.PhoneNumber,
                    Address = _model.Address,
                    OwnerId = _model.OwnerId,
                    TimeZoneId = _model.TimeZoneId,
                    MaxSimultaneousOrders = _model.MaxSimultaneousOrders,
                    IsAcceptingOrders = _currentIsAcceptingOrders,
                    BusinessHours = _model.BusinessHours
                };
                response = await client.PutAsJsonAsync($"api/v1/restaurants/{Id}", updateDto);
                Logger.LogInformation("Updating restaurant {Id}", Id);
            }
            else
            {
                response = await client.PostAsJsonAsync("api/v1/restaurants", _model);
                Logger.LogInformation("Creating new restaurant");
            }

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("Restaurant {Action} successfully", _isEdit ? "updated" : "created");
                Navigation.NavigateTo("/restaurants");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogWarning("Failed to {Action} restaurant. Status: {Status}, Error: {Error}",
                    _isEdit ? "update" : "create", response.StatusCode, errorContent);
                _error = await ApiErrorHelper.GetErrorMessageAsync(response,
                    $"Failed to {(_isEdit ? "update" : "create")} restaurant. Please check the form and try again.");
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Network error {Action} restaurant", _isEdit ? "updating" : "creating");
            _error = "Unable to connect to the server. Please try again.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error {Action} restaurant", _isEdit ? "updating" : "creating");
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

    private void Cancel() => Navigation.NavigateTo("/restaurants");

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void SyncBusinessHoursToModel()
    {
        _model.BusinessHours = _businessHoursEntries
            .Select(e => new BusinessHoursDTO
            {
                DayOfWeek = e.DayOfWeek,
                IsClosed = e.IsClosed,
                OpenTime = TimeSpan.TryParse(e.OpenTimeString, out var open) ? open : TimeSpan.Zero,
                CloseTime = TimeSpan.TryParse(e.CloseTimeString, out var close) ? close : TimeSpan.Zero
            })
            .ToList();
    }

    private void ToggleClosed(BusinessHoursEntry entry, bool isClosed)
    {
        entry.IsClosed = isClosed;
    }

    private void UpdateOpenTime(BusinessHoursEntry entry, ChangeEventArgs e)
    {
        entry.OpenTimeString = e.Value?.ToString() ?? "09:00";
    }

    private void UpdateCloseTime(BusinessHoursEntry entry, ChangeEventArgs e)
    {
        entry.CloseTimeString = e.Value?.ToString() ?? "22:00";
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

    /// <summary>
    /// Helper class for two-way binding of business hours in the form.
    /// </summary>
    private class BusinessHoursEntry
    {
        public DayOfWeek DayOfWeek { get; set; }
        public bool IsClosed { get; set; } = true;
        public string OpenTimeString { get; set; } = "09:00";
        public string CloseTimeString { get; set; } = "22:00";
    }
}
