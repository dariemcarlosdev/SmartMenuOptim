/*
 * File: RestaurantMappingExtensions.cs
 * Mapping extensions for Restaurant-related entities and DTOs
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Provides extension methods for converting between Domain entities
 * and DTOs following Clean Architecture principles.
 * 
 * Design Patterns:
 * - Extension Methods: Allows adding mapping behavior without modifying entities
 * - Single Responsibility: Each method handles one mapping direction
 */

using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.ValueObjects;
using DishEntity = SmartMenuOptim.Domain.Aggregates.DishAggregate.Dish;

namespace SmartMenuOptim.Application.Features.Restaurants.Mappings;

/// <summary>
/// Extension methods for mapping Restaurant-related entities to DTOs and vice versa.
/// </summary>
/// <remarks>
/// <para><strong>Clean Architecture Compliance:</strong></para>
/// <para>These extensions keep mapping logic in the Application layer, preventing
/// Domain entities from having knowledge of DTOs.</para>
/// </remarks>
public static class RestaurantMappingExtensions
{
    // ═══════════════════════════════════════════════════════════════════════
    // RESTAURANT MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a Restaurant entity to a RestaurantDTO.
    /// </summary>
    /// <param name="entity">The Restaurant entity to map.</param>
    /// <returns>A new RestaurantDTO with mapped values.</returns>
    public static RestaurantDTO ToDto(this Restaurant entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RestaurantDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            OwnerId = entity.OwnerId,
            Email = entity.ContactEmail?.Value ?? string.Empty,
            PhoneNumber = entity.ContactPhone?.Value ?? string.Empty,
            Address = entity.Location?.ToDto() ?? new AddressDTO(),
            TimeZoneId = entity.TimeZoneId,
            MaxSimultaneousOrders = entity.MaxSimultaneousOrders,
            IsAcceptingOrders = entity.IsAcceptingOrders,
            BusinessHours = entity.OperatingHours?.Select(h => h.ToDto()).ToList() ?? [],
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt,
            IsDeleted = entity.IsDeleted
        };
    }

    /// <summary>
    /// Maps a Restaurant entity to a RestaurantDetailDTO with full related data.
    /// </summary>
    /// <param name="entity">The Restaurant entity to map.</param>
    /// <param name="menus">Optional list of menus to include.</param>
    /// <param name="dishes">Optional list of dishes to include.</param>
    /// <param name="categories">Optional list of categories to include.</param>
    /// <returns>A new RestaurantDetailDTO with mapped values.</returns>
    public static RestaurantDetailDTO ToDetailDto(
        this Restaurant entity,
        IEnumerable<Menu>? menus = null,
        IEnumerable<DishEntity>? dishes = null,
        IEnumerable<DishCategory>? categories = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RestaurantDetailDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            OwnerId = entity.OwnerId,
            Email = entity.ContactEmail?.Value ?? string.Empty,
            PhoneNumber = entity.ContactPhone?.Value ?? string.Empty,
            Address = entity.Location?.ToDto() ?? new AddressDTO(),
            TimeZoneId = entity.TimeZoneId,
            MaxSimultaneousOrders = entity.MaxSimultaneousOrders,
            IsAcceptingOrders = entity.IsAcceptingOrders,
            BusinessHours = entity.OperatingHours?.Select(h => h.ToDto()).ToList() ?? [],
            Menus = menus?.Select(m => m.ToDto()).ToList() ?? [],
            Dishes = dishes?.Select(d => d.ToDto()).ToList() ?? [],
            Categories = categories?.Select(c => c.ToDto()).ToList() ?? [],
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt,
            IsDeleted = entity.IsDeleted
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ADDRESS MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps an Address value object to an AddressDTO.
    /// </summary>
    /// <param name="valueObject">The Address value object to map.</param>
    /// <returns>A new AddressDTO with mapped values.</returns>
    public static AddressDTO ToDto(this Address valueObject)
    {
        ArgumentNullException.ThrowIfNull(valueObject);

        return new AddressDTO
        {
            Street = valueObject.Street,
            Street2 = valueObject.Street2,
            City = valueObject.City,
            State = valueObject.State,
            PostalCode = valueObject.PostalCode,
            CountryCode = valueObject.CountryCode
        };
    }

    /// <summary>
    /// Maps an AddressDTO to an Address value object.
    /// </summary>
    /// <param name="dto">The AddressDTO to map.</param>
    /// <returns>A new Address value object with mapped values.</returns>
    public static Address ToValueObject(this AddressDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Address(
            street: dto.Street,
            city: dto.City,
            state: dto.State,
            postalCode: dto.PostalCode,
            countryCode: dto.CountryCode,
            street2: dto.Street2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUSINESS HOURS MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a BusinessHours entity to a BusinessHoursDTO.
    /// </summary>
    /// <param name="entity">The BusinessHours entity to map.</param>
    /// <returns>A new BusinessHoursDTO with mapped values.</returns>
    public static BusinessHoursDTO ToDto(this BusinessHours entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new BusinessHoursDTO
        {
            Id = entity.Id,
            DayOfWeek = entity.DayOfWeek,
            OpenTime = entity.OpenTime,
            CloseTime = entity.CloseTime,
            IsClosed = entity.IsClosed
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MENU MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a Menu entity to a MenuDTO.
    /// </summary>
    /// <param name="entity">The Menu entity to map.</param>
    /// <returns>A new MenuDTO with mapped values.</returns>
    public static MenuDTO ToDto(this Menu entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new MenuDTO
        {
            Id = entity.Id,
            RestaurantId = entity.RestaurantId,
            Name = entity.Name,
            Description = entity.Description,
            MenuTypeId = entity.MenuTypeId,
            AvailableFrom = entity.AvailableFrom,
            AvailableTo = entity.AvailableTo,
            IsActive = entity.IsAvailable,
            DishCount = entity.MenuDishes?.Count ?? 0,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt ?? entity.CreatedAt // Handle null UpdatedAt by using CreatedAt as fallback
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CATEGORY MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a DishCategory entity to a CategoryDTO.
    /// </summary>
    /// <param name="entity">The DishCategory entity to map.</param>
    /// <returns>A new CategoryDTO with mapped values.</returns>
    public static CategoryDTO ToDto(this DishCategory entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CategoryDTO
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive,
            RestaurantId = entity.RestaurantId
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DISH MAPPINGS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps a Dish entity to a DishDTO.
    /// </summary>
    /// <param name="entity">The Dish entity to map.</param>
    /// <returns>A new DishDTO with mapped values.</returns>
    public static DishDTO ToDto(this DishEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DishDTO
        {
            Id = entity.Id,
            Name = entity.Name?.Value ?? string.Empty,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.Name
        };
    }
}
