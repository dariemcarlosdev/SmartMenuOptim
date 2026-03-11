/*
 * File: AddressDTO.cs
 * Data Transfer Object for Address value object
 * Version: 1.0
 * .NET Target: .NET 8
 * 
 * Purpose: Represents address information for data transfer operations.
 * This DTO maps to the Address value object in the Domain layer.
 * 
 * Usage: Used in Restaurant DTOs for location information.
 */

namespace SmartMenuOptim.Application.Features.Restaurants.DTOs;

/// <summary>
/// Data Transfer Object for Address value object.
/// </summary>
/// <remarks>
/// <para>Maps to <see cref="Domain.ValueObjects.Address"/> value object.</para>
/// <para>All properties are mutable for Blazor form binding and CRUD operations.</para>
/// </remarks>
public class AddressDTO
{
    /// <summary>
    /// Street address line 1.
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Street address line 2 (optional).
    /// </summary>
    public string? Street2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "CA", "GB").
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full formatted address for display.
    /// </summary>
    public string FormattedAddress =>
        string.IsNullOrWhiteSpace(Street2)
            ? $"{Street}, {City}, {State} {PostalCode}, {CountryCode}"
            : $"{Street}, {Street2}, {City}, {State} {PostalCode}, {CountryCode}";
}
