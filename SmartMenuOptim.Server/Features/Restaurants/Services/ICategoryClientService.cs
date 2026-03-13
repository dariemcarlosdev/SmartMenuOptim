using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;

namespace SmartMenuOptim.Server.Features.Restaurants.Services;

/// <summary>
/// Defines the contract for Category operations from the Blazor Server client.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Note:</strong></para>
/// <para>This interface adapts the Application layer's ICategoryService for use
/// in the Blazor Server project, communicating via HTTP with the backend API.</para>
/// </remarks>
public interface ICategoryClientService
{
    /// <summary>
    /// Retrieves all categories for a restaurant.
    /// </summary>
    Task<Result<IReadOnlyList<CategoryDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category for a restaurant.
    /// </summary>
    Task<Result<CategoryDTO>> CreateAsync(int restaurantId, CategoryCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    Task<Result<CategoryDTO>> UpdateAsync(CategoryUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders categories within a restaurant.
    /// </summary>
    Task<Result> ReorderAsync(int restaurantId, Dictionary<int, int> categoryOrders, CancellationToken cancellationToken = default);
}
