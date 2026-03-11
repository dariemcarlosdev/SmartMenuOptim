/*
 * File: RestaurantDetailDTO.cs
 * Data Transfer Object for detailed Restaurant view
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents detailed restaurant information including related entities.
 * Used for detailed views where full restaurant information is needed.
 * 
 * Usage: Dashboard views, restaurant detail pages, admin management.
 */

using SmartMenuOptim.Application.Dtos;
using SmartMenuOptim.Application.Dtos.Restaurant;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for detailed Restaurant view with related entities.
/// </summary>
/// <remarks>
/// <para>Extended version of RestaurantDTO with additional navigation data.</para>
/// <para>Use this DTO when you need full restaurant details with menus and dishes.</para>
/// </remarks>
public class RestaurantDetailDTO
{
    /// <summary>
    /// Restaurant identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the restaurant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the restaurant.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Owner (AdminUser) identifier.
    /// </summary>
    public int OwnerId { get; set; }

    /// <summary>
    /// Owner name for display purposes.
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Physical location/address of the restaurant.
    /// </summary>
    public AddressDTO Address { get; set; } = new();

    /// <summary>
    /// IANA timezone identifier.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Maximum number of simultaneous orders.
    /// </summary>
    public int MaxSimultaneousOrders { get; set; }

    /// <summary>
    /// Whether the restaurant is currently accepting orders.
    /// </summary>
    public bool IsAcceptingOrders { get; set; }

    /// <summary>
    /// Business hours for each day of the week.
    /// </summary>
    public List<BusinessHoursDTO> BusinessHours { get; set; } = [];

    /// <summary>
    /// List of categories in this restaurant.
    /// </summary>
    public List<CategoryDTO> Categories { get; set; } = [];

    /// <summary>
    /// List of menus in this restaurant.
    /// </summary>
    public List<MenuDTO> Menus { get; set; } = [];

    /// <summary>
    /// List of dishes in this restaurant.
    /// </summary>
    public List<DishDTO> Dishes { get; set; } = [];

    /// <summary>
    /// Average rating of the restaurant from all reviews (1-5).
    /// </summary>
    public double? AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Date and time when the restaurant was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date and time when the restaurant was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if the restaurant has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // === Computed Properties for Dashboard ===

    /// <summary>
    /// Gets the current operational status for display.
    /// </summary>
    public string StatusDisplay => IsDeleted
        ? "Closed"
        : IsAcceptingOrders
            ? "Open"
            : "Not Accepting Orders";

    /// <summary>
    /// Gets today's business hours for display.
    /// </summary>
    public string? TodaysHours
    {
        get
        {
            var today = DateTime.Now.DayOfWeek;
            var todaysHours = BusinessHours.FirstOrDefault(h => h.DayOfWeek == today);
            return todaysHours?.FormattedHours;
        }
    }

    /// <summary>
    /// Gets whether the restaurant is currently open based on business hours.
    /// </summary>
    public bool IsCurrentlyOpen
    {
        get
        {
            if (!IsAcceptingOrders) return false;
            
            var today = DateTime.Now.DayOfWeek;
            var currentTime = DateTime.Now.TimeOfDay;
            var todaysHours = BusinessHours.FirstOrDefault(h => h.DayOfWeek == today);
            
            if (todaysHours == null || todaysHours.IsClosed) return false;
            
            return currentTime >= todaysHours.OpenTime && currentTime <= todaysHours.CloseTime;
        }
    }

    /// <summary>
    /// Total number of menus.
    /// </summary>
    public int MenuCount => Menus.Count;

    /// <summary>
    /// Total number of dishes.
    /// </summary>
    public int DishCount => Dishes.Count;

    /// <summary>
    /// Total number of categories.
    /// </summary>
    public int CategoryCount => Categories.Count;

    /// <summary>
    /// Number of active menus.
    /// </summary>
    public int ActiveMenuCount => Menus.Count(m => m.IsActive);
}
