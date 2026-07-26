/*
 * File: RestaurantDetailState.cs
 * State container for RestaurantDetail component
 * Version: 1.0
 * .NET Target: .NET 9
 * 
 * Purpose: Manages state for the RestaurantDetail page component.
 * Separates state management from UI rendering following Clean Architecture. This is useful for complex components that require loading data, handling errors, and managing user interactions (like toggling restaurant status) without cluttering the component code.
 * 
 * Design Patterns:
 * - State Container Pattern: Centralized state for restaurant details
 * - Dependency Injection: Uses IRestaurantClientService for data operations
 */

using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Restaurants.State;

/// <summary>
/// State container for RestaurantDetail component.
/// Manages loading, error, and data state for restaurant details.
/// </summary>
/// <remarks>
/// <para><strong>Registration:</strong></para>
/// <para>Register as Scoped service in Program.cs:</para>
/// <code>builder.Services.AddScoped&lt;RestaurantDetailState&gt;();</code>
/// </remarks>
public class RestaurantDetailState : ComponentStateBase<RestaurantDTO>
{
    private readonly IRestaurantClientService _restaurantService;
    private readonly ILogger<RestaurantDetailState> _logger;
    private bool _togglingStatus;

    /// <summary>
    /// Initializes a new instance of RestaurantDetailState.
    /// </summary>
    /// <param name="restaurantService">The restaurant client service for data operations.</param>
    /// <param name="logger">The logger instance.</param>
    public RestaurantDetailState(
        IRestaurantClientService restaurantService,
        ILogger<RestaurantDetailState> logger)
    {
        _restaurantService = restaurantService;
        _logger = logger;
    }

    /// <summary>
    /// Indicates whether a status toggle operation is in progress.
    /// </summary>
    public bool IsTogglingStatus
    {
        get => _togglingStatus;
        private set
        {
            _togglingStatus = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// The current restaurant data (alias for Data property for clarity).
    /// </summary>
    public RestaurantDTO? Restaurant => Data;

    /// <summary>
    /// Loads restaurant data by ID.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(int id, CancellationToken cancellationToken = default)
    {
        SetLoading();

        try
        {
            var result = await _restaurantService.GetByIdAsync(id, cancellationToken);

            if (result.IsSuccess && result.Value is not null)
            {
                SetData(result.Value);
                _logger.LogInformation("Loaded restaurant {Id}", id);
            }
            else
            {
                SetError(result.Error ?? "Restaurant not found.");
                _logger.LogWarning("Failed to load restaurant {Id}: {Error}", id, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading restaurant {Id}", id);
            SetError("An unexpected error occurred while loading the restaurant.");
        }
    }

    /// <summary>
    /// Toggles the restaurant's accepting orders status.
    /// </summary>
    /// <param name="id">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if toggle was successful, false otherwise.</returns>
    public async Task<bool> ToggleStatusAsync(int id, CancellationToken cancellationToken = default)
    {
        if (Restaurant is null) return false;

        IsTogglingStatus = true;

        try
        {
            var newStatus = !Restaurant.IsAcceptingOrders;
            var result = await _restaurantService.ToggleAcceptingOrdersAsync(id, newStatus, cancellationToken);

            if (result.IsSuccess)
            {
                Restaurant.IsAcceptingOrders = newStatus;
                _logger.LogInformation("Restaurant {Id} status toggled to {Status}", id, newStatus);
                NotifyStateChanged();
                return true;
            }
            else
            {
                SetError(result.Error ?? "Failed to update status. Please try again.");
                _logger.LogWarning("Failed to toggle status for restaurant {Id}: {Error}", id, result.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling status for restaurant {Id}", id);
            SetError("An error occurred while updating status.");
            return false;
        }
        finally
        {
            IsTogglingStatus = false;
        }
    }

    /// <summary>
    /// Clears any current error message.
    /// </summary>
    public void ClearError()
    {
        if (HasError)
        {
            SetError(null!);
        }
    }
}
