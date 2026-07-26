using SmartMenuOptim.Domain.Aggregates.MenuAggregate.Errors;
/*
 * File: MenuService.cs
 * Service implementation for Menu aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Design Patterns:
 * - Service Layer Pattern: Orchestrates menu use cases
 * - Repository Pattern: Abstracts data access through IUnityOfWork
 * - Result Pattern: Returns operation results with success/failure semantics
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.Mappings;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Service implementation for Menu aggregate operations.
/// </summary>
public class MenuService : IMenuService
{
    private readonly IUnityOfWork _unitOfWork;
    private readonly ILogger<MenuService> _logger;

    public MenuService(IUnityOfWork unitOfWork, ILogger<MenuService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<MenuDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving menu with ID {MenuId}", id);

            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                _logger.LogWarning("Menu with ID {MenuId} not found", id);
                return Result<MenuDTO>.Failure($"Menu with ID {id} not found.");
            }

            return Result<MenuDTO>.Success(menu.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu with ID {MenuId}", id);
            return Result<MenuDTO>.Failure("An error occurred while retrieving the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MenuDTO>>> GetByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving menus for restaurant {RestaurantId}", restaurantId);

            var menus = await _unitOfWork.Menus
                .Query()
                .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted)
                .Include(m => m.MenuDishes)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            var dtos = menus.Select(m => m.ToDto()).ToList();
            return Result<IReadOnlyList<MenuDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menus for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<MenuDTO>>.Failure("An error occurred while retrieving menus.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<MenuDTO>>> GetActiveByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving active menus for restaurant {RestaurantId}", restaurantId);

            var menus = await _unitOfWork.Menus
                .Query()
                .Where(m => m.RestaurantId == restaurantId && !m.IsDeleted && m.IsAvailable)
                .Include(m => m.MenuDishes)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken);

            var dtos = menus.Select(m => m.ToDto()).ToList();
            return Result<IReadOnlyList<MenuDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active menus for restaurant {RestaurantId}", restaurantId);
            return Result<IReadOnlyList<MenuDTO>>.Failure("An error occurred while retrieving menus.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<MenuDTO>> CreateAsync(int restaurantId, MenuCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating menu '{MenuName}' for restaurant {RestaurantId}", dto.Name, restaurantId);

            // Verify restaurant exists
            var restaurantExists = await _unitOfWork.Restaurants.ExistsAsync(restaurantId);
            if (!restaurantExists)
            {
                return Result<MenuDTO>.Failure($"Restaurant with ID {restaurantId} not found.");
            }

            // MenuTypeId is required, use default of 1 if not provided
            var menuTypeId = dto.MenuTypeId ?? 1;

            var menu = new Menu(
                restaurantId: restaurantId,
                name: dto.Name,
                menuTypeId: menuTypeId,
                description: dto.Description);

            // Set availability window if provided
            if (dto.AvailableFrom.HasValue && dto.AvailableTo.HasValue)
            {
                menu.SetAvailability(dto.AvailableFrom.Value, dto.AvailableTo.Value);
            }

            // Note: A newly created menu cannot be made available immediately because
            // the domain requires at least one active dish. Activation is deferred until
            // dishes are added via AddDishToMenuAsync, then MakeAvailableAsync.
            if (dto.IsActive)
            {
                _logger.LogInformation(
                    "Menu '{MenuName}' requested as active, but activation is deferred until dishes are added",
                    dto.Name);
            }

            await _unitOfWork.Menus.AddAsync(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu created with ID {MenuId}", menu.Id);
            return Result<MenuDTO>.Success(menu.ToDto());
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating menu: {Message}", ex.Message);
            return Result<MenuDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating menu: {Message}", ex.Message);
            return Result<MenuDTO>.Failure(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating menu for restaurant {RestaurantId}", restaurantId);
            var message = ex.InnerException?.Message?.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true
                ? "Invalid menu type or restaurant reference. Please verify the selected options exist."
                : "A database error occurred while creating the menu.";
            return Result<MenuDTO>.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu for restaurant {RestaurantId}", restaurantId);
            return Result<MenuDTO>.Failure("An error occurred while creating the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<MenuDTO>> UpdateAsync(MenuUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating menu with ID {MenuId}", dto.Id);

            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                return Result<MenuDTO>.Failure($"Menu with ID {dto.Id} not found.");
            }

            menu.UpdateBasicInfo(dto.Name, dto.Description);

            if (dto.AvailableFrom.HasValue && dto.AvailableTo.HasValue)
            {
                menu.SetAvailability(dto.AvailableFrom.Value, dto.AvailableTo.Value);
            }

            if (dto.IsActive && !menu.IsAvailable)
            {
                menu.MakeAvailable();
            }
            else if (!dto.IsActive && menu.IsAvailable)
            {
                menu.MakeUnavailable();
            }

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu with ID {MenuId} updated", dto.Id);
            return Result<MenuDTO>.Success(menu.ToDto());
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating menu {MenuId}: {Message}", dto.Id, ex.Message);
            return Result<MenuDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating menu: {Message}", ex.Message);
            return Result<MenuDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu with ID {MenuId}", dto.Id);
            return Result<MenuDTO>.Failure("An error occurred while updating the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting menu with ID {MenuId}", id);

            var menu = await _unitOfWork.Menus.GetByIdAsync(id);

            if (menu is null || menu.IsDeleted)
            {
                return Result.Failure($"Menu with ID {id} not found.");
            }

            _unitOfWork.Menus.Delete(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu with ID {MenuId} deleted", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting menu with ID {MenuId}", id);
            return Result.Failure("An error occurred while deleting the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> MakeAvailableAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load menu with MenuDishes to allow domain validation
            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                return Result.Failure($"Menu with ID {id} not found.");
            }

            menu.MakeAvailable();
            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} made available", id);
            return Result.Success();
        }
        catch (MenuDomainException ex)
        {
            // Return domain validation messages to the caller
            _logger.LogWarning(ex, "Domain validation failed for menu {MenuId}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making menu {MenuId} available", id);
            return Result.Failure("An error occurred while updating the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> MakeUnavailableAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Load menu with MenuDishes for consistency with MakeAvailableAsync
            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                return Result.Failure($"Menu with ID {id} not found.");
            }

            menu.MakeUnavailable();
            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} made unavailable", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making menu {MenuId} unavailable", id);
            return Result.Failure("An error occurred while updating the menu.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DISH MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result> AddDishToMenuAsync(
        int menuId,
        int dishId,
        int displayOrder = 0,
        decimal? specialPrice = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Adding dish {DishId} to menu {MenuId}", dishId, menuId);

            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == menuId && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                return Result.Failure($"Menu with ID {menuId} not found.");
            }

            var dish = await _unitOfWork.Dishes
                .Query()
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.Id == dishId && !d.IsDeleted, cancellationToken);

            if (dish is null)
            {
                return Result.Failure($"Dish with ID {dishId} not found.");
            }

            // Verify same restaurant
            if (dish.RestaurantId != menu.RestaurantId)
            {
                return Result.Failure("Dish must belong to the same restaurant as the menu.");
            }

            // AddDish requires the Dish entity
            menu.AddDish(dish, displayOrder, specialPrice);

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Dish {DishId} added to menu {MenuId}", dishId, menuId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation adding dish {DishId} to menu {MenuId}: {Message}", dishId, menuId, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error adding dish to menu: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding dish {DishId} to menu {MenuId}", dishId, menuId);
            return Result.Failure("An error occurred while adding the dish to the menu.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> RemoveDishFromMenuAsync(int menuId, int dishId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing dish {DishId} from menu {MenuId}", dishId, menuId);

            var menu = await _unitOfWork.Menus
                .Query()
                .Include(m => m.MenuDishes)
                .FirstOrDefaultAsync(m => m.Id == menuId && !m.IsDeleted, cancellationToken);

            if (menu is null)
            {
                return Result.Failure($"Menu with ID {menuId} not found.");
            }

            menu.RemoveDish(dishId);

            _unitOfWork.Menus.Update(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Dish {DishId} removed from menu {MenuId}", dishId, menuId);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error removing dish from menu: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing dish {DishId} from menu {MenuId}", dishId, menuId);
            return Result.Failure("An error occurred while removing the dish from the menu.");
        }
    }
}
