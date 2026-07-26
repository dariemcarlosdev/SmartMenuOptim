/*
 * File: RestaurantUpdateDTO.cs
 * Data Transfer Object for updating an existing Restaurant
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents the data required to update an existing restaurant.
 * Used by the Application layer services and API controllers.
 * 
 * Multi-Tenant Considerations:
 * - Id must match the restaurant being updated
 * - OwnerId can be changed for ownership transfer
 * - Cannot change the RestaurantId of existing child entities
 */

using System.ComponentModel.DataAnnotations;

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for updating an existing Restaurant.
/// </summary>
/// <remarks>
/// <para>Contains all data that can be modified on an existing <see cref="Domain.Aggregates.RestaurantAggregate.Restaurant"/>.</para>
/// <para>Validation attributes provide client-side validation in Blazor forms.</para>
/// </remarks>
public class RestaurantUpdateDTO
{
    /// <summary>
    /// Restaurant identifier (required for update).
    /// </summary>
    [Required(ErrorMessage = "Restaurant ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Restaurant ID must be a positive number")]
    public int Id { get; set; }

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
    /// Changing this value transfers ownership to another admin user.
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
    [Required(ErrorMessage = "Timezone is required")]
    [StringLength(50, ErrorMessage = "Timezone cannot exceed 50 characters")]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>
    /// Maximum number of simultaneous orders the restaurant can handle.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Max simultaneous orders must be between 1 and 1000")]
    public int MaxSimultaneousOrders { get; set; } = 50;

    /// <summary>
    /// Whether the restaurant is currently accepting orders.
    /// </summary>
    /// <remarks>
    /// Can only be set to true if business hours are configured.
    /// </remarks>
    public bool IsAcceptingOrders { get; set; }

    /// <summary>
    /// Updated business hours configuration.
    /// </summary>
    public List<BusinessHoursDTO> BusinessHours { get; set; } = [];
}
