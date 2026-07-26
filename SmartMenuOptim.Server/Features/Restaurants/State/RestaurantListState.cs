/*
 * File: RestaurantListState.cs
 * State container for RestaurantList component
 * Version: 1.0
 * .NET Target: .NET 9
 *
 * Purpose: Manages state for the RestaurantList page component.
 * Separates state management from UI rendering following Clean Architecture.
 *
 * Design Patterns:
 * - State Container Pattern: Centralized state for restaurant list
 * - Dependency Injection: Uses IRestaurantClientService for data operations
 */

using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Restaurants.State;

/// <summary>
/// State container for RestaurantList component.
/// Manages loading, error, data, and delete state for the restaurant list.
/// </summary>
/// <remarks>
/// <para><strong>Registration:</strong></para>
/// <para>Register as Scoped service in DI:</para>
/// <code>builder.Services.AddScoped&lt;RestaurantListState&gt;();</code>
/// </remarks>
public class RestaurantListState : ComponentStateBase<IReadOnlyList<RestaurantDTO>>
{
    private readonly IRestaurantClientService _restaurantService;
    private readonly ILogger<RestaurantListState> _logger;

    private bool _deleting;
    private RestaurantDTO? _restaurantToDelete;
    private bool _showDeleteModal;

    /// <summary>
    /// Initializes a new instance of RestaurantListState.
    /// </summary>
    /// <param name="restaurantService">The restaurant client service for data operations.</param>
    /// <param name="logger">The logger instance.</param>
    public RestaurantListState(
        IRestaurantClientService restaurantService,
        ILogger<RestaurantListState> logger)
    {
        _restaurantService = restaurantService;
        _logger = logger;
    }

    /// <summary>
    /// The current list of restaurants (alias for Data property).
    /// </summary>
    public IReadOnlyList<RestaurantDTO>? Restaurants => Data;

    /// <summary>
    /// Indicates whether a delete operation is in progress.
    /// </summary>
    public bool IsDeleting
    {
        get => _deleting;
        private set
        {
            _deleting = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// The restaurant currently targeted for deletion.
    /// </summary>
    public RestaurantDTO? RestaurantToDelete => _restaurantToDelete;

    /// <summary>
    /// Indicates whether the delete confirmation modal is visible.
    /// </summary>
    public bool ShowDeleteModal => _showDeleteModal;

    /// <summary>
    /// Loads all restaurants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        SetLoading();

        try
        {
            var result = await _restaurantService.GetAllAsync(cancellationToken);

            if (result.IsSuccess)
            {
                SetData(result.Value ?? (IReadOnlyList<RestaurantDTO>)[]);
                _logger.LogInformation("Loaded {Count} restaurants", Data?.Count ?? 0);
            }
            else
            {
                SetError(result.Error ?? "Failed to load restaurants.");
                _logger.LogWarning("Failed to load restaurants: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading restaurants");
            SetError("An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Shows the delete confirmation modal for a restaurant.
    /// </summary>
    /// <param name="restaurant">The restaurant to delete.</param>
    public void ConfirmDelete(RestaurantDTO restaurant)
    {
        _restaurantToDelete = restaurant;
        _showDeleteModal = true;
        NotifyStateChanged();
    }

    /// <summary>
    /// Cancels the delete operation and hides the modal.
    /// </summary>
    public void CancelDelete()
    {
        _restaurantToDelete = null;
        _showDeleteModal = false;
        NotifyStateChanged();
    }

    /// <summary>
    /// Deletes the currently targeted restaurant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_restaurantToDelete is null) return;

        IsDeleting = true;

        try
        {
            var result = await _restaurantService.DeleteAsync(_restaurantToDelete.Id, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Restaurant {Id} deleted successfully", _restaurantToDelete.Id);

                // Remove from local list and update state
                if (Data is not null)
                {
                    var updated = Data.Where(r => r.Id != _restaurantToDelete.Id).ToList();
                    SetData(updated);
                }

                CancelDelete();
            }
            else
            {
                SetError(result.Error ?? "Failed to delete restaurant. Please try again.");
                _logger.LogWarning("Failed to delete restaurant {Id}: {Error}",
                    _restaurantToDelete.Id, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting restaurant {Id}", _restaurantToDelete.Id);
            SetError("An error occurred while deleting the restaurant.");
        }
        finally
        {
            IsDeleting = false;
        }
    }

    /// <summary>
    /// Clears the current error message.
    /// </summary>
    public void ClearError()
    {
        if (HasError)
        {
            SetError(null!);
        }
    }
}
