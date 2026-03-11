/*
 * File: CategoryService.cs
 * Service implementation for DishCategory entity operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Design Patterns:
 * - Service Layer Pattern: Orchestrates category use cases
 * - Repository Pattern: Abstracts data access through IUnityOfWork
 * - Result Pattern: Returns operation results with success/failure semantics
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Dtos.Dish;
using SmartMenuOptim.Application.Features.Restaurants.Mappings;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Repositories;

namespace SmartMenuOptim.Application.Services.Restaurant;

/// <summary>
/// Service implementation for DishCategory entity operations.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnityOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IUnityOfWork unitOfWork, ILogger<CategoryService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<CategoryDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving category with ID {CategoryId}", id);

            var category = await _unitOfWork.Categories
                .Query()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (category is null)
            {
                _logger.LogWarning("Category with ID {CategoryId} not found", id);
                return Result<CategoryDTO>.Failure($"Category with ID {id} not found.");
            }

            return Result<CategoryDTO>.Success(category.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
            return Result<CategoryDTO>.Failure("An error occurred while retrieving the category.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CategoryDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving categories for restaurant {RestaurantId}", restaurantId);

            var categories = await _unitOfWork.Categories
                .Query()
                .Where(c => c.RestaurantId == restaurantId && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var dtos = categories.Select(c => c.ToDto()).ToList();
            return Result<IReadOnlyList<CategoryDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<CategoryDTO>>.Failure("An error occurred while retrieving categories.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<CategoryDTO>> CreateAsync(int restaurantId, CategoryCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating category '{CategoryName}' for restaurant {RestaurantId}", dto.Name, restaurantId);

            // Verify restaurant exists
            var restaurantExists = await _unitOfWork.Restaurants.ExistsAsync(restaurantId);
            if (!restaurantExists)
            {
                return Result<CategoryDTO>.Failure($"Restaurant with ID {restaurantId} not found.");
            }

            // Check for duplicate name in same restaurant
            var exists = await _unitOfWork.Categories
                .Query()
                .AnyAsync(c => c.RestaurantId == restaurantId 
                            && c.Name == dto.Name 
                            && !c.IsDeleted, cancellationToken);

            if (exists)
            {
                return Result<CategoryDTO>.Failure($"A category named '{dto.Name}' already exists in this restaurant.");
            }

            var category = new DishCategory(
                restaurantId: restaurantId,
                name: dto.Name,
                description: dto.Description);

            if (dto.DisplayOrder > 0)
            {
                category.UpdateDisplayOrder(dto.DisplayOrder);
            }

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category created with ID {CategoryId}", category.Id);
            return Result<CategoryDTO>.Success(category.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating category: {Message}", ex.Message);
            return Result<CategoryDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category for restaurant {RestaurantId}", restaurantId);
            return Result<CategoryDTO>.Failure("An error occurred while creating the category.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<CategoryDTO>> UpdateAsync(CategoryUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating category with ID {CategoryId}", dto.Id);

            var category = await _unitOfWork.Categories
                .Query()
                .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted, cancellationToken);

            if (category is null)
            {
                return Result<CategoryDTO>.Failure($"Category with ID {dto.Id} not found.");
            }

            // Check for duplicate name in same restaurant (excluding current category)
            var duplicateExists = await _unitOfWork.Categories
                .Query()
                .AnyAsync(c => c.RestaurantId == category.RestaurantId 
                            && c.Name == dto.Name 
                            && c.Id != dto.Id
                            && !c.IsDeleted, cancellationToken);

            if (duplicateExists)
            {
                return Result<CategoryDTO>.Failure($"A category named '{dto.Name}' already exists in this restaurant.");
            }

            category.UpdateBasicInfo(dto.Name, dto.Description);
            category.UpdateDisplayOrder(dto.DisplayOrder);

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category with ID {CategoryId} updated", dto.Id);
            return Result<CategoryDTO>.Success(category.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating category: {Message}", ex.Message);
            return Result<CategoryDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}", dto.Id);
            return Result<CategoryDTO>.Failure("An error occurred while updating the category.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting category with ID {CategoryId}", id);

            var category = await _unitOfWork.Categories
                .Query()
                .Include(c => c.Dishes)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

            if (category is null)
            {
                return Result.Failure($"Category with ID {id} not found.");
            }

            // Check if category has active dishes
            var hasActiveDishes = category.Dishes?.Any(d => !d.IsDeleted) ?? false;
            if (hasActiveDishes)
            {
                return Result.Failure("Cannot delete category that contains active dishes. Please reassign or delete the dishes first.");
            }

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Category with ID {CategoryId} deleted", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
            return Result.Failure("An error occurred while deleting the category.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ReorderCategoriesAsync(
        int restaurantId,
        Dictionary<int, int> categoryOrders,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Reordering {Count} categories for restaurant {RestaurantId}", 
                categoryOrders.Count, restaurantId);

            var categories = await _unitOfWork.Categories
                .Query()
                .Where(c => c.RestaurantId == restaurantId && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var category in categories)
            {
                if (categoryOrders.TryGetValue(category.Id, out var newOrder))
                {
                    category.UpdateDisplayOrder(newOrder);
                    _unitOfWork.Categories.Update(category);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Categories reordered for restaurant {RestaurantId}", restaurantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories for restaurant {RestaurantId}", restaurantId);
            return Result.Failure("An error occurred while reordering categories.");
        }
    }
}
