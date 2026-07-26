using SmartMenuOptim.Domain.Aggregates.DishAggregate.Errors;
/*
 * File: DishService.cs
 * Service implementation for Dish aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Implements Dish management operations following
 * Clean Architecture and Domain-Driven Design principles.
 * 
 * Design Patterns:
 * - Service Layer Pattern: Orchestrates dish use cases
 * - Repository Pattern: Abstracts data access through IUnityOfWork
 * - Result Pattern: Returns operation results with success/failure semantics
 * - Dependency Injection: Constructor injection for dependencies
 * 
 * Exception Handling Strategy:
 * - DishDomainException: Caught first for dish-specific business rule violations
 *   (tenant consistency, category boundaries). Includes DishId when available.
 * - DomainException: Catches remaining domain-level violations from base class.
 * - ArgumentException: Catches DishName value object validation (min/max length,
 *   invalid characters) and constructor guard clauses.
 * - DbUpdateException: Catches FK constraint violations with user-friendly messages.
 * - Exception: Generic fallback with masked internal details.
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.ValueObjects;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Service implementation for Dish aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This service orchestrates Dish use cases by coordinating between
/// DTOs, domain entities (including the <see cref="DishName"/> value object),
/// and repositories. It resides in the Application layer.</para>
/// 
/// <para><strong>Dependency Injection:</strong></para>
/// <para>All dependencies are injected through the constructor, following
/// the Dependency Inversion Principle (DIP).</para>
/// 
/// <para><strong>Error Handling:</strong></para>
/// <para>Uses the Result pattern to return operation outcomes. Domain exceptions
/// are caught in order of specificity:</para>
/// <list type="number">
///   <item><description><see cref="DishDomainException"/>: Dish-specific business rule violations
///     (tenant consistency, category boundaries). Logged with <see cref="DishDomainException.DishId"/> when available.</description></item>
///   <item><description><see cref="DomainException"/>: Base domain violations from shared rules.</description></item>
///   <item><description><see cref="ArgumentException"/>: Value object validation failures
///     (e.g., <see cref="DishName"/> length/character constraints).</description></item>
///   <item><description><see cref="DbUpdateException"/>: Database constraint violations
///     (FK references to categories, restaurants).</description></item>
///   <item><description><see cref="Exception"/>: Generic fallback with masked internal details.</description></item>
/// </list>
/// 
/// <para><strong>Global Query Filter:</strong></para>
/// <para>The Dish entity has a global query filter (<c>HasQueryFilter(d => !d.IsDeleted)</c>)
/// configured in <c>DishConfiguration</c>, so soft-deleted dishes are automatically excluded
/// from all queries without explicit <c>!d.IsDeleted</c> filters.</para>
/// </remarks>
public class DishService : IDishService
{
    private readonly IUnityOfWork _unitOfWork;
    private readonly ILogger<DishService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DishService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for repository access.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
    public DishService(IUnityOfWork unitOfWork, ILogger<DishService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<DishDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving dish with ID {DishId}", id);

            var dish = await _unitOfWork.Dishes
                .Query()
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (dish is null)
            {
                _logger.LogWarning("Dish with ID {DishId} not found", id);
                return Result<DishDTO>.Failure($"Dish with ID {id} not found.");
            }

            return Result<DishDTO>.Success(MapToDto(dish));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dish with ID {DishId}", id);
            return Result<DishDTO>.Failure("An error occurred while retrieving the dish.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<DishDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving dishes for restaurant {RestaurantId}", restaurantId);

            var dishes = await _unitOfWork.Dishes
                .Query()
                .Where(d => d.RestaurantId == restaurantId)
                .Include(d => d.Category)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);

            var dtos = dishes.Select(MapToDto).ToList();
            _logger.LogDebug("Retrieved {Count} dishes for restaurant {RestaurantId}", dtos.Count, restaurantId);
            return Result<IReadOnlyList<DishDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dishes for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<DishDTO>>.Failure("An error occurred while retrieving dishes.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<DishDTO>>> GetByMenuIdAsync(int menuId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving dishes for menu {MenuId}", menuId);

            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                    .ThenInclude(md => md.Dish)
                        .ThenInclude(d => d.Category)
                .FirstOrDefaultAsync(m => m.Id == menuId, cancellationToken);

            if (menu is null)
            {
                return Result<IReadOnlyList<DishDTO>>.Failure($"Menu with ID {menuId} not found.");
            }

            var dtos = menu.MenuDishes
                .Where(md => md.Dish is not null && !md.Dish.IsDeleted)
                .OrderBy(md => md.DisplayOrder)
                .Select(md => MapToDto(md.Dish!))
                .ToList();

            _logger.LogDebug("Retrieved {Count} dishes for menu {MenuId}", dtos.Count, menuId);
            return Result<IReadOnlyList<DishDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dishes for menu {MenuId}", menuId);
            return Result<IReadOnlyList<DishDTO>>.Failure("An error occurred while retrieving menu dishes.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<DishDTO>> CreateAsync(DishCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating dish '{DishName}' for restaurant {RestaurantId}", dto.Name, dto.RestaurantId);

            var restaurantExists = await _unitOfWork.Restaurants.ExistsAsync(dto.RestaurantId);
            if (!restaurantExists)
            {
                return Result<DishDTO>.Failure($"Restaurant with ID {dto.RestaurantId} not found.");
            }

            var dish = new Dish
            {
                RestaurantId = dto.RestaurantId,
                Name = new DishName(dto.Name),
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                DishPrice = dto.DishPrice,
                Calories = dto.Calories,
                IsVegetarian = dto.IsVegetarian,
                IsSpicy = dto.IsSpicy,
                Ingredients = dto.Ingredients,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Dishes.AddAsync(dish);
            await _unitOfWork.SaveChangesAsync();

            // Reload with category navigation for the response DTO
            var created = await _unitOfWork.Dishes
                .Query()
                .Include(d => d.Category)
                .FirstAsync(d => d.Id == dish.Id, cancellationToken);

            _logger.LogInformation("Dish created with ID {DishId}", dish.Id);
            return Result<DishDTO>.Success(MapToDto(created));
        }
        catch (DishDomainException ex)
        {
            _logger.LogWarning(ex, "Dish domain rule violation creating dish (DishId: {DishId}): {Message}", ex.DishId, ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating dish: {Message}", ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating dish: {Message}", ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating dish for restaurant {RestaurantId}", dto.RestaurantId);
            var innerMessage = ex.InnerException?.Message;
            string message;
            if (innerMessage?.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true)
                message = "Invalid category or restaurant reference. Please verify the selected options exist.";
            else if (innerMessage?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                     || innerMessage?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
                message = $"A dish with the name '{dto.Name}' already exists for this restaurant.";
            else
                message = "A database error occurred while creating the dish.";
            return Result<DishDTO>.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dish for restaurant {RestaurantId}", dto.RestaurantId);
            return Result<DishDTO>.Failure("An error occurred while creating the dish.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<DishDTO>> UpdateAsync(DishUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating dish with ID {DishId}", dto.Id);

            var dish = await _unitOfWork.Dishes
                .Query()
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == dto.Id, cancellationToken);

            if (dish is null)
            {
                return Result<DishDTO>.Failure($"Dish with ID {dto.Id} not found.");
            }

            dish.Name = new DishName(dto.Name);
            dish.Description = dto.Description;
            dish.CategoryId = dto.CategoryId;
            dish.DishPrice = dto.DishPrice;
            dish.Calories = dto.Calories;
            dish.IsVegetarian = dto.IsVegetarian;
            dish.IsSpicy = dto.IsSpicy;
            dish.Ingredients = dto.Ingredients;
            dish.IsActive = dto.IsActive;
            dish.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Dishes.Update(dish);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Dish with ID {DishId} updated", dto.Id);
            return Result<DishDTO>.Success(MapToDto(dish));
        }
        catch (DishDomainException ex)
        {
            _logger.LogWarning(ex, "Dish domain rule violation updating dish {DishId} (DomainDishId: {DomainDishId}): {Message}",
                dto.Id, ex.DishId, ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating dish {DishId}: {Message}", dto.Id, ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating dish: {Message}", ex.Message);
            return Result<DishDTO>.Failure(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error updating dish {DishId}", dto.Id);
            var innerMessage = ex.InnerException?.Message;
            string message;
            if (innerMessage?.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true)
                message = "Invalid category or restaurant reference. Please verify the selected options exist.";
            else if (innerMessage?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                     || innerMessage?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
                message = $"A dish with the name '{dto.Name}' already exists for this restaurant.";
            else
                message = "A database error occurred while updating the dish.";
            return Result<DishDTO>.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dish with ID {DishId}", dto.Id);
            return Result<DishDTO>.Failure("An error occurred while updating the dish.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting dish with ID {DishId}", id);

            var dish = await _unitOfWork.Dishes
                .Query()
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            if (dish is null)
            {
                return Result.Failure($"Dish with ID {id} not found.");
            }

            _unitOfWork.Dishes.Delete(dish);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Dish with ID {DishId} deleted", id);
            return Result.Success();
        }
        catch (DishDomainException ex)
        {
            _logger.LogWarning(ex, "Dish domain rule violation deleting dish {DishId} (DomainDishId: {DomainDishId}): {Message}",
                id, ex.DishId, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation deleting dish {DishId}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting dish {DishId}", id);
            var message = ex.InnerException?.Message?.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true
                ? "Cannot delete this dish because it is referenced by menus, orders, or sales records."
                : "A database error occurred while deleting the dish.";
            return Result.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dish with ID {DishId}", id);
            return Result.Failure("An error occurred while deleting the dish.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a <see cref="Dish"/> domain entity to a <see cref="DishDTO"/>.
    /// </summary>
    /// <param name="dish">The dish entity to map. Must not be null.</param>
    /// <returns>A new <see cref="DishDTO"/> populated from the entity.</returns>
    /// <remarks>
    /// Accesses <see cref="DishName.Value"/> for the display name (safe in-memory after materialization).
    /// Maps <see cref="Dish.DishPrice"/> to <see cref="DishDTO.Price"/> for frontend consistency.
    /// </remarks>
    private static DishDTO MapToDto(Dish dish) => new()
    {
        Id = dish.Id,
        RestaurantId = dish.RestaurantId,
        Name = dish.Name.Value,
        Description = dish.Description,
        CategoryId = dish.CategoryId,
        CategoryName = dish.Category?.Name,
        Price = dish.DishPrice,
        Calories = dish.Calories,
        IsVegetarian = dish.IsVegetarian,
        IsSpicy = dish.IsSpicy,
        Ingredients = dish.Ingredients,
        IsActive = dish.IsActive
    };
}
