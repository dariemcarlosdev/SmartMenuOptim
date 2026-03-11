/*
 * File: ICategoryService.cs
 * Service interface for DishCategory entity operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Defines the contract for Category management operations
 * following Clean Architecture and Interface Segregation Principle (ISP).
 */

using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Dish;

namespace SmartMenuOptim.Application.Services.Restaurant;

/// <summary>
/// Defines the contract for DishCategory entity operations.
/// </summary>
/// <remarks>
/// <para><strong>Interface Segregation Principle:</strong></para>
/// <para>This interface is focused solely on Category operations.</para>
/// </remarks>
public interface ICategoryService
{
    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves a category by its unique identifier.
    /// </summary>
    /// <param name="id">The category identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the CategoryDTO if found.</returns>
    Task<Result<CategoryDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all categories for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing a list of CategoryDTOs.</returns>
    Task<Result<IReadOnlyList<CategoryDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new category for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="dto">The category creation data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the created CategoryDTO.</returns>
    Task<Result<CategoryDTO>> CreateAsync(int restaurantId, CategoryCreateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="dto">The category update data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result containing the updated CategoryDTO.</returns>
    Task<Result<CategoryDTO>> UpdateAsync(CategoryUpdateDTO dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a category.
    /// </summary>
    /// <param name="id">The category identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders categories within a restaurant.
    /// </summary>
    /// <param name="restaurantId">The restaurant identifier.</param>
    /// <param name="categoryOrders">Dictionary mapping category ID to new display order.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> ReorderCategoriesAsync(
        int restaurantId,
        Dictionary<int, int> categoryOrders,
        CancellationToken cancellationToken = default);
}
