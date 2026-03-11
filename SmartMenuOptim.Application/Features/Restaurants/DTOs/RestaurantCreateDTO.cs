/*
 * File: RestaurantCreateDTO.cs
 * Data Transfer Object for creating a new Restaurant
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents the data required to create a new restaurant.
 * Used by the Application layer services and API controllers.
 * 
 * Multi-Tenant Considerations:
 * - This DTO creates a new tenant root entity
 * - OwnerId links the restaurant to its AdminUser
 * - All child entities will reference this restaurant's Id
 */

using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for creating a new Restaurant.
/// </summary>
/// <remarks>
/// <para>Contains all required data to create a new <see cref="Domain.Aggregates.RestaurantAggregate.Restaurant"/>.</para>
/// <para>Validation attributes provide client-side validation in Blazor forms.</para>
/// </remarks>
public class RestaurantCreateDTO
{
    /// <summary>
    /// Name of the restaurant.
    /// </summary>
    [Required(ErrorMessage = "Restaurant name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Owner (AdminUser) identifier.
    /// </summary>
    /// <remarks>
    /// For MVP without auth, this can be a default/mock value.
    /// </remarks>
    [Required(ErrorMessage = "Owner ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Owner ID must be a positive number")]
    public int OwnerId { get; set; }

    /// <summary>
    /// Brief description of the restaurant.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Physical location/address of the restaurant.
    /// </summary>
    [Required(ErrorMessage = "Address is required")]
    public AddressDTO Address { get; set; } = new();

    /// <summary>
    /// IANA timezone identifier (e.g., "America/New_York", "Europe/London").
    /// </summary>
    /// <remarks>
    /// Defaults to "UTC" if not specified.
    /// </remarks>
    [Required(ErrorMessage = "Timezone is required")]
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Maximum number of simultaneous orders the restaurant can handle.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Max simultaneous orders must be between 1 and 1000")]
    public int MaxSimultaneousOrders { get; set; } = 50;

    /// <summary>
    /// Initial business hours configuration (optional).
    /// </summary>
    /// <remarks>
    /// If not provided, the restaurant will be created without operating hours
    /// and cannot accept orders until hours are configured.
    /// </remarks>
    public List<BusinessHoursDTO> BusinessHours { get; set; } = [];
}
