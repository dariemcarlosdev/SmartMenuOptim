/*
 * File: RestaurantService.cs
 * Service implementation for Restaurant aggregate operations
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Implements Restaurant management operations following
 * Clean Architecture and Domain-Driven Design principles.
 * 
 * Design Patterns:
 * - Service Layer Pattern: Orchestrates use cases
 * - Repository Pattern: Abstracts data access through IUnityOfWork
 * - Result Pattern: Returns operation results with success/failure semantics
 * - Dependency Injection: Constructor injection for dependencies
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartMenuOptim.Application.Common;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Application.Features.Restaurants.Mappings;
using SmartMenuOptim.Domain.Exceptions;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.ValueObjects;
using RestaurantEntity = SmartMenuOptim.Domain.Aggregates.RestaurantAggregate.Restaurant;

namespace SmartMenuOptim.Application.Features.Restaurants.Services;

/// <summary>
/// Service implementation for Restaurant aggregate operations.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>This service orchestrates use cases by coordinating between
/// DTOs, domain entities, and repositories. It resides in the Application layer.</para>
/// 
/// <para><strong>Dependency Injection:</strong></para>
/// <para>All dependencies are injected through the constructor, following
/// the Dependency Inversion Principle (DIP).</para>
/// 
/// <para><strong>Error Handling:</strong></para>
/// <para>Uses Result pattern to return operation outcomes. Exceptions are
/// caught and converted to failure results with appropriate error messages.</para>
/// </remarks>
public class RestaurantService : IRestaurantService
{
    private readonly IUnityOfWork _unitOfWork;
    private readonly ILogger<RestaurantService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestaurantService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for repository access.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
    public RestaurantService(
        IUnityOfWork unitOfWork,
        ILogger<RestaurantService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<RestaurantDTO>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving restaurant with ID {RestaurantId}", id);

            var restaurant = await _unitOfWork.Restaurants
                .Query()
                .Include(r => r.OperatingHours)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found", id);
                return Result<RestaurantDTO>.Failure($"Restaurant with ID {id} not found.");
            }

            return Result<RestaurantDTO>.Success(restaurant.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving restaurant with ID {RestaurantId}", id);
            return Result<RestaurantDTO>.Failure("An error occurred while retrieving the restaurant.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<RestaurantDetailDTO>> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving detailed restaurant with ID {RestaurantId}", id);

            var restaurant = await _unitOfWork.Restaurants
                .Query()
                .Include(r => r.OperatingHours)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found", id);
                return Result<RestaurantDetailDTO>.Failure($"Restaurant with ID {id} not found.");
            }

            // Load related data
            var menus = await _unitOfWork.Menus
                .Query()
                .Where(m => m.RestaurantId == id && !m.IsDeleted)
                .Include(m => m.MenuDishes)
                .ToListAsync(cancellationToken);

            var dishes = await _unitOfWork.Dishes
                .Query()
                .Where(d => d.RestaurantId == id && !d.IsDeleted)
                .Include(d => d.Category)
                .ToListAsync(cancellationToken);

            var categories = await _unitOfWork.Categories
                .Query()
                .Where(c => c.RestaurantId == id && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            return Result<RestaurantDetailDTO>.Success(
                restaurant.ToDetailDto(menus, dishes, categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving detailed restaurant with ID {RestaurantId}", id);
            return Result<RestaurantDetailDTO>.Failure("An error occurred while retrieving the restaurant details.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<RestaurantDTO>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving all restaurants");

            var restaurants = await _unitOfWork.Restaurants
                .Query()
                .Where(r => !r.IsDeleted)
                .Include(r => r.OperatingHours)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var dtos = restaurants.Select(r => r.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} restaurants", dtos.Count);
            return Result<IReadOnlyList<RestaurantDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all restaurants");
            return Result<IReadOnlyList<RestaurantDTO>>.Failure("An error occurred while retrieving restaurants.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<RestaurantDTO>>> GetByOwnerAsync(int ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving restaurants for owner {OwnerId}", ownerId);

            var restaurants = await _unitOfWork.Restaurants
                .Query()
                .Where(r => r.OwnerId == ownerId && !r.IsDeleted)
                .Include(r => r.OperatingHours)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var dtos = restaurants.Select(r => r.ToDto()).ToList();

            _logger.LogDebug("Retrieved {Count} restaurants for owner {OwnerId}", dtos.Count, ownerId);
            return Result<IReadOnlyList<RestaurantDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving restaurants for owner {OwnerId}", ownerId);
            return Result<IReadOnlyList<RestaurantDTO>>.Failure("An error occurred while retrieving restaurants.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result<RestaurantDTO>> CreateAsync(RestaurantCreateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new restaurant: {RestaurantName}", dto.Name);

            // Create value objects
            var address = dto.Address.ToValueObject();
            var email = new Email(dto.Email);
            var phone = new PhoneNumber(dto.PhoneNumber);

            // Create the restaurant aggregate
            var restaurant = new RestaurantEntity(
                ownerId: dto.OwnerId,
                name: dto.Name,
                location: address,
                contactPhone: phone,
                contactEmail: email,
                maxSimultaneousOrders: dto.MaxSimultaneousOrders,
                description: dto.Description,
                timeZoneId: dto.TimeZoneId);

            // Set business hours if provided
            foreach (var hours in dto.BusinessHours.Where(h => !h.IsClosed))
            {
                restaurant.SetBusinessHours(hours.DayOfWeek, hours.OpenTime, hours.CloseTime);
            }

            await _unitOfWork.Restaurants.AddAsync(restaurant);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Restaurant created successfully with ID {RestaurantId}", restaurant.Id);

            return Result<RestaurantDTO>.Success(restaurant.ToDto());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating restaurant: {Message}", ex.Message);
            return Result<RestaurantDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating restaurant: {RestaurantName}", dto.Name);
            return Result<RestaurantDTO>.Failure("An error occurred while creating the restaurant.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<RestaurantDTO>> UpdateAsync(RestaurantUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating restaurant with ID {RestaurantId}", dto.Id);

            var restaurant = await _unitOfWork.Restaurants
                .Query()
                .Include(r => r.OperatingHours)
                .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found for update", dto.Id);
                return Result<RestaurantDTO>.Failure($"Restaurant with ID {dto.Id} not found.");
            }

            // Update basic info
            restaurant.UpdateBasicInfo(dto.Name, dto.Description);

            // Update contact info
            var email = new Email(dto.Email);
            var phone = new PhoneNumber(dto.PhoneNumber);
            restaurant.UpdateContactInfo(email, phone);

            // Update location if provided
            if (dto.Address is not null)
            {
                var address = dto.Address.ToValueObject();
                restaurant.UpdateLocation(address);
            }

            // Update business hours if provided
            if (dto.BusinessHours?.Count > 0)
            {
                foreach (var hours in dto.BusinessHours)
                {
                    if (hours.IsClosed)
                    {
                        restaurant.RemoveBusinessHours(hours.DayOfWeek);
                    }
                    else
                    {
                        restaurant.SetBusinessHours(hours.DayOfWeek, hours.OpenTime, hours.CloseTime);
                    }
                }
            }

            // Note: IsAcceptingOrders is managed by the dedicated ToggleAcceptingOrdersAsync method,
            // which enforces the domain rule requiring business hours before accepting orders.

            _unitOfWork.Restaurants.Update(restaurant);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Restaurant with ID {RestaurantId} updated successfully", dto.Id);

            return Result<RestaurantDTO>.Success(restaurant.ToDto());
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation updating restaurant {RestaurantId}: {Message}", dto.Id, ex.Message);
            return Result<RestaurantDTO>.Failure(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating restaurant: {Message}", ex.Message);
            return Result<RestaurantDTO>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating restaurant with ID {RestaurantId}", dto.Id);
            return Result<RestaurantDTO>.Failure("An error occurred while updating the restaurant.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting restaurant with ID {RestaurantId}", id);

            var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(id);

            if (restaurant is null || restaurant.IsDeleted)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found for deletion", id);
                return Result.Failure($"Restaurant with ID {id} not found.");
            }

            // Soft delete
            _unitOfWork.Restaurants.Delete(restaurant);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Restaurant with ID {RestaurantId} deleted successfully", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting restaurant with ID {RestaurantId}", id);
            return Result.Failure("An error occurred while deleting the restaurant.");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> ToggleAcceptingOrdersAsync(int id, bool isAccepting, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Toggling accepting orders for restaurant {RestaurantId} to {IsAccepting}", id, isAccepting);

            var restaurant = await _unitOfWork.Restaurants
                .Query()
                .Include(r => r.OperatingHours)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found", id);
                return Result.Failure($"Restaurant with ID {id} not found.");
            }

            if (isAccepting)
            {
                restaurant.StartAcceptingOrders();
            }
            else
            {
                restaurant.StopAcceptingOrders();
            }

            _unitOfWork.Restaurants.Update(restaurant);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Restaurant {RestaurantId} accepting orders set to {IsAccepting}", id, isAccepting);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation toggling orders for restaurant {RestaurantId}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling accepting orders for restaurant {RestaurantId}", id);
            return Result.Failure("An error occurred while updating the restaurant status.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<Result> SetBusinessHoursAsync(
        int restaurantId,
        List<BusinessHoursDTO> businessHours,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting business hours for restaurant {RestaurantId}", restaurantId);

            var restaurant = await _unitOfWork.Restaurants
                .Query()
                .Include(r => r.OperatingHours)
                .FirstOrDefaultAsync(r => r.Id == restaurantId && !r.IsDeleted, cancellationToken);

            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant with ID {RestaurantId} not found", restaurantId);
                return Result.Failure($"Restaurant with ID {restaurantId} not found.");
            }

            // Clear existing hours and set new ones
            foreach (var hours in businessHours)
            {
                if (hours.IsClosed)
                {
                    restaurant.RemoveBusinessHours(hours.DayOfWeek);
                }
                else
                {
                    restaurant.SetBusinessHours(hours.DayOfWeek, hours.OpenTime, hours.CloseTime);
                }
            }

            _unitOfWork.Restaurants.Update(restaurant);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Business hours updated for restaurant {RestaurantId}", restaurantId);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error setting business hours: {Message}", ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting business hours for restaurant {RestaurantId}", restaurantId);
            return Result.Failure("An error occurred while setting business hours.");
        }
    }
}
